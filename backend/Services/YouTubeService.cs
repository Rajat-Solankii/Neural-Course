using ELearnAggregator.Models;
using System.Text.Json;

namespace ELearnAggregator.Services;

public interface IYouTubeService
{
    Task<List<Course>> SearchCoursesAsync(string field);
}

public class YouTubeService : IYouTubeService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<YouTubeService> _logger;

    private static readonly Dictionary<string, string[]> FieldQueries = new()
    {
        ["webdev"]      = ["full stack web development course 2024", "react javascript tutorial complete"],
        ["datascience"] = ["data science python full course 2024", "pandas numpy matplotlib tutorial"],
        ["aiml"]        = ["machine learning full course 2024", "deep learning tensorflow pytorch tutorial"]
    };

    public YouTubeService(IHttpClientFactory factory, IConfiguration config, ILogger<YouTubeService> logger)
    {
        _http   = factory.CreateClient("YouTube");
        _config = config;
        _logger = logger;
    }

    public async Task<List<Course>> SearchCoursesAsync(string field)
    {
        var apiKey = _config["YOUTUBE_API_KEY"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("YouTube API key not set. Returning mock data.");
            return MockYouTube(field);
        }

        var queries  = FieldQueries.GetValueOrDefault(field, ["online course tutorial"]);
        var courses  = new List<Course>();
        var rng      = new Random();

        foreach (var query in queries.Take(2))
        {
            try
            {
                var url = $"https://www.googleapis.com/youtube/v3/search" +
                          $"?part=snippet&q={Uri.EscapeDataString(query)}&type=video" +
                          $"&videoDuration=long&maxResults=5&key={apiKey}";

                var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode) continue;

                var doc   = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var items = doc.RootElement.GetProperty("items");

                foreach (var item in items.EnumerateArray())
                {
                    var videoId = item.GetProperty("id").GetProperty("videoId").GetString() ?? "";
                    var snippet = item.GetProperty("snippet");

                    courses.Add(new Course
                    {
                        Id             = $"yt-{videoId}",
                        Title          = snippet.GetProperty("title").GetString() ?? "",
                        Platform       = "YouTube",
                        Field          = field,
                        Instructor     = snippet.GetProperty("channelTitle").GetString() ?? "",
                        Price          = 0,
                        Rating         = Math.Round(4.0 + rng.NextDouble() * 0.9, 1),
                        ReviewCount    = rng.Next(5_000, 200_000),
                        ThumbnailUrl   = snippet.GetProperty("thumbnails").GetProperty("high").GetProperty("url").GetString() ?? "",
                        CourseUrl      = $"https://www.youtube.com/watch?v={videoId}",
                        YoutubeVideoId = videoId,
                        IsFree         = true,
                        Duration       = "10+ hours",
                        Level          = "Beginner to Intermediate",
                        Tags           = [field, "free", "youtube"],
                        Description    = snippet.GetProperty("description").GetString() ?? "",
                        Source         = "youtube"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "YouTube error for query: {Q}", query);
            }
        }

        return courses.Count > 0 ? courses : MockYouTube(field);
    }

    // ── Mock fallback ──────────────────────────────────────────────────────
    private static List<Course> MockYouTube(string field) => field switch
    {
        "webdev" =>
        [
            new() { Id="yt-nu_pCVPKzTk", Title="Full Stack Web Development Course 2024", Platform="YouTube", Field="webdev", Instructor="Traversy Media", Price=0, Rating=4.8, ReviewCount=85420, ThumbnailUrl="https://i.ytimg.com/vi/nu_pCVPKzTk/hqdefault.jpg", CourseUrl="https://www.youtube.com/watch?v=nu_pCVPKzTk", YoutubeVideoId="nu_pCVPKzTk", IsFree=true, Duration="11h 32m", Level="Beginner", Tags=["HTML","CSS","JavaScript","React"], Description="Complete web dev crash course covering HTML, CSS, JS and React fundamentals.", Source="youtube" },
            new() { Id="yt-RVFAyFWO4go", Title="React JS Full Course for Beginners | Complete Tutorial", Platform="YouTube", Field="webdev", Instructor="Dave Gray", Price=0, Rating=4.7, ReviewCount=62100, ThumbnailUrl="https://i.ytimg.com/vi/RVFAyFWO4go/hqdefault.jpg", CourseUrl="https://www.youtube.com/watch?v=RVFAyFWO4go", YoutubeVideoId="RVFAyFWO4go", IsFree=true, Duration="9h 15m", Level="Beginner", Tags=["React","Hooks","JSX","State"], Description="Learn React JS from scratch including hooks, context, and routing.", Source="youtube" },
            new() { Id="yt-jS4aFq5-91M", Title="JavaScript Full Course – Beginner to Professional", Platform="YouTube", Field="webdev", Instructor="freeCodeCamp.org", Price=0, Rating=4.9, ReviewCount=125300, ThumbnailUrl="https://i.ytimg.com/vi/jS4aFq5-91M/hqdefault.jpg", CourseUrl="https://www.youtube.com/watch?v=jS4aFq5-91M", YoutubeVideoId="jS4aFq5-91M", IsFree=true, Duration="12h 0m", Level="Beginner", Tags=["JavaScript","ES6","DOM","Async"], Description="Complete JavaScript from variables to async/await.", Source="youtube" },
        ],
        "datascience" =>
        [
            new() { Id="yt-ua-CiDNNj30", Title="Data Science Full Course 2024 | Learn Data Science", Platform="YouTube", Field="datascience", Instructor="Simplilearn", Price=0, Rating=4.7, ReviewCount=95200, ThumbnailUrl="https://i.ytimg.com/vi/ua-CiDNNj30/hqdefault.jpg", CourseUrl="https://www.youtube.com/watch?v=ua-CiDNNj30", YoutubeVideoId="ua-CiDNNj30", IsFree=true, Duration="11h 0m", Level="Beginner", Tags=["Python","Statistics","ML","Visualization"], Description="Master data science with Python.", Source="youtube" },
            new() { Id="yt-LHBE6Q9XlzI", Title="Python for Data Science – Full Course", Platform="YouTube", Field="datascience", Instructor="freeCodeCamp.org", Price=0, Rating=4.8, ReviewCount=148000, ThumbnailUrl="https://i.ytimg.com/vi/LHBE6Q9XlzI/hqdefault.jpg", CourseUrl="https://www.youtube.com/watch?v=LHBE6Q9XlzI", YoutubeVideoId="LHBE6Q9XlzI", IsFree=true, Duration="12h 20m", Level="Beginner", Tags=["Python","Pandas","NumPy","Matplotlib"], Description="Python for data science including pandas, numpy, and visualization.", Source="youtube" },
        ],
        _ =>
        [
            new() { Id="yt-GwIo3gDZCVQ", Title="Machine Learning Full Course 2024", Platform="YouTube", Field="aiml", Instructor="Simplilearn", Price=0, Rating=4.7, ReviewCount=78400, ThumbnailUrl="https://i.ytimg.com/vi/GwIo3gDZCVQ/hqdefault.jpg", CourseUrl="https://www.youtube.com/watch?v=GwIo3gDZCVQ", YoutubeVideoId="GwIo3gDZCVQ", IsFree=true, Duration="10h 0m", Level="Beginner", Tags=["ML","Sklearn","Regression","Classification"], Description="Complete machine learning course for beginners.", Source="youtube" },
            new() { Id="yt-tPYj3fFJGjk", Title="Deep Learning with TensorFlow 2.0 | Full Course", Platform="YouTube", Field="aiml", Instructor="freeCodeCamp.org", Price=0, Rating=4.8, ReviewCount=92100, ThumbnailUrl="https://i.ytimg.com/vi/tPYj3fFJGjk/hqdefault.jpg", CourseUrl="https://www.youtube.com/watch?v=tPYj3fFJGjk", YoutubeVideoId="tPYj3fFJGjk", IsFree=true, Duration="13h 10m", Level="Intermediate", Tags=["TensorFlow","Deep Learning","CNN","Keras"], Description="Learn deep learning with TensorFlow 2.0 from scratch.", Source="youtube" },
        ]
    };
}
