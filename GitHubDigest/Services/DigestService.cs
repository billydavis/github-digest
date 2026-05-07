using GitHubDigest.Models;

namespace GitHubDigest.Services;

public class DigestService
{
    private readonly IGitHubRestClient _rest;
    private readonly IGitHubGraphQLClient _graphql;

    public DigestService(IGitHubRestClient rest, IGitHubGraphQLClient graphql)
    {
        _rest = rest;
        _graphql = graphql;
    }

    public async Task<DigestReport> BuildReportAsync(int sinceDays, string? repoFilter, CancellationToken ct = default)
    {
        var (prs, issues, contributions) = await FetchAllAsync(sinceDays, repoFilter, ct);
        return new DigestReport(prs, issues, contributions);
    }

    private async Task<(IReadOnlyList<PullRequestSummary>, IReadOnlyList<IssueSummary>, ContributionSummary)> FetchAllAsync(
        int sinceDays, string? repoFilter, CancellationToken ct)
    {
        var prsTask = _rest.GetMyOpenPullRequestsAsync(repoFilter, ct);
        var issuesTask = _rest.GetMyAssignedIssuesAsync(sinceDays, repoFilter, ct);
        var contributionsTask = _graphql.GetContributionSummaryAsync(sinceDays, ct);

        await Task.WhenAll(prsTask, issuesTask, contributionsTask);

        return (await prsTask, await issuesTask, await contributionsTask);
    }
}
