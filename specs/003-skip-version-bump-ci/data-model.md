# Data Model: Skip CI Checks for Version Bump PRs

**Feature**: [spec.md](./spec.md)
**Date**: 2026-04-10

---

## Entities

This feature introduces no new data entities. It modifies the behavior of two existing workflow entities.

---

## Modified Workflow Entities

### PR - Build & Test Workflow (`pr-build-test.yml`)

**What it represents**: The primary CI pipeline that validates code quality on Pull Requests. Currently runs unconditionally on every PR against `main`.

**Modified behavior**:

| PR Type | Before | After |
|---|---|---|
| Regular PR (code changes) | Full build + test | Full build + test (unchanged) |
| Version bump PR (`version/bump-*`) | Full build + test | Detect → immediately pass |

**Detection logic**:

```
PR source branch starts with "version/bump-"
  → TRUE:  Skip all build/test steps, exit success
  → FALSE: Run full build and test pipeline
```

**Key constraint**: The job name (`build-test`) MUST remain unchanged. Branch protection requires this exact name to pass.

---

### Sonar Analysis Workflow (`sonar-analysis.yml`)

**What it represents**: Code quality analysis that runs after a successful build. Triggered via `workflow_run` (not directly on PRs).

**Modified behavior**:

| Triggering Build | Before | After |
|---|---|---|
| Regular PR build | Sonar runs | Sonar runs (unchanged) |
| Version bump PR build | Sonar runs (wasted) | Sonar skipped |

**Detection logic**:

```
Triggering workflow_run head branch starts with "version/bump-"
  → TRUE:  Skip Sonar job entirely
  → FALSE: Run full Sonar analysis
```

---

## State Transitions

### Version Bump PR Lifecycle (with this feature)

```
[PR Opened: version/bump-X.Y.Z]
        |
        | pr-build-test.yml triggers
        v
[build-test job starts]
        |
        | Step: detect branch name
        v
[is_version_bump = true]
        |
        | all heavy steps skipped
        v
[build-test job: SUCCESS (fast)]
        |
        | sonar-analysis.yml triggers (workflow_run)
        v
[sonar job: SKIPPED (branch condition false)]
        |
        | PR shows: "build-test ✓ passed"
        v
[PR is mergeable]
```

### Regular PR Lifecycle (unchanged)

```
[PR Opened: feature/my-change]
        |
        | pr-build-test.yml triggers
        v
[build-test job starts]
        |
        | Step: detect branch name
        v
[is_version_bump = false]
        |
        | all steps execute normally
        v
[build-test job: SUCCESS or FAILURE]
        |
        | sonar-analysis.yml triggers (on success)
        v
[sonar job: runs full analysis]
```
