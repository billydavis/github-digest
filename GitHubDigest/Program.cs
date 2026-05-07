using GitHubDigest.Commands;
using GitHubDigest.Renderers;
using GitHubDigest.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using System.Net.Http.Headers;
using OctokitRest = Octokit;

if (args.Length > 0 && args[0] is "--version" or "--help" or "-?" or "-h")
{
    var meta = new System.CommandLine.RootCommand("Produce a morning briefing of your GitHub activity");
    meta.Add(new System.CommandLine.Option<string>("--since") { DefaultValueFactory = _ => "14d", Description = "Lookback window (e.g. 7d, 14d, 30d)" });
    meta.Add(new System.CommandLine.Option<string?>("--repo") { Description = "Scope to an org (github) or specific repo (github/gh-aw-threat-detection)" });
    meta.Add(new System.CommandLine.Option<string>("--output") { DefaultValueFactory = _ => "terminal", Description = "Output format: terminal, markdown, or json" });
    return await meta.Parse(args).InvokeAsync();
}

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.local.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var token = config["GitHub:Token"];
if (string.IsNullOrWhiteSpace(token))
    throw new InvalidOperationException(
        "GitHub token not configured. Set GITHUB__TOKEN env var or GitHub:Token in appsettings.json.");

var services = new ServiceCollection();

services.AddMemoryCache();
services.AddSingleton<ResiliencePipeline>(_ => GitHubResiliencePipeline.Build());
services.AddSingleton<HttpClient>(_ =>
{
    var http = new HttpClient();
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    http.DefaultRequestHeaders.UserAgent.ParseAdd("github-digest/0.1.0");
    return http;
});

services.AddSingleton<OctokitRest.GitHubClient>(_ =>
{
    var client = new OctokitRest.GitHubClient(new OctokitRest.ProductHeaderValue("github-digest"));
    client.Credentials = new OctokitRest.Credentials(token);
    return client;
});

services.AddSingleton<IGitHubRestClient, GitHubRestClient>();
services.AddSingleton<IGitHubGraphQLClient, GitHubGraphQLClient>();
services.AddSingleton<DigestService>();
services.AddSingleton<TerminalRenderer>();
services.AddSingleton<MarkdownRenderer>();
services.AddSingleton<JsonRenderer>();
services.AddSingleton<DigestCommand>();

var provider = services.BuildServiceProvider();

var command = provider.GetRequiredService<DigestCommand>();
return await command.Parse(args).InvokeAsync();
