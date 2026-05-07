using GitHubDigest.Models;
using Spectre.Console;
using System.Text;

namespace GitHubDigest.Renderers;

public class TerminalRenderer
{
    private const int MaxTitleWidth = 60;

    // Keeps only printable ASCII + Latin Extended (covers accented Western European chars).
    // Drops CJK, Arabic, emoji, and anything else that breaks terminal column width measurement.
    private static string Sanitize(string text, int maxWidth = int.MaxValue)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text.EnumerateRunes())
        {
            if (Rune.IsControl(c)) continue;
            if (c.Value > 0x024F) continue;           // keep Basic Latin + Latin Extended-A/B only
            sb.Append(c.ToString());
        }
        var result = sb.ToString().Trim();
        if (result.Length > maxWidth)
            result = result[..maxWidth].TrimEnd() + "…";
        return result;
    }
    public void Render(DigestReport report)
    {
        AnsiConsole.Write(new Rule("[bold yellow]GitHub Digest[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        RenderPullRequests(report.OpenPullRequests);
        RenderIssues(report.AssignedIssues);
        RenderContributions(report.Contributions);
    }

    private static void RenderPullRequests(IReadOnlyList<PullRequestSummary> prs)
    {
        AnsiConsole.Write(new Rule("[bold green]Open Pull Requests[/]").LeftJustified().RuleStyle("grey"));

        if (prs.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No open pull requests.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Repo[/]"))
            .AddColumn(new TableColumn("[bold]#[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Title[/]"))
            .AddColumn(new TableColumn("[bold]Review[/]"))
            .AddColumn(new TableColumn("[bold]Updated[/]").RightAligned());

        foreach (var pr in prs.OrderByDescending(p => p.UpdatedAt))
        {
            var reviewMarkup = pr.ReviewStatus switch
            {
                ReviewStatus.Approved => "[green]Approved[/]",
                ReviewStatus.ChangesRequested => "[red]Changes Requested[/]",
                ReviewStatus.NeedsReview => "[yellow]Needs Review[/]",
                ReviewStatus.Merged => "[blue]Merged[/]",
                _ => "[grey]Unknown[/]"
            };

            table.AddRow(
                Markup.Escape(Sanitize(pr.Repo)),
                $"[blue]#{pr.Number}[/]",
                Markup.Escape(Sanitize(pr.Title, MaxTitleWidth)),
                reviewMarkup,
                pr.UpdatedAt.LocalDateTime.ToString("MMM d")
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void RenderIssues(IReadOnlyList<IssueSummary> issues)
    {
        AnsiConsole.Write(new Rule("[bold blue]Assigned Issues[/]").LeftJustified().RuleStyle("grey"));

        if (issues.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No assigned issues updated recently.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Repo[/]"))
            .AddColumn(new TableColumn("[bold]#[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Title[/]"))
            .AddColumn(new TableColumn("[bold]Labels[/]"))
            .AddColumn(new TableColumn("[bold]Updated[/]").RightAligned());

        foreach (var issue in issues.OrderByDescending(i => i.UpdatedAt))
        {
            var labels = issue.Labels.Length > 0
                ? string.Join(", ", issue.Labels.Select(Markup.Escape))
                : "[grey]-[/]";

            table.AddRow(
                Markup.Escape(Sanitize(issue.Repo)),
                $"[blue]#{issue.Number}[/]",
                Markup.Escape(Sanitize(issue.Title, MaxTitleWidth)),
                labels,
                issue.UpdatedAt.LocalDateTime.ToString("MMM d")
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void RenderContributions(ContributionSummary contributions)
    {
        AnsiConsole.Write(new Rule("[bold magenta]Contribution Summary[/]").LeftJustified().RuleStyle("grey"));

        var period = $"{contributions.From:MMM d} – {contributions.To:MMM d, yyyy}";
        AnsiConsole.MarkupLine($"[grey]Period: {period}[/]");
        AnsiConsole.WriteLine();

        var grid = new Grid()
            .AddColumn().AddColumn();

        grid.AddRow("[bold]Commits[/]", $"[green]{contributions.TotalCommits}[/]");
        grid.AddRow("[bold]PRs Opened[/]", $"[green]{contributions.TotalPullRequestsOpened}[/]");
        grid.AddRow("[bold]Reviews Given[/]", $"[green]{contributions.TotalReviewsGiven}[/]");

        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();
    }
}
