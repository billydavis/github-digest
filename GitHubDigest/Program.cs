using GitHubDigest.Commands;
using GitHubDigest.Renderers;
using GitHubDigest.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OctokitRest = Octokit;
using OctokitGQL = Octokit.GraphQL;

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

services.AddSingleton<OctokitRest.GitHubClient>(_ =>
{
    var client = new OctokitRest.GitHubClient(new OctokitRest.ProductHeaderValue("github-digest"));
    client.Credentials = new OctokitRest.Credentials(token);
    return client;
});

services.AddSingleton<OctokitGQL.Connection>(_ =>
    new OctokitGQL.Connection(new OctokitGQL.ProductHeaderValue("github-digest", "0.1.0"), token));

services.AddSingleton<IGitHubRestClient, GitHubRestClient>();
services.AddSingleton<IGitHubGraphQLClient, GitHubGraphQLClient>();
services.AddSingleton<DigestService>();
services.AddSingleton<TerminalRenderer>();
services.AddSingleton<MarkdownRenderer>();
services.AddSingleton<DigestCommand>();

var provider = services.BuildServiceProvider();

var command = provider.GetRequiredService<DigestCommand>();
return await command.Parse(args).InvokeAsync();
