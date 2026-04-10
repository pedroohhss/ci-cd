# ci-cd Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-10

## Active Technologies
- YAML (GitHub Actions) + `actions/checkout@v4`, `peter-evans/create-pull-request@v6`, `jq` (pre-installed on `ubuntu-latest`) (main)
- `version.json` at repository root (file-based, git-committed) (main)
- YAML (GitHub Actions) + No new actions required — uses built-in GitHub Actions expression syntax (`startsWith`, `github.head_ref`, `github.event.workflow_run.head_branch`, step outputs) (main)
- YAML (GitHub Actions) + `actions/setup-dotnet@v4` (built-in NuGet cache via `cache: 'nuget'` parameter) — no new actions required (main)
- NuGet package cache on GitHub Actions cache storage (managed by GitHub) (main)

- YAML (GitHub Actions), C# / .NET 8 + `actions/checkout@v4`, `actions/setup-dotnet@v4`, `dotnet-sonarscanner` (global tool), `jq` (pre-installed on ubuntu-latest) (001-github-actions-cicd)

## Project Structure

```text
backend/
frontend/
tests/
```

## Commands

# Add commands for YAML (GitHub Actions), C# / .NET 8

## Code Style

YAML (GitHub Actions), C# / .NET 8: Follow standard conventions

## Recent Changes
- main: Added YAML (GitHub Actions) + `actions/setup-dotnet@v4` (built-in NuGet cache via `cache: 'nuget'` parameter) — no new actions required
- main: Added YAML (GitHub Actions) + No new actions required — uses built-in GitHub Actions expression syntax (`startsWith`, `github.head_ref`, `github.event.workflow_run.head_branch`, step outputs)
- main: Added YAML (GitHub Actions) + `actions/checkout@v4`, `peter-evans/create-pull-request@v6`, `jq` (pre-installed on `ubuntu-latest`)


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
