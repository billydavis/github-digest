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
        var usernameTask = _rest.GetCurrentUserLoginAsync(ct);
        var prsTask = _rest.GetMyOpenPullRequestsAsync(repoFilter, ct);
        var issuesTask = _rest.GetMyAssignedIssuesAsync(sinceDays, repoFilter, ct);
        var contributionsTask = _graphql.GetContributionSummaryAsync(sinceDays, ct);

        var prs = await prsTask;
        var reviewDecisionsTask = _graphql.GetReviewDecisionsAsync(prs, ct);

        await Task.WhenAll(usernameTask, issuesTask, contributionsTask, reviewDecisionsTask);

        var reviewDecisions = await reviewDecisionsTask;
        var prsWithReviews = prs
            .Select(pr => pr.NodeId is not null && reviewDecisions.TryGetValue(pr.NodeId, out var status)
                ? pr with { ReviewStatus = status }
                : pr)
            .ToList();

        return new DigestReport(prsWithReviews, await issuesTask, await contributionsTask, await usernameTask);
    }
}
