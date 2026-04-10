# Contract: Sonar Analysis Workflow

**Workflow name**: `Sonar Analysis`  
**File**: `.github/workflows/sonar-analysis.yml`  
**Purpose**: Run SonarQube/SonarCloud quality analysis after the build pipeline succeeds and decorate the Pull Request with results.

---

## Trigger Contract

| Property         | Value                                      |
|------------------|--------------------------------------------|
| Event            | `workflow_run`                             |
| Watched workflow | `"PR - Build & Test"`                      |
| Types            | `completed`                                |
| Filter           | `if: github.event.workflow_run.conclusion == 'success'` |

**Guarantee**: Runs only when the build-and-test workflow completes with a `success` conclusion. Does not run on `failure`, `cancelled`, or `skipped`.

---

## Inputs

None (no manual dispatch inputs).

**Implicit inputs**:

| Input                     | Source                        | Description                                     |
|---------------------------|-------------------------------|-------------------------------------------------|
| Source code               | `actions/checkout@v4`         | Checked out at the SHA that triggered the build |
| Coverage reports          | `actions/download-artifact@v4`| Downloaded from the artifact uploaded by the build workflow |
| Triggering workflow run ID | `github.event.workflow_run.id` | Used to download the correct artifact          |

---

## Secrets Required

| Secret           | Description                                           | Required |
|------------------|-------------------------------------------------------|----------|
| `SONAR_TOKEN`    | Authentication token for SonarQube/SonarCloud         | Yes      |
| `SONAR_HOST_URL` | Base URL of the SonarQube instance (or `https://sonarcloud.io`) | Yes |

Both secrets must be configured in the repository's **Settings → Secrets and variables → Actions**.

---

## Configuration Required (non-secret)

| Variable         | Where            | Description                                           |
|------------------|------------------|-------------------------------------------------------|
| `YOUR_PROJECT_KEY` | Hardcoded in YAML | SonarQube project key — unique per project, not secret |
| `sonar.cs.opencover.reportsPaths` | Sonar begin step | Glob pointing to coverage XML files, e.g., `**/coverage.opencover.xml` |

---

## Outputs

| Output              | Type         | Location                   | Consumer                        |
|---------------------|--------------|----------------------------|---------------------------------|
| PR status check     | GitHub Check | PR "Checks" tab            | Branch protection rule (optional) |
| PR comment          | GitHub PR comment | PR "Conversation" tab | Developer (human review)        |
| Sonar dashboard     | External URL | SonarQube/SonarCloud       | Tech leads / quality team       |

---

## Jobs

### `sonar`

| Step                     | Action / Command                                                       |
|--------------------------|------------------------------------------------------------------------|
| Checkout                 | `actions/checkout@v4` with `fetch-depth: 0` (required for Sonar blame) |
| Setup .NET               | `actions/setup-dotnet@v4` with `dotnet-version: '8.0.x'`              |
| Download coverage        | `actions/download-artifact@v4` — downloads `TestResults` from build run |
| Install Sonar scanner    | `dotnet tool install --global dotnet-sonarscanner`                     |
| Sonar Begin              | `dotnet sonarscanner begin /k:"..." /d:sonar.login /d:sonar.host.url /d:sonar.cs.opencover.reportsPaths` |
| Build                    | `dotnet build --configuration Release`                                  |
| Sonar End                | `dotnet sonarscanner end /d:sonar.login="${{ secrets.SONAR_TOKEN }}"`  |

---

## Success/Failure Semantics

| Outcome        | Condition                                   | Effect on PR                            |
|----------------|---------------------------------------------|-----------------------------------------|
| **Success**    | Quality Gate passes                         | Green Sonar status check                |
| **Failure**    | Quality Gate fails OR scanner errors        | Red Sonar status check; optionally blocks merge |
| **Skipped**    | Build pipeline did not succeed              | Sonar does not run; no check reported   |

**Quality Gate enforcement**: Optional. Controlled by a `SONAR_QUALITY_GATE_WAIT=true` parameter or equivalent. Disabled by default; team enables via repository configuration.

---

## Permissions

```yaml
permissions:
  contents: read
  checks: write
  pull-requests: write    # Required to post PR comment/decoration
  statuses: write
```
