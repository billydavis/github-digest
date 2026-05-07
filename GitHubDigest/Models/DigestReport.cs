namespace GitHubDigest.Models;

public record DigestReport(
    IReadOnlyList<PullRequestSummary> OpenPullRequests,
    IReadOnlyList<IssueSummary> AssignedIssues,
    ContributionSummary Contributions,
    string Username
);

public record ContributionSummary(
    int TotalCommits,
    int TotalPullRequestsOpened,
    int TotalReviewsGiven,
    DateTimeOffset From,
    DateTimeOffset To
);
