using GitHubDigest.Demo;
using GitHubDigest.Models;
using GitHubDigest.Renderers;
using GitHubDigest.Services;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace GitHubDigest.Commands;

public class DigestCommand : RootCommand
{
    private readonly Option<string> _sinceOption;
    private readonly Option<string?> _repoOption;
    private readonly Option<string> _outputOption;
    private readonly Option<bool> _demoOption;

    public DigestCommand(DigestService digestService, TerminalRenderer terminalRenderer, MarkdownRenderer markdownRenderer, JsonRenderer jsonRenderer)
        : base("Produce a morning briefing of your GitHub activity")
    {
        _sinceOption = new Option<string>("--since") { DefaultValueFactory = _ => "14d", Description = "Lookback window (e.g. 7d, 14d, 30d)" };
        _repoOption = new Option<string?>("--repo") { Description = "Scope to an org (github) or specific repo (github/gh-aw-threat-detection)" };
        _outputOption = new Option<string>("--output") { DefaultValueFactory = _ => "terminal", Description = "Output format: terminal, markdown, or json" };
        _demoOption = new Option<bool>("--demo") { Description = "Show sample data without calling the GitHub API" };

        Add(_sinceOption);
        Add(_repoOption);
        Add(_outputOption);
        Add(_demoOption);

        this.SetAction(async (parseResult, ct) =>
        {
            var since = parseResult.GetValue(_sinceOption) ?? "14d";
            var repo = parseResult.GetValue(_repoOption);
            var output = parseResult.GetValue(_outputOption) ?? "terminal";
            var demo = parseResult.GetValue(_demoOption);

            var sinceDays = ParseSinceDays(since);
            DigestReport report;

            if (demo)
            {
                report = DemoData.BuildReport(sinceDays);
            }
            else if (output == "json")
            {
                // Skip the spinner so it doesn't pollute stdout
                report = await digestService.BuildReportAsync(sinceDays, repo, ct);
            }
            else
            {
                DigestReport? fetched = null;
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Fetching GitHub data...", async ctx =>
                    {
                        ctx.Status("Fetching pull requests and issues...");
                        fetched = await digestService.BuildReportAsync(sinceDays, repo, ct);
                    });
                report = fetched!;
            }

            if (output == "json")
            {
                Console.WriteLine(jsonRenderer.Render(report));
            }
            else if (output == "markdown")
            {
                var content = markdownRenderer.Render(report);
                var path = $"github-digest-{DateTime.Now:yyyy-MM-dd}.md";
                await File.WriteAllTextAsync(path, content, ct);
                AnsiConsole.MarkupLine($"[green]Digest written to[/] [bold]{path}[/]");
            }
            else
            {
                terminalRenderer.Render(report, sinceDays);
            }
        });
    }

    private static int ParseSinceDays(string since)
    {
        if (since.EndsWith('d') && int.TryParse(since[..^1], out var days))
            return days;

        AnsiConsole.MarkupLine($"[yellow]Unrecognized --since value '{since}', defaulting to 14 days.[/]");
        return 14;
    }
}
