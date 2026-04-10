# Contract: Versioning Workflow (PR-Based)

**Workflow name**: `Versioning`
**File**: `.github/workflows/versioning.yml`
**Purpose**: Auto-increment the patch version in `version.json` on every developer-initiated push to main, and open a Pull Request with the change rather than pushing directly to `main`.

---

## Trigger Contract

| Property        | Value                   |
|-----------------|-------------------------|
| Event           | `push`                  |
| Branches        | `main`                  |
| `paths-ignore`  | `['version.json']`      |

**Guarantee**: Triggers on any push to `main` **except** when only `version.json` was changed. This prevents the version bump PR merge from re-triggering the workflow (loop prevention).

---

## Concurrency Contract

```yaml
concurrency:
  group: versioning
  cancel-in-progress: false
```

**Guarantee**: At most one versioning run executes at a time. Additional runs queue and wait (never cancelled). Ensures no two runs read the same version value and produce duplicate version numbers or conflicting PRs.

---

## Inputs

None.

**Implicit inputs**:

| Input            | Source             | Description                                     |
|------------------|--------------------|-------------------------------------------------|
| Repository source | `actions/checkout` | Checkout with write access (for branch push)   |
| Current version  | `version.json`     | The `version` field is read via `jq`           |

---

## Secrets Required

None. Uses `GITHUB_TOKEN` (auto-provided by GitHub Actions).

**Required permissions on `GITHUB_TOKEN`**:

```yaml
permissions:
  contents: write        # Required to push the version bump branch
  pull-requests: write   # Required to open the Pull Request
```

**Note**: PRs opened with `GITHUB_TOKEN` will not automatically trigger other GitHub Actions workflows (e.g., `pr-build-test.yml`). This is a GitHub security restriction. If automated CI on the version bump PR is required, replace the checkout token with a PAT or GitHub App token.

---

## Outputs

| Output              | Type          | Description                                                   |
|---------------------|---------------|---------------------------------------------------------------|
| Version bump branch | Git branch    | `version/bump-X.Y.Z` pushed to origin                        |
| Pull Request        | GitHub PR     | Opened targeting `main`; title `chore: bump version to X.Y.Z` |
| `NEW_VERSION`       | Env var       | Available within the workflow run for downstream steps        |

---

## Jobs

### `bump-version`

| Step                     | Command / Action                                                                                        |
|--------------------------|---------------------------------------------------------------------------------------------------------|
| Checkout                 | `actions/checkout@v4` with `token: ${{ secrets.GITHUB_TOKEN }}`                                        |
| Read & increment version | Shell: read `version.json` with `jq`, split semver, increment `PATCH`, write back, export `NEW_VERSION` |
| Create PR                | `peter-evans/create-pull-request@v6` with `branch`, `title`, `body` parameters                         |

**Version increment logic**:

```
Input:  "MAJOR.MINOR.PATCH"
Output: "MAJOR.MINOR.(PATCH + 1)"

Example: "1.0.5" → "1.0.6"
```

**peter-evans/create-pull-request configuration**:

| Parameter      | Value                                        |
|----------------|----------------------------------------------|
| `token`        | `${{ secrets.GITHUB_TOKEN }}`                |
| `branch`       | `version/bump-${{ env.NEW_VERSION }}`        |
| `commit-message` | `chore: bump version to ${{ env.NEW_VERSION }}` |
| `title`        | `chore: bump version to ${{ env.NEW_VERSION }}` |
| `body`         | `Automated version bump`                     |
| `base`         | `main`                                       |

---

## Success/Failure Semantics

| Outcome      | Condition                                              | Effect                                                             |
|--------------|--------------------------------------------------------|--------------------------------------------------------------------|
| **Success**  | PR created (or updated) with version bump              | New branch `version/bump-X.Y.Z` exists; PR opened targeting main  |
| **Failure**  | `jq` parse error, push rejected, or PR creation error  | Workflow fails; no PR created; manual intervention required        |
| **Queued**   | Another versioning run is in progress                  | Current run waits; runs sequentially after previous completes      |
| **Skipped**  | Push only modified `version.json`                      | Workflow never starts (paths-ignore filter)                        |
| **No-op**    | PR for this version already exists                     | `peter-evans` updates the existing PR; no duplicate PR created     |

---

## Loop Prevention

The `paths-ignore: ['version.json']` filter on the trigger is the primary mechanism. When the version bump PR is merged into `main`, the resulting push only modifies `version.json` and falls entirely within the ignored paths, preventing re-trigger.

**Difference from original workflow**: The original workflow committed directly to `main`, so `paths-ignore` was critical for immediate loop prevention. In the PR-based approach, the loop prevention is the same — the merge of the version bump PR creates a push to `main` that only modifies `version.json`, which is ignored.

---

## Comparison with Previous Versioning Strategy

| Aspect               | Original (direct push)                    | New (PR-based)                              |
|----------------------|-------------------------------------------|---------------------------------------------|
| Branch protection    | Requires bypass rule or disabled          | Compatible — uses PRs                       |
| Loop prevention      | `paths-ignore: ['version.json']`          | Same                                        |
| Auditability         | Commit on main                            | PR + commit on main after merge             |
| 3rd party actions    | None                                      | `peter-evans/create-pull-request@v6`        |
| Merge control        | Automatic                                 | Requires manual PR approval/merge           |
| CI on version bump   | N/A (direct commit)                       | Blocked by `GITHUB_TOKEN` unless PAT used   |
