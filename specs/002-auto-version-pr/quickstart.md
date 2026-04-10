# Quickstart: Automatic Versioning via Pull Request

**Feature**: [spec.md](./spec.md)
**Date**: 2026-04-09

---

## Prerequisites

1. **Branch protection enabled on `main`**: At minimum, require a PR before merging. This is the primary reason for the PR-based approach.
2. **`version.json` present at repository root** with a valid semver string:
   ```json
   { "version": "1.0.0" }
   ```
3. **Workflow permissions**: The repository must allow GitHub Actions to create pull requests. Go to: **Settings → Actions → General → Workflow permissions** → enable "Allow GitHub Actions to create and approve pull requests".

---

## How It Works (End-to-End)

1. Developer merges a PR into `main` (any change except `version.json`).
2. The `Versioning` workflow triggers automatically.
3. `version.json` is read; patch is incremented (`1.0.0` → `1.0.1`).
4. `peter-evans/create-pull-request@v6` creates branch `version/bump-1.0.1`, commits the change, and opens a PR targeting `main`.
5. A team member (or the author) reviews and merges the version bump PR.
6. The merge pushes only `version.json` to `main` → `paths-ignore` prevents re-trigger → no loop.

---

## Testing the Pipeline

### Smoke Test (verify end-to-end)

1. Push any non-`version.json` change to `main` (e.g., update the README or any source file).
2. Navigate to **Actions** tab → find the `Versioning` run → wait for it to complete (~1 minute).
3. Navigate to **Pull Requests** tab → confirm a new PR exists titled `chore: bump version to X.Y.Z`.
4. Open the PR → verify only `version.json` was changed with the patch incremented.
5. Merge the PR → confirm no new `Versioning` run appears in the Actions tab.

### Concurrency Test (verify queuing)

1. Push two commits to `main` in rapid succession (e.g., two separate fast pushes).
2. Navigate to **Actions** tab → confirm both `Versioning` runs are queued, and the second run shows "waiting" until the first completes.
3. After both complete, confirm two PRs exist with sequential versions (e.g., `1.0.1` and `1.0.2`).

---

## Workflow Permissions Required

In the repository settings, ensure the `GITHUB_TOKEN` has the following permissions for the `Versioning` workflow:

| Permission       | Level  | Reason                                      |
|------------------|--------|---------------------------------------------|
| `contents`       | write  | Push the version bump branch                |
| `pull-requests`  | write  | Open the Pull Request                       |

These are declared in the workflow YAML:

```yaml
permissions:
  contents: write
  pull-requests: write
```

---

## Known Limitations

- **CI does not run on version bump PRs**: GitHub blocks workflow triggers from PRs opened by `GITHUB_TOKEN` to prevent recursive CI. The version bump PR will show no status check from `pr-build-test.yml`. This is expected and safe — the PR only changes `version.json`, which has no tests.
  - **Workaround if needed**: Replace `GITHUB_TOKEN` in the checkout step with a PAT or GitHub App token with the same permissions. This enables CI to run on the version bump PR.

- **Unmerged version bump PRs accumulate**: If PRs are not merged promptly, multiple version bump PRs may stack up (e.g., `1.0.1`, `1.0.2`). They should be reviewed and merged in order. `peter-evans/create-pull-request` updates an existing PR for the same branch rather than creating duplicates.

---

## Rollback

To revert to the original direct-push versioning strategy:
1. Remove `permissions.pull-requests: write` from the workflow.
2. Remove the `peter-evans/create-pull-request` step.
3. Restore the original `git commit + git push` step.
4. Ensure `permissions.contents: write` remains.
