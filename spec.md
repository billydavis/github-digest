# GitHub Digest CLI — Project Spec

## What it is
A `dotnet tool` CLI that produces a morning briefing of your GitHub activity:
- All open PRs you authored, across all repos, with review status
- Issues assigned to you updated in the last N days
- Contribution summary (commits, PRs opened, reviews given this week)

## Tech stack
- .NET 9 console app, packaged as a `dotnet tool`
- `Octokit` — GitHub REST API (PRs, issues, search)
- `Octokit.GraphQL` — GitHub GraphQL API (contribution summary, review activity)
- `Spectre.Console` — terminal output (colored tables, progress spinners)
- `System.CommandLine` (prerelease) — CLI flags
- `Microsoft.Extensions.DependencyInjection` — DI
- `Microsoft.Extensions.Configuration.Json` + `.EnvironmentVariables` — config
- `Microsoft.Extensions.Caching.Memory` — rate limit guard
- `Polly` — retry on rate limit with backoff

## Project structure
GitHubDigest/
├── Commands/
│   └── DigestCommand.cs
├── Services/
│   ├── DigestService.cs
│   ├── GitHubRestClient.cs
│   └── GitHubGraphQLClient.cs
├── Models/
│   ├── DigestReport.cs
│   ├── PullRequestSummary.cs
│   └── IssueSummary.cs
├── Renderers/
│   ├── TerminalRenderer.cs
│   └── MarkdownRenderer.cs
├── appsettings.json
└── Program.cs

## Auth
PAT token read from env var `GITHUB__TOKEN` or `appsettings.json` key `GitHub:Token`.
Never hardcoded.

## Key implementation notes

### GitHub Search API quirk
The REST API doesn't filter PRs by author at the repo level.
Use `client.Search.SearchIssues()` with `IssueIsQualifier.PullRequest` instead.
The Search API is rate-limited to 30 req/min — wire Polly early.

### Rate limit retry (Polly)
Catch `RateLimitExceededException`, read `rle.Reset` for exact wait time.
Show user a yellow Spectre.Console message during the wait.

### GraphQL for contribution data
Use `Octokit.GraphQL` (not raw HTTP) for:
- `ContributionsCollection.TotalCommitContributions`
- `TotalPullRequestContributions`
- `TotalPullRequestReviewContributions`

### Parallel REST calls
Fire `GetMyOpenPullRequestsAsync()` and `GetMyAssignedIssuesAsync()` in parallel
using `Task.WhenAll` — they don't depend on each other.

### Output
Default: Spectre.Console colored table in terminal
`--output markdown` flag: write a .md file
`--since 7d` / `--since 30d`: configurable lookback (default 14 days)
`--repo owner/name`: scope to a specific repo

## Phase plan
1. Scaffold project, wire DI and config
2. REST client — open PRs + assigned issues (Search API)
3. Terminal renderer with Spectre.Console
4. GraphQL client — contribution summary
5. Polly rate limit handling
6. `--output markdown` export
7. `dotnet tool` packaging (`IsPackable`, `PackAsTool` in .csproj)

## What does NOT exist yet
No competing C# tool in this space. 
The Go-based `gh` CLI does individual queries but no cross-repo digest.
This is a genuine gap in the dotnet tool ecosystem.