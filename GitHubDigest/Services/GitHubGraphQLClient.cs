using GitHubDigest.Models;
using Octokit.GraphQL;

namespace GitHubDigest.Services;

public interface IGitHubGraphQLClient
{
    Task<ContributionSummary> GetContributionSummaryAsync(int sinceDays, CancellationToken ct = default);
}

public class GitHubGraphQLClient : IGitHubGraphQLClient
{
    private readonly Connection _connection;

    public GitHubGraphQLClient(Connection connection)
    {
        _connection = connection;
    }

    public async Task<ContributionSummary> GetContributionSummaryAsync(int sinceDays, CancellationToken ct = default)
    {
        var from = DateTimeOffset.UtcNow.AddDays(-sinceDays);
        var to = DateTimeOffset.UtcNow;

        var query = new Query()
            .Viewer
            .ContributionsCollection(from: from, to: to)
            .Select(c => new
            {
                c.TotalCommitContributions,
                c.TotalPullRequestReviewContributions
            })
            .Compile();

        var result = await _connection.Run(query, cancellationToken: ct);

        return new ContributionSummary(
            TotalCommits: result.TotalCommitContributions,
            TotalPullRequestsOpened: 0,
            TotalReviewsGiven: result.TotalPullRequestReviewContributions,
            From: from,
            To: to
        );
    }
}
