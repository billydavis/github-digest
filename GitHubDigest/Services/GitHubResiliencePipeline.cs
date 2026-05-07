using Octokit;
using Polly;
using Polly.Retry;
using Spectre.Console;

namespace GitHubDigest.Services;

public static class GitHubResiliencePipeline
{
    public static ResiliencePipeline Build() =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<RateLimitExceededException>()
                    .Handle<SecondaryRateLimitExceededException>(),
                MaxRetryAttempts = 3,
                DelayGenerator = static args =>
                {
                    TimeSpan delay = args.Outcome.Exception switch
                    {
                        RateLimitExceededException rle =>
                            rle.Reset - DateTimeOffset.UtcNow + TimeSpan.FromSeconds(2),
                        _ => TimeSpan.FromSeconds(60)
                    };
                    return new ValueTask<TimeSpan?>(delay < TimeSpan.Zero ? TimeSpan.FromSeconds(2) : delay);
                },
                OnRetry = static args =>
                {
                    var seconds = (int)args.RetryDelay.TotalSeconds;
                    var reason = args.Outcome.Exception is RateLimitExceededException
                        ? "primary rate limit"
                        : "secondary rate limit";
                    AnsiConsole.MarkupLine($"[yellow]GitHub {reason} hit — retrying in {seconds}s...[/]");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
}
