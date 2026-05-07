using GitHubDigest.Models;
using System.Text;

namespace GitHubDigest.Renderers;

public class MarkdownRenderer
{
    public string Render(DigestReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# GitHub Digest — {DateTime.Now:MMMM d, yyyy}");
        sb.AppendLine();

        AppendPullRequests(sb, report.OpenPullRequests);
        AppendIssues(sb, report.AssignedIssues);
        AppendContributions(sb, report.Contributions);

        return sb.ToString();
    }

    private static void AppendPullRequests(StringBuilder sb, IReadOnlyList<PullRequestSummary> prs)
    {
        sb.AppendLine("## Open Pull Requests");
        sb.AppendLine();

        if (prs.Count == 0)
        {
            sb.AppendLine("_No open pull requests._");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("| Repo | # | Title | Review | Updated |");
        sb.AppendLine("|------|---|-------|--------|---------|");

        foreach (var pr in prs.OrderByDescending(p => p.UpdatedAt))
        {
            var reviewLabel = pr.ReviewStatus switch
            {
                ReviewStatus.Approved => "✅ Approved",
                ReviewStatus.ChangesRequested => "🔴 Changes Requested",
                ReviewStatus.NeedsReview => "🟡 Needs Review",
                ReviewStatus.Merged => "🔵 Merged",
                _ => "—"
            };

            sb.AppendLine($"| {pr.Repo} | [#{pr.Number}]({pr.Url}) | {pr.Title} | {reviewLabel} | {pr.UpdatedAt:MMM d} |");
        }

        sb.AppendLine();
    }

    private static void AppendIssues(StringBuilder sb, IReadOnlyList<IssueSummary> issues)
    {
        sb.AppendLine("## Assigned Issues");
        sb.AppendLine();

        if (issues.Count == 0)
        {
            sb.AppendLine("_No assigned issues updated recently._");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("| Repo | # | Title | Labels | Updated |");
        sb.AppendLine("|------|---|-------|--------|---------|");

        foreach (var issue in issues.OrderByDescending(i => i.UpdatedAt))
        {
            var labels = issue.Labels.Length > 0 ? string.Join(", ", issue.Labels) : "—";
            sb.AppendLine($"| {issue.Repo} | [#{issue.Number}]({issue.Url}) | {issue.Title} | {labels} | {issue.UpdatedAt:MMM d} |");
        }

        sb.AppendLine();
    }

    private static void AppendContributions(StringBuilder sb, ContributionSummary c)
    {
        sb.AppendLine("## Contribution Summary");
        sb.AppendLine();
        sb.AppendLine($"_Period: {c.From:MMM d} – {c.To:MMM d, yyyy}_");
        sb.AppendLine();
        sb.AppendLine($"- **Commits:** {c.TotalCommits}");
        sb.AppendLine($"- **PRs Opened:** {c.TotalPullRequestsOpened}");
        sb.AppendLine($"- **Reviews Given:** {c.TotalReviewsGiven}");
        sb.AppendLine();
    }
}
