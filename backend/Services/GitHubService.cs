using ELearnAggregator.Models;
using System.Text.Json;

namespace ELearnAggregator.Services;

public interface IGitHubService
{
    Task<List<ResourceLink>> GetResourcesAsync(string field);
}

public class GitHubService : IGitHubService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<GitHubService> _logger;

    private static readonly Dictionary<string, string[]> FieldTopics = new()
    {
        ["webdev"]      = ["awesome-web-development", "awesome-react"],
        ["datascience"] = ["awesome-data-science", "awesome-python"],
        ["aiml"]        = ["awesome-machine-learning", "awesome-deep-learning"]
    };

    public GitHubService(IHttpClientFactory factory, IConfiguration config, ILogger<GitHubService> logger)
    {
        _http   = factory.CreateClient("GitHub");
        _config = config;
        _logger = logger;
    }

    public async Task<List<ResourceLink>> GetResourcesAsync(string field)
    {
        var token  = _config["GITHUB_TOKEN"];
        var topics = FieldTopics.GetValueOrDefault(field, ["awesome-programming"]);
        var result = new List<ResourceLink>();

        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        foreach (var topic in topics.Take(2))
        {
            try
            {
                var url  = $"https://api.github.com/search/repositories?q=topic:{topic}&sort=stars&per_page=4";
                var resp = await _http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) continue;

                var doc   = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                var items = doc.RootElement.GetProperty("items");

                foreach (var item in items.EnumerateArray())
                {
                    result.Add(new ResourceLink
                    {
                        Id          = $"gh-{item.GetProperty("id").GetInt32()}",
                        Title       = item.GetProperty("name").GetString() ?? "",
                        Url         = item.GetProperty("html_url").GetString() ?? "",
                        Type        = "github",
                        Description = item.TryGetProperty("description", out var d) && d.ValueKind != JsonValueKind.Null
                                        ? d.GetString() ?? "" : "",
                        Stars       = item.GetProperty("stargazers_count").GetInt32(),
                        Language    = item.TryGetProperty("language", out var l) && l.ValueKind != JsonValueKind.Null
                                        ? l.GetString() : null
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GitHub error for topic: {T}", topic);
            }
        }

        return result.Count > 0 ? result : MockResources(field);
    }

    private static List<ResourceLink> MockResources(string field) => field switch
    {
        "webdev" =>
        [
            new() { Id="r-fc",   Title="freeCodeCamp",           Url="https://github.com/freeCodeCamp/freeCodeCamp",       Type="github", Description="freeCodeCamp open-source codebase and curriculum.",             Stars=390000, Language="TypeScript" },
            new() { Id="r-jsa",  Title="javascript-algorithms",  Url="https://github.com/trekhleb/javascript-algorithms",  Type="github", Description="Algorithms and data structures implemented in JavaScript.",      Stars=185000, Language="JavaScript" },
            new() { Id="r-mdn",  Title="MDN Web Docs",           Url="https://developer.mozilla.org/",                     Type="docs",   Description="The definitive reference for HTML, CSS, and JavaScript.",        Stars=null,   Language=null },
        ],
        "datascience" =>
        [
            new() { Id="r-ads",  Title="awesome-data-science",   Url="https://github.com/academic/awesome-datascience",    Type="github", Description="Curated list for Data Science learning resources.",             Stars=24000,  Language=null },
            new() { Id="r-pd",   Title="pandas",                 Url="https://github.com/pandas-dev/pandas",               Type="github", Description="Flexible data analysis library for Python.",                   Stars=42000,  Language="Python" },
            new() { Id="r-kag",  Title="Kaggle Learn",           Url="https://www.kaggle.com/learn",                       Type="docs",   Description="Free micro-courses: Python, ML, SQL and data visualization.",  Stars=null,   Language=null },
        ],
        _ =>
        [
            new() { Id="r-aml",  Title="awesome-machine-learning", Url="https://github.com/josephmisiti/awesome-machine-learning", Type="github", Description="Curated list of ML frameworks and libraries.", Stars=64000, Language=null },
            new() { Id="r-skl",  Title="scikit-learn",             Url="https://github.com/scikit-learn/scikit-learn",             Type="github", Description="Machine Learning in Python.",                Stars=59000, Language="Python" },
            new() { Id="r-pwc",  Title="Papers With Code",         Url="https://paperswithcode.com/",                             Type="docs",   Description="Latest ML papers with open-source implementations.", Stars=null, Language=null },
        ]
    };
}
