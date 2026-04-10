# Contract: Versioning Workflow

**Workflow name**: `Versioning`  
**File**: `.github/workflows/versioning.yml`  
**Purpose**: Auto-increment the patch version in `version.json` on every developer-initiated push to main, and commit the change back to the branch.

---

## Trigger Contract

| Property      | Value                         |
|---------------|-------------------------------|
| Event         | `push`                        |
| Branches      | `main`                        |
| `paths-ignore` | `['version.json']`           |

**Guarantee**: Triggers on any push to main **except** when only `version.json` was changed. This prevents the auto-commit that updates `version.json` from re-triggering the workflow (loop prevention).

---

## Concurrency Contract

```yaml
concurrency:
  group: versioning
  cancel-in-progress: false
```

**Guarantee**: At most one versioning run executes at a time. Additional runs queue and wait (never cancelled). Ensures no two runs read the same version value and produce duplicate version numbers.

---

## Inputs

None.

**Implicit inputs**:

| Input               | Source              | Description                                      |
|---------------------|---------------------|--------------------------------------------------|
| Repository source   | `actions/checkout`  | Full checkout with write access (for git push)  |
| Current version     | `version.json`      | The `version` field is read via `jq`            |

---

## Secrets Required

None. Uses `GITHUB_TOKEN` (auto-provided by GitHub Actions) for the git push.

**Required permission on `GITHUB_TOKEN`**:

```yaml
permissions:
  contents: write    # Required to push the version bump commit
```

---

## Outputs

| Output              | Type        | Description                                               |
|---------------------|-------------|-----------------------------------------------------------|
| `version.json`      | File (committed) | Updated with incremented patch version             |
| Git commit          | Commit on main | `chore: bump version to X.Y.Z` by `github-actions` bot |
| `NEW_VERSION`       | Env var     | Available within the workflow run for downstream steps    |

---

## Jobs

### `bump-version`

| Step                  | Command / Action                                               |
|-----------------------|----------------------------------------------------------------|
| Checkout              | `actions/checkout@v4` with `token: ${{ secrets.GITHUB_TOKEN }}` |
| Read & increment version | Shell: read `version.json` with `jq`, split semver, increment `PATCH`, write back |
| Commit & push         | `git config`, `git add version.json`, `git commit -m "chore: bump version to $NEW_VERSION"`, `git push` |

**Version increment logic**:

```
Input:  "MAJOR.MINOR.PATCH"
Output: "MAJOR.MINOR.(PATCH + 1)"

Example: "1.0.5" → "1.0.6"
```

---

## Success/Failure Semantics

| Outcome        | Condition                                          | Effect                                              |
|----------------|----------------------------------------------------|-----------------------------------------------------|
| **Success**    | Version incremented and pushed                     | New commit on main; `version.json` updated          |
| **Failure**    | `jq` parse error, git push conflict, or permission issue | Workflow fails; no version change committed; manual intervention required |
| **Queued**     | Another versioning run is in progress              | Current run waits; runs sequentially after previous completes |
| **Skipped**    | Push only modified `version.json`                  | Workflow never starts (paths-ignore filter)         |

---

## Loop Prevention

The `paths-ignore: ['version.json']` filter on the trigger is the primary mechanism. The auto-commit produced by this workflow only modifies `version.json`, so it falls entirely within the ignored paths and does not re-trigger the workflow.

**Secondary safeguard** (optional, belt-and-suspenders):

```yaml
if: github.actor != 'github-actions[bot]'
```

Can be added as a job-level condition if paths-ignore is ever removed or bypassed.
