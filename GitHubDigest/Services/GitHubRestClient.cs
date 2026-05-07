using GitHubDigest.Models;
using Octokit;

namespace GitHubDigest.Services;

public interface IGitHubRestClient
{
    Task<IReadOnlyList<PullRequestSummary>> GetMyOpenPullRequestsAsync(string? repoFilter, CancellationToken ct = default);
    Task<IReadOnlyList<IssueSummary>> GetMyAssignedIssuesAsync(int sinceDays, string? repoFilter, CancellationToken ct = default);
}

public class GitHubRestClient : IGitHubRestClient
{
    private readonly GitHubClient _client;

    public GitHubRestClient(GitHubClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<PullRequestSummary>> GetMyOpenPullRequestsAsync(string? repoFilter, CancellationToken ct = default)
    {
        var request = new SearchIssuesRequest
        {
            Author = _client.Credentials.Login,
            Is = [IssueIsQualifier.Open, IssueIsQualifier.PullRequest],
        };

        if (repoFilter is not null)
        {
            var parts = repoFilter.Split('/');
            if (parts.Length == 2)
                request.Repos.Add(parts[0], parts[1]);
        }

        var result = await _client.Search.SearchIssues(request);

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
                BaseRef: null
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
        {
            var parts = repoFilter.Split('/');
            if (parts.Length == 2)
                request.Repos.Add(parts[0], parts[1]);
        }

        var result = await _client.Search.SearchIssues(request);

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
