# Contract: PR Build & Test Workflow

**Workflow name**: `PR - Build & Test`  
**File**: `.github/workflows/pr-build-test.yml`  
**Purpose**: Validate that every Pull Request targeting main compiles and all tests pass.

---

## Trigger Contract

| Property  | Value                              |
|-----------|------------------------------------|
| Event     | `pull_request`                     |
| Branches  | `main`                             |
| Activity  | `opened`, `synchronize`, `reopened` (GitHub defaults) |

**Guarantee**: Runs on every commit pushed to an open PR against main.

---

## Inputs

None. The workflow uses no `workflow_dispatch` inputs or `inputs:` block.

**Implicit inputs** (from repository context):

| Input                | Source              | Description                                |
|----------------------|---------------------|--------------------------------------------|
| Repository source    | `actions/checkout`  | Full source tree at the PR head SHA        |
| .NET SDK version     | `actions/setup-dotnet` | `8.0.x` (latest patch of .NET 8)        |

---

## Secrets Required

None. This workflow does not access any repository secrets.

---

## Outputs

| Output              | Type            | Location                              | Consumer                    |
|---------------------|-----------------|---------------------------------------|-----------------------------|
| PR status check     | GitHub Check    | PR "Checks" tab                       | Branch protection rule      |
| Coverage reports    | Artifact (files) | `TestResults/**/coverage.opencover.xml` | Sonar workflow (via artifact upload/download) |
| Build result        | Exit code       | GitHub Actions run                    | `workflow_run` trigger in Sonar workflow |

---

## Jobs

### `build-test`

| Step                      | Action / Command                                                   |
|---------------------------|--------------------------------------------------------------------|
| Checkout                  | `actions/checkout@v4`                                              |
| Setup .NET                | `actions/setup-dotnet@v4` with `dotnet-version: '8.0.x'`          |
| Restore                   | `dotnet restore`                                                   |
| Build                     | `dotnet build --no-restore --configuration Release`                |
| Test + Coverage           | `dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage"` |
| Upload coverage artifact  | `actions/upload-artifact@v4` — uploads `TestResults/` directory    |

---

## Success/Failure Semantics

| Outcome        | Condition                                  | Effect on PR                     |
|----------------|--------------------------------------------|----------------------------------|
| **Success**    | All steps exit with code 0                 | Green status check; merge unblocked (if only gate) |
| **Failure**    | Any step exits non-zero                    | Red status check; merge blocked  |
| **Cancelled**  | Superseded by newer commit on same PR      | Neutral; new run starts for latest commit |

---

## Permissions

```yaml
permissions:
  contents: read
  checks: write
  pull-requests: read
```
