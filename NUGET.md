# GitHub Digest

Your GitHub morning briefing, straight to the terminal. See all your open pull requests, assigned issues, and contribution stats across every repo in one shot.

```
GitHub Digest · Thu May 8 · billydavis
────────────────────────────────────────

Last 14 days
42 commits  8 PRs opened  14 reviews given

Open pull requests (2)

 Repo              Title                        Review status    Updated 
 ────────────────  ───────────────────────────  ───────────────  ─────── 
 myorg/api-server  Add rate limiting to…        Approved         May 7   
 myorg/frontend    Refactor auth flow           Awaiting review  May 6   

Assigned issues, last 14 days (1)

 Repo              Title                        Labels           Updated 
 ────────────────  ───────────────────────────  ───────────────  ─────── 
 myorg/api-server  Investigate memory leak      bug              May 7   
```

## Requirements

- .NET 9 runtime or later

## Installation

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

Or add it to `appsettings.local.json` next to the installed tool (never commit this file):

```json
{
  "GitHub": {
    "Token": "ghp_your_token_here"
  }
}
```

If the token is missing or invalid, the tool will exit with an error message on startup.

## Usage

```sh
# Default: last 14 days, terminal output
github-digest

# Shorter or longer lookback
github-digest --since 7d
github-digest --since 30d

# Scope to a specific repo or org
github-digest --repo owner/repo-name
github-digest --repo myorg

# Export to a markdown file
github-digest --output markdown

# Output raw JSON for scripting or piping
github-digest --output json
```

### Flags

| Flag | Default | Description |
|------|---------|-------------|
| `--since` | `14d` | Lookback window (e.g. `7d`, `14d`, `30d`) |
| `--repo` | _(all repos)_ | Scope to an org or specific `owner/repo` |
| `--output` | `terminal` | Output format: `terminal`, `markdown`, or `json` |

### Markdown export

`--output markdown` writes a `github-digest-YYYY-MM-DD.md` file in the current directory.

### Exit codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | Error (missing token, API failure, network error) |

Error details are written to stderr, so stdout remains clean for piping.

### JSON output

`--output json` writes the full report to stdout — no spinner, no color codes. Useful for piping into `jq` or other tools:

```sh
github-digest --output json | jq '.openPullRequests[] | select(.reviewStatus == "needsReview")'
```

The JSON shape:

```json
{
  "openPullRequests": [
    {
      "repo": "myorg/api-server",
      "number": 42,
      "title": "Add rate limiting to auth endpoints",
      "url": "https://github.com/myorg/api-server/pull/42",
      "state": "open",
      "createdAt": "2026-04-30T10:00:00+00:00",
      "updatedAt": "2026-05-07T14:23:00+00:00",
      "reviewStatus": "approved",
      "baseRef": "main"
    }
  ],
  "assignedIssues": [
    {
      "repo": "myorg/api-server",
      "number": 88,
      "title": "Investigate memory leak in request handler",
      "url": "https://github.com/myorg/api-server/issues/88",
      "state": "open",
      "createdAt": "2026-05-01T09:00:00+00:00",
      "updatedAt": "2026-05-07T11:00:00+00:00",
      "labels": ["bug"]
    }
  ],
  "contributions": {
    "totalCommits": 42,
    "totalPullRequestsOpened": 8,
    "totalReviewsGiven": 14,
    "from": "2026-04-24T00:00:00+00:00",
    "to": "2026-05-08T00:00:00+00:00"
  },
  "username": "billydavis"
}
```
