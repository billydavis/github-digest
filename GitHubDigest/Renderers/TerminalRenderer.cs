using GitHubDigest.Models;
using Spectre.Console;
using System.Text;

namespace GitHubDigest.Renderers;

public class TerminalRenderer
{
    private const int MaxTitleWidth = 48;

    public void Render(DigestReport report, int sinceDays)
    {
        AnsiConsole.WriteLine();
        RenderContributions(report.Contributions, sinceDays);
        RenderPullRequests(report.OpenPullRequests);
        RenderIssues(report.AssignedIssues, sinceDays);
    }

    private static void RenderContributions(ContributionSummary c, int sinceDays)
    {
        AnsiConsole.MarkupLine($"[dim]{SinceLabel(sinceDays)}[/]");
        AnsiConsole.MarkupLine(
            $"[bold green]{c.TotalCommits}[/] [dim]commits[/]  [dim]·[/]  " +
            $"[bold white]{c.TotalPullRequestsOpened}[/] [dim]PRs opened[/]  [dim]·[/]  " +
            $"[bold green]{c.TotalReviewsGiven}[/] [dim]reviews given[/]");
        AnsiConsole.WriteLine();
    }

    private static void RenderPullRequests(IReadOnlyList<PullRequestSummary> prs)
    {
        AnsiConsole.MarkupLine($"[bold white]Open pull requests[/] [dim]({prs.Count})[/]");

        if (prs.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]  No open pull requests.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderStyle(new Style(Color.Grey))
            .AddColumn(new TableColumn("[dim]Repo[/]"))
            .AddColumn(new TableColumn("[dim]Title[/]"))
            .AddColumn(new TableColumn("[dim]Review status[/]"))
            .AddColumn(new TableColumn("[dim]Updated[/]"));

        foreach (var pr in prs.OrderByDescending(p => p.UpdatedAt))
        {
            var (statusText, statusColor) = ReviewStatusParts(pr.ReviewStatus);
            table.AddRow(
                $"[cyan]{Markup.Escape(Sanitize(pr.Repo))}[/]",
                Markup.Escape(Sanitize(pr.Title, MaxTitleWidth)),
                $"[{statusColor}]{Markup.Escape(statusText)}[/]",
                $"[dim]{RelativeDate(pr.UpdatedAt)}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void RenderIssues(IReadOnlyList<IssueSummary> issues, int sinceDays)
    {
        AnsiConsole.MarkupLine($"[bold white]Assigned issues, {SinceLabel(sinceDays).ToLower()}[/] [dim]({issues.Count})[/]");

        if (issues.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]  No assigned issues updated recently.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderStyle(new Style(Color.Grey))
            .AddColumn(new TableColumn("[dim]Repo[/]"))
            .AddColumn(new TableColumn("[dim]Title[/]"))
            .AddColumn(new TableColumn("[dim]Labels[/]"))
            .AddColumn(new TableColumn("[dim]Updated[/]"));

        foreach (var issue in issues.OrderByDescending(i => i.UpdatedAt))
        {
            var labelsMarkup = issue.Labels.Length > 0
                ? string.Join(" ", issue.Labels.Select(LabelBadge))
                : "[dim]-[/]";

            table.AddRow(
                $"[cyan]{Markup.Escape(Sanitize(issue.Repo))}[/]",
                Markup.Escape(Sanitize(issue.Title, MaxTitleWidth)),
                labelsMarkup,
                $"[dim]{RelativeDate(issue.UpdatedAt)}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static (string text, string color) ReviewStatusParts(ReviewStatus status) => status switch
    {
        ReviewStatus.Approved         => ("Approved", "green"),
        ReviewStatus.ChangesRequested => ("Changes requested", "red"),
        ReviewStatus.NeedsReview      => ("Awaiting review", "yellow"),
        _                             => ("Unknown", "dim")
    };

    private static string LabelBadge(string label)
    {
        var l = label.ToLowerInvariant();
        var (fg, bg) = l switch
        {
            _ when l.Contains("bug")                               => ("white", "red3"),
            _ when l.Contains("feature") || l.Contains("enhance") => ("white", "blue"),
            _ when l.Contains("doc")                               => ("white", "grey50"),
            _ when l.Contains("question")                          => ("white", "purple"),
            _ when l.Contains("help")                              => ("black", "green4"),
            _ when l.Contains("urgent") || l.Contains("critical")  => ("white", "red1"),
            _                                                      => ("white", "grey30")
        };
        return $"[{fg} on {bg}] {Markup.Escape(label)} [/]";
    }

    private static string SinceLabel(int days) => days switch
    {
        7 => "This week",
        _ => $"Last {days} days"
    };

    private static string RelativeDate(DateTimeOffset dt)
    {
        var age = DateTimeOffset.UtcNow - dt;
        if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes}m ago";
        if (age.TotalHours < 24) return $"{(int)age.TotalHours}h ago";
        return dt.LocalDateTime.ToString("MMM d");
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
            result = result[..Math.Max(0, maxWidth - 3)].TrimEnd() + "...";
        return result;
    }
}
