# Implementation Plan: Automatic Versioning via Pull Request

**Branch**: `002-auto-version-pr` | **Date**: 2026-04-09 | **Spec**: [spec.md](./spec.md)

## Summary

Replace the existing direct-push versioning workflow with a PR-based approach. On every push to `main` (excluding `version.json` changes), the pipeline reads the current version from `version.json`, increments the patch component, and uses `peter-evans/create-pull-request@v6` to automatically open a Pull Request targeting `main` with the bumped version. This approach respects branch protection rules that require PRs for all changes to `main`.

## Technical Context

**Language/Version**: YAML (GitHub Actions)
**Primary Dependencies**: `actions/checkout@v4`, `peter-evans/create-pull-request@v6`, `jq` (pre-installed on `ubuntu-latest`)
**Storage**: `version.json` at repository root (file-based, git-committed)
**Testing**: Manual trigger inspection + PR creation verification
**Target Platform**: GitHub Actions `ubuntu-latest` hosted runners
**Project Type**: CI/CD pipeline configuration (YAML)
**Performance Goals**: Version bump PR opened within 2 minutes of triggering push (SC-001)
**Constraints**: Pipeline must run sequentially (concurrency group, `cancel-in-progress: false`); must not trigger itself after the version bump PR is merged; must not push directly to `main`
**Scale/Scope**: Single repository; single `version.json` at root

## Constitution Check

The `.specify/memory/constitution.md` file contains only the uninitialized template — no project-specific principles have been ratified for this repository. **No gate violations apply.**

Post-design re-check (after Phase 1): Still no violations. The design uses a well-established community action (`peter-evans/create-pull-request@v6`) and standard GitHub Actions patterns with no unusual complexity.

## Project Structure

### Documentation (this feature)

```text
specs/002-auto-version-pr/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── workflow-versioning-pr.md   # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created here)
```

### Source Code (repository root)

```text
.github/
└── workflows/
    └── versioning.yml    # MODIFIED: replace direct-push with PR-based approach

version.json              # Unchanged — same schema, same file
```

**Structure Decision**: Only one file changes — `.github/workflows/versioning.yml`. The `version.json` schema and location are unchanged. No new top-level directories are introduced.

## Complexity Tracking

No constitution violations to justify.
