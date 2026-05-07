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

## Package versions (as of scaffold)
- Octokit 14.0.0
- Octokit.GraphQL 0.4.0-beta (only stable prerelease available)
- Spectre.Console 0.55.2
- System.CommandLine 3.0.0-preview.3 (prerelease)
- Polly 8.6.6
- Microsoft.Extensions.* 10.0.7

## Project structure
GitHubDigest/
├── Commands/
│   └── DigestCommand.cs        — System.CommandLine root command, wires --since/--repo/--output
├── Services/
│   ├── DigestService.cs        — orchestrates parallel REST + GraphQL calls via Task.WhenAll
│   ├── GitHubRestClient.cs     — open PRs + assigned issues via Search API
│   └── GitHubGraphQLClient.cs  — contribution summary via Octokit.GraphQL
├── Models/
│   ├── DigestReport.cs         — top-level report aggregate + ContributionSummary
│   ├── PullRequestSummary.cs   — PR record + ReviewStatus enum
│   └── IssueSummary.cs         — issue record
├── Renderers/
│   ├── TerminalRenderer.cs     — Spectre.Console colored tables
│   └── MarkdownRenderer.cs     — .md file export
├── appsettings.json
└── Program.cs                  — DI bootstrap, config, entry point

## Auth
PAT token read from env var `GITHUB__TOKEN` or `appsettings.json` key `GitHub:Token`.
Never hardcoded. The double-underscore in `GITHUB__TOKEN` is the .NET env var hierarchy separator.

## Key implementation notes

### GitHub Search API quirk
The REST API doesn't filter PRs by author at the repo level.
Use `client.Search.SearchIssues()` with `IssueIsQualifier.PullRequest` instead.
The Search API is rate-limited to 30 req/min — wire Polly early.

### Rate limit retry (Polly) — Phase 5, not yet implemented
Catch `RateLimitExceededException`, read `rle.Reset` for exact wait time.
Show user a yellow Spectre.Console message during the wait.

### GraphQL for contribution data
Use `Octokit.GraphQL` (not raw HTTP) for:
- `ContributionsCollection.TotalCommitContributions`
- `TotalPullRequestReviewContributions`
Note: `TotalPullRequestContributions` does NOT exist in Octokit.GraphQL 0.4.0-beta.
`Connection` is `Octokit.GraphQL.Connection`; `ProductHeaderValue` is `Octokit.GraphQL.ProductHeaderValue`.
`ContributionsCollection` signature: `(from?, organizationID?, to?)` — always use named params `from:` and `to:`.

### Namespace collision
`Octokit` and `Octokit.GraphQL` both expose `Connection` and `ProductHeaderValue`.
In `Program.cs` use aliases: `using OctokitRest = Octokit;` and `using OctokitGQL = Octokit.GraphQL;`

### System.CommandLine 3.0 preview API
- `command.Add(option)` — not `AddOption`
- `command.SetAction(Func<ParseResult, CancellationToken, Task>)` — not `SetHandler`
- `new Option<T>("--name") { DefaultValueFactory = _ => value }` — lambda not in constructor
- `command.Parse(args).InvokeAsync()` — `InvokeAsync` is on `ParseResult`, not the command
- `parseResult.GetValue(option)` to read values in the action

### Parallel REST calls
`DigestService.BuildReportAsync` fires PRs, issues, and contributions in parallel via `Task.WhenAll`.

### Output
Default: Spectre.Console colored table in terminal
`--output markdown` flag: writes `github-digest-YYYY-MM-DD.md` in the working directory
`--since 7d` / `--since 30d`: configurable lookback (default 14 days)
`--repo owner/name`: scope to a specific repo

## Phase plan
1. ✅ Scaffold project, wire DI and config
2. ✅ REST client — open PRs + assigned issues (Search API)
3. ✅ Terminal renderer with Spectre.Console
4. ✅ GraphQL client — contribution summary
5. ⬜ Polly rate limit handling
6. ✅ `--output markdown` export
7. ✅ `dotnet tool` packaging validation + publish

## Build & run
```sh
cd GitHubDigest
dotnet run -- --since 7d
dotnet run -- --since 14d --output markdown
dotnet run -- --repo owner/myrepo
```

## Pack as tool
```sh
dotnet pack
dotnet tool install --global --add-source ./nupkg GitHubDigest
github-digest --since 7d
```
