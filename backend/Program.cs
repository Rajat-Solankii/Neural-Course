using ELearnAggregator.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers + Swagger ─────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "NeuralCourse API",
        Version     = "v1",
        Description = "AI-powered e-learning aggregator – YouTube + GitHub + Groq"
    });
});

// ── In-memory cache ───────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ── CORS – allow the React/HTML frontend dev server ───────────────────────
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p =>
        p.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader()));

// ── Named HttpClients via IHttpClientFactory ──────────────────────────────
builder.Services.AddHttpClient("YouTube", c =>
{
    c.BaseAddress = new Uri("https://www.googleapis.com/");
    c.Timeout     = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient("GitHub", c =>
{
    c.BaseAddress = new Uri("https://api.github.com/");
    c.DefaultRequestHeaders.Add("User-Agent",  "NeuralCourse/1.0");
    c.DefaultRequestHeaders.Add("Accept",      "application/vnd.github+json");
    c.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    c.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient("Groq", c =>
{
    c.BaseAddress = new Uri("https://api.groq.com/");
    c.Timeout     = TimeSpan.FromSeconds(20);

    // Inject Groq Bearer token if configured
    var groqKey = builder.Configuration["GROQ_API_KEY"];
    if (!string.IsNullOrEmpty(groqKey))
        c.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", groqKey);
});

// ── Application services ──────────────────────────────────────────────────
builder.Services.AddScoped<IYouTubeService, YouTubeService>();
builder.Services.AddScoped<IGitHubService,  GitHubService>();
builder.Services.AddScoped<IGroqService,    GroqService>();
builder.Services.AddScoped<ICourseService,  CourseService>();

// ── Build ─────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NeuralCourse API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
