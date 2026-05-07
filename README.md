# GitHub Digest

Your GitHub morning briefing, straight to the terminal. See all your open pull requests, assigned issues, and weekly contribution stats across every repo in one shot.

```
──────────────────────── GitHub Digest ────────────────────────

── Open Pull Requests ──────────────────────────────────────────
╭─────────────────────┬──────┬──────────────────────────┬─────────────┬─────────╮
│ Repo                │    # │ Title                    │ Review      │ Updated │
├─────────────────────┼──────┼──────────────────────────┼─────────────┼─────────┤
│ myorg/api-server    │  #42 │ Add rate limiting to...  │ Approved    │ May 7   │
│ myorg/frontend      │ #101 │ Refactor auth flow       │ Needs Review│ May 6   │
╰─────────────────────┴──────┴──────────────────────────┴─────────────┴─────────╯

── Assigned Issues ─────────────────────────────────────────────
╭─────────────────────┬──────┬──────────────────────────┬────────────┬─────────╮
│ Repo                │    # │ Title                    │ Labels     │ Updated │
├─────────────────────┼──────┼──────────────────────────┼────────────┼─────────┤
│ myorg/api-server    │  #88 │ Investigate memory leak  │ bug        │ May 7   │
╰─────────────────────┴──────┴──────────────────────────┴────────────┴─────────╯

── Contribution Summary ────────────────────────────────────────
Period: Apr 23 – May 7, 2026

Commits        42
PRs Opened      8
Reviews Given  14
```

## Installation

Requires [.NET 9 SDK](https://dotnet.microsoft.com/download) or later.

```sh
dotnet tool install --global GitHubDigest
```

## Setup

Create a GitHub [Personal Access Token](https://github.com/settings/tokens) (classic) with these scopes:

- `repo` — read PRs and issues from private repos
- `read:user` — contribution summary via GraphQL

Then set it as an environment variable:

```sh
# Windows
$env:GITHUB__TOKEN = "ghp_your_token_here"

# macOS/Linux
export GITHUB__TOKEN="ghp_your_token_here"
```

Or add it to `appsettings.local.json` in the tool directory (never commit this file):

```json
{
  "GitHub": {
    "Token": "ghp_your_token_here"
  }
}
```

## Usage

```sh
# Default: last 14 days, terminal output
github-digest

# Shorter or longer lookback
github-digest --since 7d
github-digest --since 30d

# Scope to a single repo
github-digest --repo owner/repo-name

# Export to a markdown file
github-digest --output markdown
```

The `--output markdown` flag writes a `github-digest-YYYY-MM-DD.md` file in the current directory.

## Building from source

```sh
git clone https://github.com/billydavis/github-digest
cd github-digest/GitHubDigest
dotnet run -- --since 7d
```

To pack and install locally:

```sh
dotnet pack
dotnet tool install --global --add-source ./bin/Release GitHubDigest
```

## Tech stack

- [Octokit](https://github.com/octokit/octokit.net) — GitHub REST API
- [Octokit.GraphQL](https://github.com/octokit/octokit.graphql.net) — GitHub GraphQL API
- [Spectre.Console](https://spectreconsole.net) — terminal rendering
- [System.CommandLine](https://github.com/dotnet/command-line-api) — CLI flags
- .NET 9 · packaged as a `dotnet tool`
