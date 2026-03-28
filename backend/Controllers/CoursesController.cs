using ELearnAggregator.Services;
using Microsoft.AspNetCore.Mvc;

namespace ELearnAggregator.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courses;
    private readonly ILogger<CoursesController> _logger;

    private static readonly HashSet<string> ValidFields = ["webdev", "datascience", "aiml"];

    public CoursesController(ICourseService courses, ILogger<CoursesController> logger)
    {
        _courses = courses;
        _logger  = logger;
    }

    /// <summary>
    /// GET /api/courses/{field}
    /// Returns all courses + AI top-5 recommendations for the given field.
    /// </summary>
    /// <param name="field">webdev | datascience | aiml</param>
    /// <param name="type">free | paid (optional)</param>
    /// <param name="maxPrice">Maximum price in INR (optional)</param>
    /// <param name="page">Page number – default 1</param>
    /// <param name="pageSize">Results per page – default 50</param>
    [HttpGet("{field}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCourses(
        string field,
        [FromQuery] string?  type     = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int      page     = 1,
        [FromQuery] int      pageSize = 50)
    {
        if (!ValidFields.Contains(field.ToLower()))
            return BadRequest(new { error = "Invalid field. Allowed values: webdev, datascience, aiml" });

        try
        {
            var result = await _courses.GetCoursesAsync(
                field.ToLower(), type?.ToLower(), maxPrice, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching courses for field: {Field}", field);
            return StatusCode(500, new { error = "Failed to fetch courses. Please try again." });
        }
    }

    /// <summary>
    /// GET /api/courses/{field}/filter?type=free|paid
    /// Convenience filter endpoint.
    /// </summary>
    [HttpGet("{field}/filter")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> FilterCourses(
        string field,
        [FromQuery] string?  type     = null,
        [FromQuery] decimal? maxPrice = null)
        => await GetCourses(field, type, maxPrice);
}
