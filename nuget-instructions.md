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

## Before publishing, sort out

- **RepositoryUrl** in the csproj points to `github.com/billy-davis/github-digest` — make sure the repo is public and the URL is correct
- **README packaging** — NuGet shows a blank page without one. Add to csproj:
  ```xml
  <PackageReadmeFile>README.md</PackageReadmeFile>
  ```
  And include the file in the pack:
  ```xml
  <None Include="..\README.md" Pack="true" PackagePath="\" />
  ```
- **Token setup instructions** — the README needs clear steps for new users to configure their PAT (`GITHUB__TOKEN` env var or `appsettings.json`). This is the main friction point for first-time installs.
