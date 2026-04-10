# Implementation Plan: Skip CI Checks for Version Bump PRs

**Branch**: `003-skip-version-bump-ci` | **Date**: 2026-04-10 | **Spec**: [spec.md](./spec.md)

## Summary

Modify two existing GitHub Actions workflow files so that Pull Requests opened from `version/bump-*` branches bypass the build-and-test and Sonar analysis steps. The `build-test` job in `pr-build-test.yml` is restructured to detect the branch name and skip all heavy steps for version bump PRs while still exiting with success — satisfying the required branch protection check. The `sonar` job in `sonar-analysis.yml` is updated to skip entirely when triggered by a version bump build.

## Technical Context

**Language/Version**: YAML (GitHub Actions)
**Primary Dependencies**: No new actions required — uses built-in GitHub Actions expression syntax (`startsWith`, `github.head_ref`, `github.event.workflow_run.head_branch`, step outputs)
**Storage**: N/A
**Testing**: Manual trigger inspection (see `quickstart.md`)
**Target Platform**: GitHub Actions `ubuntu-latest` hosted runners
**Project Type**: CI/CD pipeline configuration (YAML)
**Performance Goals**: Version bump PR receives a passing `build-test` check in under 30 seconds (vs. ~3–5 minutes for a full build)
**Constraints**: The `build-test` job name MUST NOT change (branch protection depends on it); detection must occur before any checkout step (no git operations allowed for detection)
**Scale/Scope**: Two workflow files modified; no new files created

## Constitution Check

The `.specify/memory/constitution.md` contains only the uninitialized template. **No gate violations apply.**

Post-design re-check: Still no violations. The design uses only native GitHub Actions expression syntax with no new actions or complexity.

## Project Structure

### Documentation (this feature)

```text
specs/003-skip-version-bump-ci/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── workflow-pr-build-test-updated.md   # Phase 1 output
│   └── workflow-sonar-updated.md           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created here)
```

### Source Code (repository root)

```text
.github/
└── workflows/
    ├── pr-build-test.yml    # MODIFIED: add branch detection + conditional steps
    └── sonar-analysis.yml   # MODIFIED: add branch condition to job if-clause
```

**Structure Decision**: Only two files change. No new workflows are created. No new GitHub Actions actions are introduced.

## Complexity Tracking

No constitution violations to justify.
