namespace GitHubDigest.Models;

public record PullRequestSummary(
    string Repo,
    int Number,
    string Title,
    string Url,
    string State,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    ReviewStatus ReviewStatus,
    string? BaseRef
);

public enum ReviewStatus
{
    NeedsReview,
    ChangesRequested,
    Approved,
    Merged,
    Unknown
}
