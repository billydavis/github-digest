using GitHubDigest.Models;
using Spectre.Console;
using System.Text;

namespace GitHubDigest.Renderers;

public class TerminalRenderer
{
    private const int MaxTitleWidth = 60;

    public void Render(DigestReport report, int sinceDays)
    {
        AnsiConsole.MarkupLine(
            $"[bold white]GitHub Digest[/] [grey]·[/] [white]{DateTime.Now:ddd MMM d}[/] [grey]·[/] [yellow]{Markup.Escape(report.Username)}[/]");
        AnsiConsole.Write(new Rule().RuleStyle("grey dim"));
        AnsiConsole.WriteLine();

        RenderContributions(report.Contributions, sinceDays);
        RenderPullRequests(report.OpenPullRequests, sinceDays);
        RenderIssues(report.AssignedIssues, sinceDays);
    }

    private static void RenderContributions(ContributionSummary c, int sinceDays)
    {
        AnsiConsole.MarkupLine($"[yellow]{SinceLabel(sinceDays)}[/]");
        AnsiConsole.MarkupLine(
            $"[bold]{c.TotalCommits}[/] [grey]commits[/]  " +
            $"[bold]{c.TotalPullRequestsOpened}[/] [grey]PRs opened[/]  " +
            $"[bold]{c.TotalReviewsGiven}[/] [grey]reviews given[/]");
        AnsiConsole.WriteLine();
    }

    private static void RenderPullRequests(IReadOnlyList<PullRequestSummary> prs, int sinceDays)
    {
        AnsiConsole.MarkupLine($"[yellow]Open pull requests ({prs.Count})[/]");
        AnsiConsole.WriteLine();

        if (prs.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No open pull requests.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderStyle(new Style(Color.Grey))
            .AddColumn(new TableColumn("[grey]Repo[/]"))
            .AddColumn(new TableColumn("[grey]Title[/]"))
            .AddColumn(new TableColumn("[grey]Review status[/]"))
            .AddColumn(new TableColumn("[grey]Updated[/]"));

        foreach (var pr in prs.OrderByDescending(p => p.UpdatedAt))
        {
            var reviewMarkup = pr.ReviewStatus switch
            {
                ReviewStatus.Approved => "[green]Approved[/]",
                ReviewStatus.ChangesRequested => "[red]Changes requested[/]",
                ReviewStatus.NeedsReview => "[yellow]Awaiting review[/]",
                ReviewStatus.Merged => "[blue]Merged[/]",
                _ => "[grey]Unknown[/]"
            };

            table.AddRow(
                $"[cyan]{Markup.Escape(Sanitize(pr.Repo))}[/]",
                Markup.Escape(Sanitize(pr.Title, MaxTitleWidth)),
                reviewMarkup,
                RelativeTime(pr.UpdatedAt)
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void RenderIssues(IReadOnlyList<IssueSummary> issues, int sinceDays)
    {
        AnsiConsole.MarkupLine($"[yellow]Assigned issues, {SinceLabel(sinceDays).ToLower()} ({issues.Count})[/]");
        AnsiConsole.WriteLine();

        if (issues.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No assigned issues updated recently.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderStyle(new Style(Color.Grey))
            .AddColumn(new TableColumn("[grey]Repo[/]"))
            .AddColumn(new TableColumn("[grey]Title[/]"))
            .AddColumn(new TableColumn("[grey]Labels[/]"))
            .AddColumn(new TableColumn("[grey]Updated[/]"));

        foreach (var issue in issues.OrderByDescending(i => i.UpdatedAt))
        {
            var labelsMarkup = issue.Labels.Length > 0
                ? string.Join(", ", issue.Labels.Select(l => $"[{LabelColor(l)}]{Markup.Escape(l)}[/]"))
                : "[grey]-[/]";

            table.AddRow(
                $"[cyan]{Markup.Escape(Sanitize(issue.Repo))}[/]",
                Markup.Escape(Sanitize(issue.Title, MaxTitleWidth)),
                labelsMarkup,
                RelativeTime(issue.UpdatedAt)
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static string SinceLabel(int days) => days switch
    {
        7 => "This week",
        _ => $"Last {days} days"
    };

    private static string RelativeTime(DateTimeOffset dt)
    {
        var age = DateTimeOffset.UtcNow - dt;
        if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes}m ago";
        if (age.TotalHours < 24) return $"{(int)age.TotalHours}h ago";
        if (age.TotalDays < 2) return "yesterday";
        if (age.TotalDays < 7) return $"{(int)age.TotalDays} days ago";
        return dt.LocalDateTime.ToString("MMM d");
    }

    private static string LabelColor(string label)
    {
        var l = label.ToLowerInvariant();
        if (l.Contains("high") || l.Contains("critical") || l.Contains("urgent")) return "red";
        if (l.Contains("medium") || l.Contains("moderate")) return "yellow";
        if (l.Contains("low")) return "grey";
        return "white";
    }

    private static string Sanitize(string text, int maxWidth = int.MaxValue)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text.EnumerateRunes())
        {
            if (Rune.IsControl(c)) continue;
            if (c.Value > 0x024F) continue;
            sb.Append(c.ToString());
        }
        var result = sb.ToString().Trim();
        if (result.Length > maxWidth)
            result = result[..maxWidth].TrimEnd() + "…";
        return result;
    }
}
