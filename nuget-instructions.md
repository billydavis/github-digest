# Publishing to NuGet.org

## Prerequisites

- NuGet.org account — create one at https://nuget.org
- API key — nuget.org → Account → API Keys → Create, scoped to `GitHubDigest`

## Steps

1. **Bump the version** in `GitHubDigest/GitHubDigest.csproj`:
   ```xml
   <Version>0.1.0</Version>
   ```

2. **Pack:**
   ```sh
   cd GitHubDigest
   dotnet pack -c Release -o ./nupkg
   ```

3. **Push:**
   ```sh
   dotnet nuget push ./nupkg/GitHubDigest.0.1.0.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json
   ```

4. **Anyone can then install it:**
   ```sh
   dotnet tool install --global GitHubDigest
   github-digest --since 7d
   ```

## Before publishing

- **RepositoryUrl** in the csproj points to `github.com/billydavis/github-digest` — make sure the repo is public before publishing.

## Already done

- **README packaging** — `NUGET.md` is wired up via `<PackageReadmeFile>` and included in the pack via `<None Pack="true">`. NuGet.org will display it on the package page.
- **Token setup instructions** — `NUGET.md` covers PAT creation, env var setup, and the error shown when the token is missing.
- **Package tags** — `<PackageTags>` is set for NuGet.org search discoverability.
