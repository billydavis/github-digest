using GitHubDigest.Models;
using Octokit.GraphQL;
using Polly;

namespace GitHubDigest.Services;

public interface IGitHubGraphQLClient
{
    Task<ContributionSummary> GetContributionSummaryAsync(int sinceDays, CancellationToken ct = default);
}

public class GitHubGraphQLClient : IGitHubGraphQLClient
{
    private readonly Connection _connection;
    private readonly ResiliencePipeline _pipeline;

    public GitHubGraphQLClient(Connection connection, ResiliencePipeline pipeline)
    {
        _connection = connection;
        _pipeline = pipeline;
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

        var result = await _pipeline.ExecuteAsync(async ct => await _connection.Run(query, cancellationToken: ct), ct);

        return new ContributionSummary(
            TotalCommits: result.TotalCommitContributions,
            TotalPullRequestsOpened: 0,
            TotalReviewsGiven: result.TotalPullRequestReviewContributions,
            From: from,
            To: to
        );
    }
}
