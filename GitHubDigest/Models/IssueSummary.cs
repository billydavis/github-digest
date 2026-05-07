namespace GitHubDigest.Models;

public record IssueSummary(
    string Repo,
    int Number,
    string Title,
    string Url,
    string State,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string[] Labels
);
