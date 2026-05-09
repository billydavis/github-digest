using GitHubDigest.Models;

namespace GitHubDigest.Demo;

internal static class DemoData
{
    public static DigestReport BuildReport(int sinceDays)
    {
        var now = DateTimeOffset.UtcNow;

        var prs = new List<PullRequestSummary>
        {
            new("myorg/api-server",    101, "Add rate limiting to endpoints",  "https://github.com/myorg/api-server/pull/101",    "open", now.AddDays(-5), now.AddDays(-2), ReviewStatus.Approved,        "main", null),
            new("myorg/frontend",       88, "Refactor auth flow",              "https://github.com/myorg/frontend/pull/88",        "open", now.AddDays(-4), now.AddDays(-3), ReviewStatus.NeedsReview,     "main", null),
            new("myorg/data-pipeline",  23, "Migrate Spark jobs to Delta Lake","https://github.com/myorg/data-pipeline/pull/23",   "open", now.AddDays(-8), now.AddDays(-6), ReviewStatus.ChangesRequested, "main", null),
        };

        var issues = new List<IssueSummary>
        {
            new("myorg/api-server",  42, "Investigate memory leak in pool",    "https://github.com/myorg/api-server/issues/42",   "open", now.AddDays(-10), now.AddDays(-2), ["bug"]),
            new("myorg/frontend",    71, "Improve dashboard load on mobile",   "https://github.com/myorg/frontend/issues/71",     "open", now.AddDays(-7),  now.AddDays(-4), ["enhancement", "mobile"]),
            new("myorg/infra",       15, "Terraform plan fails on GCP init",   "https://github.com/myorg/infra/issues/15",        "open", now.AddDays(-3),  now.AddDays(-1), ["bug", "critical"]),
        };

        var contributions = new ContributionSummary(
            TotalCommits: 42,
            TotalPullRequestsOpened: 8,
            TotalReviewsGiven: 14,
            From: now.AddDays(-sinceDays),
            To: now
        );

        return new DigestReport(prs, issues, contributions, "demo-user");
    }
}
