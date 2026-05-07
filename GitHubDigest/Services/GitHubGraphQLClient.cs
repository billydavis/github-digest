using GitHubDigest.Models;
using Polly;
using System.Net.Http.Json;
using System.Text.Json;

namespace GitHubDigest.Services;

public interface IGitHubGraphQLClient
{
    Task<ContributionSummary> GetContributionSummaryAsync(int sinceDays, CancellationToken ct = default);
    Task<Dictionary<string, ReviewStatus>> GetReviewDecisionsAsync(IReadOnlyList<PullRequestSummary> prs, CancellationToken ct = default);
}

public class GitHubGraphQLClient : IGitHubGraphQLClient
{
    private readonly ResiliencePipeline _pipeline;
    private readonly HttpClient _http;

    public GitHubGraphQLClient(ResiliencePipeline pipeline, HttpClient http)
    {
        _pipeline = pipeline;
        _http = http;
    }

    public async Task<ContributionSummary> GetContributionSummaryAsync(int sinceDays, CancellationToken ct = default)
    {
        var from = DateTimeOffset.UtcNow.AddDays(-sinceDays);
        var to = DateTimeOffset.UtcNow;

        var payload = new
        {
            query = """
                query($from: DateTime!, $to: DateTime!) {
                  viewer {
                    contributionsCollection(from: $from, to: $to) {
                      totalCommitContributions
                      totalPullRequestContributions
                      totalPullRequestReviewContributions
                    }
                  }
                }
                """,
            variables = new { from = from.ToString("o"), to = to.ToString("o") }
        };

        var response = await _pipeline.ExecuteAsync(async ct =>
        {
            var resp = await _http.PostAsJsonAsync("https://api.github.com/graphql", payload, ct);
            resp.EnsureSuccessStatusCode();
            return resp;
        }, ct);

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var c = doc.RootElement
            .GetProperty("data")
            .GetProperty("viewer")
            .GetProperty("contributionsCollection");

        return new ContributionSummary(
            TotalCommits: c.GetProperty("totalCommitContributions").GetInt32(),
            TotalPullRequestsOpened: c.GetProperty("totalPullRequestContributions").GetInt32(),
            TotalReviewsGiven: c.GetProperty("totalPullRequestReviewContributions").GetInt32(),
            From: from,
            To: to
        );
    }

    public async Task<Dictionary<string, ReviewStatus>> GetReviewDecisionsAsync(
        IReadOnlyList<PullRequestSummary> prs, CancellationToken ct = default)
    {
        var nodeIds = prs
            .Where(pr => pr.NodeId is not null)
            .Select(pr => pr.NodeId!)
            .ToList();

        if (nodeIds.Count == 0)
            return new();

        var payload = new
        {
            query = """
                query($ids: [ID!]!) {
                  nodes(ids: $ids) {
                    ... on PullRequest {
                      id
                      reviewDecision
                    }
                  }
                }
                """,
            variables = new { ids = nodeIds }
        };

        var response = await _pipeline.ExecuteAsync(async ct =>
        {
            var resp = await _http.PostAsJsonAsync("https://api.github.com/graphql", payload, ct);
            resp.EnsureSuccessStatusCode();
            return resp;
        }, ct);

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var nodes = doc.RootElement.GetProperty("data").GetProperty("nodes");

        var result = new Dictionary<string, ReviewStatus>();
        foreach (var node in nodes.EnumerateArray())
        {
            if (!node.TryGetProperty("id", out var idProp)) continue;
            var id = idProp.GetString()!;
            var decision = node.TryGetProperty("reviewDecision", out var dp) ? dp.GetString() : null;
            result[id] = decision switch
            {
                "APPROVED" => ReviewStatus.Approved,
                "CHANGES_REQUESTED" => ReviewStatus.ChangesRequested,
                "REVIEW_REQUIRED" => ReviewStatus.NeedsReview,
                _ => ReviewStatus.NeedsReview
            };
        }

        return result;
    }
}
