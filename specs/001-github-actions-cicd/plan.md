# Implementation Plan: CI/CD Pipelines with GitHub Actions (.NET + Sonar + Versioning)

**Branch**: `001-github-actions-cicd` | **Date**: 2026-04-09 | **Spec**: [spec.md](./spec.md)

## Summary

Implement three GitHub Actions workflows for a .NET 8 project: (1) a PR validation pipeline that builds and tests on every pull request, (2) a Sonar quality analysis pipeline that runs after the build pipeline succeeds and decorates the PR with metrics, and (3) a versioning pipeline that auto-increments the patch version in `version.json` on every merge to main. Additionally, integrate the version file into the .NET API's Swagger documentation.

## Technical Context

**Language/Version**: YAML (GitHub Actions), C# / .NET 8  
**Primary Dependencies**: `actions/checkout@v4`, `actions/setup-dotnet@v4`, `dotnet-sonarscanner` (global tool), `jq` (pre-installed on ubuntu-latest)  
**Storage**: `version.json` at repository root (file-based, git-committed)  
**Testing**: `dotnet test` with `--collect:"XPlat Code Coverage"` (Coverlet)  
**Target Platform**: GitHub Actions ubuntu-latest hosted runners  
**Project Type**: CI/CD pipeline configuration (YAML) + .NET web service integration (C#)  
**Performance Goals**: PR build+test pipeline completes in < 5 minutes (SC-001); Sonar analysis available within 10 minutes of build completion (SC-002)  
**Constraints**: Versioning pipeline must run sequentially (concurrency group, `cancel-in-progress: false`); must not trigger itself after committing the version bump  
**Scale/Scope**: Single .NET 8 solution; single repository; GitHub-hosted runners only

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The `.specify/memory/constitution.md` file contains only the uninitialized template — no project-specific principles have been ratified for this repository. **No gate violations apply.** If a constitution is added in the future, re-run this check.

Post-design re-check (after Phase 1): Still no violations. The design uses only standard GitHub Actions patterns with no unusual complexity.

## Project Structure

### Documentation (this feature)

```text
specs/001-github-actions-cicd/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── workflow-pr-build-test.md
│   ├── workflow-sonar.md
│   └── workflow-versioning.md
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created here)
```

### Source Code (repository root)

```text
.github/
└── workflows/
    ├── pr-build-test.yml        # FR-001, FR-002, FR-003
    ├── sonar-analysis.yml       # FR-004, FR-005, FR-006
    └── versioning.yml           # FR-007 through FR-011

version.json                     # Single source of truth for version (FR-008)

# Inside the existing .NET project (path varies per solution):
src/
└── [Project]/
    ├── Models/
    │   └── VersionModel.cs      # FR-012 — reads version.json
    └── Program.cs               # FR-012 — injects version into Swagger
```

**Structure Decision**: This is a CI/CD infrastructure feature. All pipeline logic lives in `.github/workflows/`. The .NET version integration is a small addition to the existing project structure (one model class + minimal Program.cs change). No new top-level src folders are introduced.

## Complexity Tracking

No constitution violations to justify.
