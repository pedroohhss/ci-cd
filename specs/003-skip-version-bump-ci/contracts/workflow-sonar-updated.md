# Contract: Sonar Analysis Workflow (Updated)

**Workflow name**: `Sonar Analysis`
**File**: `.github/workflows/sonar-analysis.yml`
**Purpose**: Run Sonar quality analysis after a successful build pipeline, for PRs that contain real code changes. Skip entirely when the triggering build was for a version bump PR.

---

## Trigger Contract (unchanged)

| Property    | Value                             |
|-------------|-----------------------------------|
| Event       | `workflow_run`                    |
| Workflows   | `["PR - Build & Test"]`           |
| Types       | `completed`                       |

---

## Job: `sonar`

### Updated `if` Condition

```yaml
if: |
  github.event.workflow_run.conclusion == 'success' &&
  !startsWith(github.event.workflow_run.head_branch, 'version/bump-')
```

| Condition                                  | Result           |
|--------------------------------------------|------------------|
| Build succeeded AND not a version bump branch | Sonar runs      |
| Build succeeded AND IS a version bump branch  | Sonar skipped   |
| Build failed (any branch)                     | Sonar skipped   |

---

## Success/Failure Semantics

| Scenario                          | Outcome                                           |
|-----------------------------------|---------------------------------------------------|
| Regular PR — build succeeded      | Sonar runs full analysis; posts results to PR     |
| Regular PR — build failed         | Sonar does not run (unchanged behavior)           |
| Version bump PR — build succeeded | Sonar job is skipped; no analysis performed       |

---

## Branch Protection Note

The Sonar workflow uses `workflow_run` trigger and posts results as commit statuses, not as a direct PR check in the same way as `pull_request` workflows. If Sonar is configured as a required branch protection check:
- For version bump PRs, the Sonar check will be absent (not "failing" — just not present)
- This may still block merge if branch protection requires it with "strict" mode

**Recommendation**: Configure Sonar as an informational check (not a required gate) in branch protection settings. The `build-test` check from `pr-build-test.yml` is the appropriate required gate.

---

## Required Permissions (unchanged)

```yaml
permissions:
  contents: read
  checks: write
  pull-requests: write
  statuses: write
```
