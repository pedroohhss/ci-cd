# Contract: PR Build & Test Workflow (Updated)

**Workflow name**: `PR - Build & Test`
**File**: `.github/workflows/pr-build-test.yml`
**Purpose**: Validate code quality on every PR against `main`. For version bump PRs (from `version/bump-*` branches), immediately pass without running build or tests. For all other PRs, run the full build-and-test pipeline.

---

## Trigger Contract

| Property  | Value                   |
|-----------|-------------------------|
| Event     | `pull_request`          |
| Branches  | `main`                  |

No `paths-ignore` filter — the workflow triggers for ALL PRs. Skipping is handled internally per job run, not at the trigger level.

---

## Job: `build-test`

**Job name is unchanged** — branch protection must continue to reference this exact name.

### Step 1: Detect Version Bump Branch (NEW)

| Property  | Value                                                                         |
|-----------|-------------------------------------------------------------------------------|
| Name      | `Check if version bump PR`                                                    |
| Output ID | `check`                                                                       |
| Output    | `is_version_bump` — `"true"` or `"false"`                                    |
| Logic     | `startsWith(github.head_ref, 'version/bump-')` → set `is_version_bump=true` |

**Guarantee**: This step always runs and never fails. It produces the `is_version_bump` output that gates all subsequent steps.

### Steps 2–7: Conditional on `is_version_bump == 'false'`

All existing build and test steps are only executed when `steps.check.outputs.is_version_bump == 'false'`:

| Step                    | Condition                              |
|-------------------------|----------------------------------------|
| Checkout                | `is_version_bump == 'false'`          |
| Setup .NET              | `is_version_bump == 'false'`          |
| Restore dependencies    | `is_version_bump == 'false'`          |
| Build                   | `is_version_bump == 'false'`          |
| Test with coverage      | `is_version_bump == 'false'`          |
| Upload coverage reports | `is_version_bump == 'false'`          |

---

## Success/Failure Semantics

| Scenario                      | Outcome                                                   |
|-------------------------------|-----------------------------------------------------------|
| Regular PR — build passes     | `build-test` job: SUCCESS; all steps ran                  |
| Regular PR — build fails      | `build-test` job: FAILURE; PR blocked from merging        |
| Version bump PR               | `build-test` job: SUCCESS (fast); all heavy steps skipped |

---

## Required Permissions (unchanged)

```yaml
permissions:
  contents: read
  checks: write
  pull-requests: read
```

---

## Branch Protection Compatibility

The `build-test` job name is preserved. Existing branch protection rules requiring `build-test` to pass continue to work correctly for:
- Regular PRs: job runs full pipeline, result reflects actual code quality
- Version bump PRs: job runs and exits quickly with success, allowing merge without build
