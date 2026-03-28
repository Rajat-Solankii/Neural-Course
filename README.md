# ⚡ NeuralCourse — ASP.NET Core .NET 8 Backend

## 📁 Project Structure

```
ELearnAggregator/
├── ELearnAggregator.csproj        ← .NET 8 project file
├── Program.cs                     ← DI, CORS, HttpClientFactory, Swagger
├── appsettings.json               ← API key config (or use env vars)
├── Controllers/
│   └── CoursesController.cs       ← GET /api/courses/{field}
├── Models/
│   └── Course.cs                  ← Course, ResourceLink, RecommendedCourse, Response
├── Services/
│   ├── YouTubeService.cs          ← YouTube Data API v3 + mock fallback
│   ├── GitHubService.cs           ← GitHub Search API + mock fallback
│   ├── GroqService.cs             ← Groq LLaMA-3 AI ranking + algorithmic fallback
│   └── CourseService.cs           ← Aggregator: merges all sources + caching
└── Data/
    └── courses.json               ← 20 paid courses (Udemy + Coursera)
```

---

## 🚀 Run the Backend

### Prerequisites
Install [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)

```bash
# Verify installation
dotnet --version   # should print 8.x.x
```

### Steps

```bash
# 1. Navigate to project folder
cd ELearnAggregator

# 2. (Optional) Set API keys as environment variables
#    Linux / macOS:
export YOUTUBE_API_KEY="AIzaSy..."
export GITHUB_TOKEN="ghp_..."
export GROQ_API_KEY="gsk_..."

#    Windows PowerShell:
$env:YOUTUBE_API_KEY = "AIzaSy..."
$env:GITHUB_TOKEN    = "ghp_..."
$env:GROQ_API_KEY    = "gsk_..."

# 3. Restore packages
dotnet restore

# 4. Run
dotnet run
```

**API running at:** `http://localhost:5000`
**Swagger UI at:**  `http://localhost:5000/swagger`

> ✅ Works without API keys — falls back to rich mock data automatically.

---

## 🔗 API Endpoints

| Method | URL | Description |
|--------|-----|-------------|
| GET | `/api/courses/webdev` | Web Development courses + AI picks |
| GET | `/api/courses/datascience` | Data Science courses + AI picks |
| GET | `/api/courses/aiml` | AI/ML courses + AI picks |
| GET | `/api/courses/{field}/filter?type=free` | Free courses only |
| GET | `/api/courses/{field}?maxPrice=1500` | Courses under ₹1500 |
| GET | `/api/courses/{field}?page=2&pageSize=10` | Pagination |

### Sample Response

```json
{
  "allCourses": [
    {
      "id": "yt-nu_pCVPKzTk",
      "title": "Full Stack Web Development Course 2024",
      "platform": "YouTube",
      "instructor": "Traversy Media",
      "price": 0,
      "isFree": true,
      "rating": 4.8,
      "reviewCount": 85420,
      "youtubeVideoId": "nu_pCVPKzTk",
      "tags": ["HTML", "CSS", "JavaScript", "React"]
    }
  ],
  "topRecommended": [
    {
      "rank": 1,
      "score": 9.5,
      "reason": "Highest-rated with exceptional learner satisfaction...",
      "course": { ... }
    }
  ],
  "resources": [
    {
      "title": "freeCodeCamp",
      "url": "https://github.com/freeCodeCamp/freeCodeCamp",
      "type": "github",
      "stars": 390000,
      "language": "TypeScript"
    }
  ],
  "totalCount": 8,
  "field": "Web Development"
}
```

---

## 🔑 Getting API Keys (all free)

| Key | Where | Notes |
|-----|-------|-------|
| `YOUTUBE_API_KEY` | [Google Cloud Console](https://console.cloud.google.com) → Enable YouTube Data API v3 → Credentials | Free 10,000 units/day |
| `GITHUB_TOKEN` | [GitHub Settings → Developer Settings → Tokens](https://github.com/settings/tokens) | No special scopes needed |
| `GROQ_API_KEY` | [console.groq.com](https://console.groq.com) → API Keys | Free tier, very fast LLaMA-3 |

---

## ⚙️ Architecture Highlights

- **Async/await** throughout — zero blocking I/O
- **`IHttpClientFactory`** — proper HTTP client lifecycle, no socket exhaustion
- **`Task.WhenAll`** — YouTube + GitHub fetched in parallel
- **`IMemoryCache`** — 10-minute TTL per field+filter combo
- **Graceful fallback** — every external API has a mock/algorithmic fallback
- **Pagination** — `page` + `pageSize` query params
- **CORS** — configured to allow the frontend dev server
- **Swagger** — auto-generated interactive API docs
