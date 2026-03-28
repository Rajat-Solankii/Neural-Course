using ELearnAggregator.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace ELearnAggregator.Services;

public interface ICourseService
{
    Task<CourseAggregateResponse> GetCoursesAsync(
        string field,
        string? type     = null,
        decimal? maxPrice = null,
        int page         = 1,
        int pageSize     = 50);
}

public class CourseService : ICourseService
{
    private readonly IYouTubeService _youtube;
    private readonly IGitHubService  _github;
    private readonly IGroqService    _groq;
    private readonly IMemoryCache    _cache;
    private readonly ILogger<CourseService> _logger;

    private static readonly Dictionary<string, string> FieldNames = new()
    {
        ["webdev"]      = "Web Development",
        ["datascience"] = "Data Science",
        ["aiml"]        = "AI / ML"
    };

    public CourseService(
        IYouTubeService youtube, IGitHubService github,
        IGroqService groq, IMemoryCache cache, ILogger<CourseService> logger)
    {
        _youtube = youtube;
        _github  = github;
        _groq    = groq;
        _cache   = cache;
        _logger  = logger;
    }

    public async Task<CourseAggregateResponse> GetCoursesAsync(
        string field, string? type, decimal? maxPrice, int page, int pageSize)
    {
        var cacheKey = $"courses_{field}_{type}_{maxPrice}_{page}_{pageSize}";

        if (_cache.TryGetValue(cacheKey, out CourseAggregateResponse? cached) && cached is not null)
            return cached;

        // Load local paid courses from embedded JSON
        var jsonPath  = Path.Combine(AppContext.BaseDirectory, "Data", "courses.json");
        var jsonText  = await File.ReadAllTextAsync(jsonPath);
        var allLocal  = JsonSerializer.Deserialize<List<Course>>(jsonText,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        var local = allLocal.Where(c => c.Field == field).ToList();

        // Fetch YouTube courses + GitHub resources in parallel
        var ytTask = _youtube.SearchCoursesAsync(field);
        var ghTask = _github.GetResourcesAsync(field);
        await Task.WhenAll(ytTask, ghTask);

        var ytCourses = await ytTask;
        var resources = await ghTask;

        // Merge all sources
        var allCourses = ytCourses.Concat(local).ToList();

        // Apply filters
        if (type == "free")  allCourses = allCourses.Where(c => c.IsFree).ToList();
        if (type == "paid")  allCourses = allCourses.Where(c => !c.IsFree).ToList();
        if (maxPrice.HasValue)
            allCourses = allCourses.Where(c => c.IsFree || c.Price <= maxPrice.Value).ToList();

        // AI ranking on full unfiltered list for better recommendations
        var recommended = await _groq.GetRecommendationsAsync(allCourses, field);

        // Paginate
        var paginated = allCourses
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var response = new CourseAggregateResponse
        {
            AllCourses     = paginated,
            TopRecommended = recommended,
            Resources      = resources,
            TotalCount     = allCourses.Count,
            Field          = FieldNames.GetValueOrDefault(field, field)
        };

        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(10));
        return response;
    }
}
