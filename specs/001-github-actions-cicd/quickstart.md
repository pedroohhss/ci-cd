# Quickstart: CI/CD Pipelines with GitHub Actions (.NET + Sonar + Versioning)

**Date**: 2026-04-09

---

## Prerequisites

Before the pipelines work end-to-end, complete these one-time setup steps:

### 1. Create `version.json` at the repository root

```json
{
  "version": "1.0.0"
}
```

Commit this file to main before the versioning pipeline runs for the first time.

### 2. Configure Repository Secrets

In **GitHub → Settings → Secrets and variables → Actions → New repository secret**:

| Secret Name      | Value                                                       |
|------------------|-------------------------------------------------------------|
| `SONAR_TOKEN`    | Token from your SonarQube/SonarCloud account               |
| `SONAR_HOST_URL` | e.g., `https://sonarcloud.io` or your self-hosted Sonar URL |

### 3. Set Your Sonar Project Key

Open `.github/workflows/sonar-analysis.yml` and replace `YOUR_PROJECT_KEY` with your actual SonarQube project key. This value is not a secret — it can be committed to the repository.

### 4. Add `coverlet.collector` to Test Projects

For coverage to work, each `.csproj` test project needs:

```xml
<PackageReference Include="coverlet.collector" Version="6.*" />
```

### 5. Add `version.json` to the .NET Build Output

In the `.csproj` of your API project:

```xml
<ItemGroup>
  <Content Include="..\..\version.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    <Link>version.json</Link>
  </Content>
</ItemGroup>
```

Adjust the relative path to match where `version.json` lives relative to the `.csproj`.

### 6. Configure Branch Protection on `main`

In **GitHub → Settings → Branches → Add branch protection rule** for `main`:

- [x] Require a pull request before merging
- [x] Require status checks to pass before merging
  - Add: `build-test` (from the PR pipeline)
  - Add: `sonar` (optional — add only if Quality Gate enforcement is desired)
- [x] Require branches to be up to date before merging

---

## How It Works

### Opening a Pull Request

1. Developer pushes a branch and opens a PR targeting `main`.
2. **`PR - Build & Test`** starts automatically (within ~60 seconds).
3. On success, **`Sonar Analysis`** starts automatically.
4. Both report status checks on the PR. The PR can only be merged if the required checks pass.

### Merging to Main

1. Developer merges the approved PR.
2. **`Versioning`** pipeline starts automatically (because the push to main does not touch `version.json`).
3. The pipeline reads `version.json`, increments the patch version, and pushes a commit back to main.
4. The version bump commit only modifies `version.json`, so the versioning pipeline does **not** re-trigger.

### Viewing the Version in Swagger

Start the .NET API and open `/swagger`. The `Info` section will display the current version from `version.json`.

---

## Workflow Files Summary

| File                                          | Trigger                      | Purpose                        |
|-----------------------------------------------|------------------------------|--------------------------------|
| `.github/workflows/pr-build-test.yml`         | PR opened/updated against main | Build + test + coverage        |
| `.github/workflows/sonar-analysis.yml`        | After build-test succeeds    | Code quality analysis          |
| `.github/workflows/versioning.yml`            | Push to main (not version.json) | Auto-increment patch version  |

---

## Troubleshooting

| Problem                                    | Likely Cause                                         | Fix                                                       |
|--------------------------------------------|------------------------------------------------------|-----------------------------------------------------------|
| Sonar pipeline skipped                     | Build pipeline failed                                | Fix the build first; Sonar only runs after build success  |
| Version not updating after merge           | Merge only changed `version.json`                    | Expected — this is loop prevention working correctly      |
| Coverage not appearing in Sonar            | `coverlet.collector` missing or artifact not uploaded | Add package reference; check upload-artifact step         |
| API fails to start with file-not-found     | `version.json` not copied to output directory        | Add `CopyToOutputDirectory` to `.csproj` (see step 5)    |
| Versioning pipeline fails on git push      | Branch protection or permissions issue               | Ensure `contents: write` permission is set on GITHUB_TOKEN |
| Two merges produced the same version       | Concurrent runs (should not happen with concurrency group) | Verify `concurrency.cancel-in-progress: false` is set  |
