# Implementation Plan: CI/CD Resilience Fixes

**Branch**: `004-cicd-resilience-fixes` | **Date**: 2026-04-10 | **Spec**: [spec.md](./spec.md)

## Summary

Apply three targeted hardening changes to two existing workflow files: (1) add a secondary loop-prevention guard to the versioning pipeline using the triggering actor's identity; (2) enable NuGet package caching in the build pipeline for faster dependency restore; (3) add a `git pull origin main` step to the versioning pipeline to ensure the most current version is read even for queued runs.

## Technical Context

**Language/Version**: YAML (GitHub Actions)
**Primary Dependencies**: `actions/setup-dotnet@v4` (built-in NuGet cache via `cache: 'nuget'` parameter) — no new actions required
**Storage**: NuGet package cache on GitHub Actions cache storage (managed by GitHub)
**Testing**: Manual pipeline observation (see quickstart.md)
**Target Platform**: GitHub Actions `ubuntu-latest` hosted runners
**Project Type**: CI/CD pipeline configuration (YAML)
**Performance Goals**: Dependency-restore step under 10 seconds on cache-hit runs (SC-002)
**Constraints**: No new external actions introduced; actor guard must not break regular developer PR flows; `git pull` must occur before version read, after checkout
**Scale/Scope**: Two workflow files modified; no new files created

## Constitution Check

The `.specify/memory/constitution.md` contains only the uninitialized template. **No gate violations apply.**

Post-design re-check: Still no violations. All changes use native GitHub Actions features and the standard `actions/setup-dotnet@v4` built-in caching.

## Project Structure

### Documentation (this feature)

```text
specs/004-cicd-resilience-fixes/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created here)
```

No contracts/ directory — this feature modifies existing internal pipelines only (no new external interfaces exposed).

### Source Code (repository root)

```text
.github/
└── workflows/
    ├── versioning.yml       # MODIFIED: +actor guard on job, +git pull step
    └── pr-build-test.yml    # MODIFIED: +cache: 'nuget' on setup-dotnet step
```

**Structure Decision**: Two files, three targeted changes. No new workflows, no new actions.

## Complexity Tracking

No constitution violations to justify.
