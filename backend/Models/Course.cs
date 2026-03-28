namespace ELearnAggregator.Models;

public class Course
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "INR";
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string CourseUrl { get; set; } = string.Empty;
    public string? YoutubeVideoId { get; set; }
    public string Duration { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public bool IsFree { get; set; }
    public List<string> Tags { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = "local"; // local | youtube
}

public class ResourceLink
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // github | docs | pdf
    public string Description { get; set; } = string.Empty;
    public int? Stars { get; set; }
    public string? Language { get; set; }
}

public class RecommendedCourse
{
    public int Rank { get; set; }
    public Course Course { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public double Score { get; set; }
}

public class CourseAggregateResponse
{
    public List<Course> AllCourses { get; set; } = new();
    public List<RecommendedCourse> TopRecommended { get; set; } = new();
    public List<ResourceLink> Resources { get; set; } = new();
    public int TotalCount { get; set; }
    public string Field { get; set; } = string.Empty;
}
