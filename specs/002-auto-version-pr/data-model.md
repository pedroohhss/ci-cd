# Data Model: Automatic Versioning via Pull Request

**Feature**: [spec.md](./spec.md)
**Date**: 2026-04-09

---

## Entities

### version.json

**What it represents**: The single source of truth for the application's current semantic version. Lives at the repository root and is committed to git.

**Schema**:

```json
{
  "version": "MAJOR.MINOR.PATCH"
}
```

| Field     | Type   | Constraints                          | Example   |
|-----------|--------|--------------------------------------|-----------|
| `version` | string | Required. Must follow `X.Y.Z` semver | `"1.0.3"` |

**State transitions**:

```
[Current: X.Y.Z]
    |
    | push to main (non-version.json change)
    v
[Pipeline reads X.Y.Z]
    |
    | PATCH = PATCH + 1
    v
[New version: X.Y.(Z+1)]
    |
    | written to version.json on bump branch
    v
[PR merged to main]
    |
    | version.json updated in main
    v
[Current: X.Y.(Z+1)]
```

**Validation rules**:
- `version` field must be present and non-empty
- Must be parseable as three integers separated by dots
- `jq -r '.version'` must return a non-empty string

---

### Version Bump Branch

**What it represents**: A short-lived git branch created by the pipeline for each version increment. Contains exactly one commit modifying `version.json`.

| Attribute      | Value                             |
|----------------|-----------------------------------|
| Naming pattern | `version/bump-X.Y.Z`              |
| Base           | `main` at the time of pipeline run |
| Lifetime       | Until the corresponding PR is merged or closed |
| Modified files | `version.json` only               |

---

### Version Bump PR

**What it represents**: An automatically created Pull Request opened from a Version Bump Branch targeting `main`. Represents a single version increment event.

| Attribute     | Value                                          |
|---------------|------------------------------------------------|
| Source branch | `version/bump-X.Y.Z`                          |
| Target branch | `main`                                         |
| Title pattern | `chore: bump version to X.Y.Z`                |
| Body          | `"Automated version bump"`                     |
| Author        | `github-actions[bot]`                          |
| File changes  | `version.json` (1 file, patch increment only) |

**Idempotency**: If the PR for a given version already exists (e.g., prior unmerged PR), `peter-evans/create-pull-request@v6` updates the existing PR rather than creating a duplicate.
