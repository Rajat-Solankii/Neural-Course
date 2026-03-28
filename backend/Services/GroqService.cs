using ELearnAggregator.Models;
using System.Text.Json;

namespace ELearnAggregator.Services;

public interface IGroqService
{
    Task<List<RecommendedCourse>> GetRecommendationsAsync(List<Course> courses, string field);
}

public class GroqService : IGroqService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<GroqService> _logger;

    public GroqService(IHttpClientFactory factory, IConfiguration config, ILogger<GroqService> logger)
    {
        _http   = factory.CreateClient("Groq");
        _config = config;
        _logger = logger;
    }

    public async Task<List<RecommendedCourse>> GetRecommendationsAsync(List<Course> courses, string field)
    {
        var apiKey = _config["GROQ_API_KEY"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Groq API key not set. Using algorithmic ranking.");
            return Algorithmic(courses);
        }

        try
        {
            var lines = courses.Take(15).Select((c, i) =>
                string.Format("{0}. [{1}] {2} | Rating:{3:F1} | Price:{4} | Level:{5} | Tags:{6}",
                    i + 1,
                    c.Platform,
                    c.Title,
                    c.Rating,
                    c.IsFree ? "Free" : "Rs." + c.Price,
                    c.Level,
                    string.Join(",", c.Tags.Take(4))));

            var courseList = string.Join("\n", lines);

            var prompt =
                "You are an expert e-learning advisor. Pick the TOP 5 " + field + " courses below.\n\n" +
                courseList + "\n\n" +
                "Return ONLY a JSON array with exactly 5 objects (no markdown fences):\n" +
                "[{\"rank\":1,\"courseIndex\":1,\"score\":9.5,\"reason\":\"reason here\"},...]\n\n" +
                "Prioritize: free > high rating > comprehensive content > engagement.";

            var body = new
            {
                model       = "llama3-8b-8192",
                max_tokens  = 800,
                temperature = 0.3,
                messages    = new[] { new { role = "user", content = prompt } }
            };

            var resp = await _http.PostAsJsonAsync("https://api.groq.com/openai/v1/chat/completions", body);
            if (!resp.IsSuccessStatusCode) return Algorithmic(courses);

            var json       = await resp.Content.ReadAsStringAsync();
            var doc        = JsonDocument.Parse(json);
            var rawContent = doc.RootElement
                               .GetProperty("choices")[0]
                               .GetProperty("message")
                               .GetProperty("content")
                               .GetString() ?? "[]";

            var s = rawContent.IndexOf('[');
            var e = rawContent.LastIndexOf(']') + 1;
            if (s < 0 || e <= s) return Algorithmic(courses);

            var arr    = JsonDocument.Parse(rawContent[s..e]).RootElement;
            var result = new List<RecommendedCourse>();

            foreach (var item in arr.EnumerateArray())
            {
                var idx = item.GetProperty("courseIndex").GetInt32() - 1;
                if (idx < 0 || idx >= courses.Count) continue;

                result.Add(new RecommendedCourse
                {
                    Rank   = item.GetProperty("rank").GetInt32(),
                    Course = courses[idx],
                    Score  = item.GetProperty("score").GetDouble(),
                    Reason = item.GetProperty("reason").GetString() ?? ""
                });
            }

            return result.OrderBy(r => r.Rank).Take(5).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq API error");
            return Algorithmic(courses);
        }
    }

    private static List<RecommendedCourse> Algorithmic(List<Course> courses)
    {
        string[] reasons =
        [
            "Highest-rated in this category with exceptional learner satisfaction and comprehensive coverage.",
            "Best free resource - massive engagement and community-backed content with real-world projects.",
            "Perfect for beginners - covers all fundamentals with a clear, structured step-by-step approach.",
            "Industry-aligned curriculum by recognised experts, packed with hands-on exercises.",
            "Outstanding value - combines theory with practice at one of the best price-to-quality ratios."
        ];

        return courses
            .OrderByDescending(c => c.Rating * 0.4 + (c.IsFree ? 3 : 0) + Math.Log(c.ReviewCount + 1) * 0.3)
            .Take(5)
            .Select((c, i) => new RecommendedCourse
            {
                Rank   = i + 1,
                Course = c,
                Score  = Math.Round(10.0 - i * 0.6, 1),
                Reason = reasons[i]
            })
            .ToList();
    }
}
