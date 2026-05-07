using GitHubDigest.Models;
using Octokit;
using Polly;
using Spectre.Console;

namespace GitHubDigest.Services;

public interface IGitHubRestClient
{
    Task<string> GetCurrentUserLoginAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PullRequestSummary>> GetMyOpenPullRequestsAsync(string? repoFilter, CancellationToken ct = default);
    Task<IReadOnlyList<IssueSummary>> GetMyAssignedIssuesAsync(int sinceDays, string? repoFilter, CancellationToken ct = default);
}

public class GitHubRestClient : IGitHubRestClient
{
    private readonly GitHubClient _client;
    private readonly ResiliencePipeline _pipeline;

    public GitHubRestClient(GitHubClient client, ResiliencePipeline pipeline)
    {
        _client = client;
        _pipeline = pipeline;
    }

    public async Task<string> GetCurrentUserLoginAsync(CancellationToken ct = default)
    {
        var user = await _pipeline.ExecuteAsync(async ct => await _client.User.Current(), ct);
        return user.Login;
    }

    public async Task<IReadOnlyList<PullRequestSummary>> GetMyOpenPullRequestsAsync(string? repoFilter, CancellationToken ct = default)
    {
        var request = new SearchIssuesRequest
        {
            Author = _client.Credentials.Login,
            Is = [IssueIsQualifier.Open, IssueIsQualifier.PullRequest],
        };

        if (repoFilter is not null)
            ApplyRepoFilter(request, repoFilter);

        var result = await _pipeline.ExecuteAsync(async ct => await _client.Search.SearchIssues(request), ct);

        return result.Items
            .Select(i => new Models.PullRequestSummary(
                Repo: ExtractRepo(i.HtmlUrl),
                Number: i.Number,
                Title: i.Title,
                Url: i.HtmlUrl,
                State: i.State.StringValue,
                CreatedAt: i.CreatedAt,
                UpdatedAt: i.UpdatedAt ?? i.CreatedAt,
                ReviewStatus: Models.ReviewStatus.Unknown,
                BaseRef: null,
                NodeId: i.NodeId
            ))
            .ToList();
    }

    public async Task<IReadOnlyList<IssueSummary>> GetMyAssignedIssuesAsync(int sinceDays, string? repoFilter, CancellationToken ct = default)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-sinceDays);

        var request = new SearchIssuesRequest
        {
            Assignee = _client.Credentials.Login,
            Is = [IssueIsQualifier.Open, IssueIsQualifier.Issue],
            Updated = new DateRange(since, SearchQualifierOperator.GreaterThanOrEqualTo),
        };

        if (repoFilter is not null)
            ApplyRepoFilter(request, repoFilter);

        var result = await _pipeline.ExecuteAsync(async ct => await _client.Search.SearchIssues(request), ct);

        return result.Items
            .Select(i => new IssueSummary(
                Repo: ExtractRepo(i.HtmlUrl),
                Number: i.Number,
                Title: i.Title,
                Url: i.HtmlUrl,
                State: i.State.StringValue,
                CreatedAt: i.CreatedAt,
                UpdatedAt: i.UpdatedAt ?? i.CreatedAt,
                Labels: i.Labels.Select(l => l.Name).ToArray()
            ))
            .ToList();
    }

    private static void ApplyRepoFilter(SearchIssuesRequest request, string repoFilter)
    {
        var parts = repoFilter.Split('/');
        if (parts.Length == 2)
            request.Repos.Add(parts[0], parts[1]);
        else
            AnsiConsole.MarkupLine($"[yellow]Invalid --repo value '{repoFilter}' — expected owner/name (e.g. microsoft/vscode). Filter ignored.[/]");
    }

    private static string ExtractRepo(string htmlUrl)
    {
        // https://github.com/owner/repo/issues/123 -> owner/repo
        var uri = new Uri(htmlUrl);
        var segments = uri.Segments;
        return segments.Length >= 3
            ? $"{segments[1].TrimEnd('/')}/{segments[2].TrimEnd('/')}"
            : htmlUrl;
    }
}
