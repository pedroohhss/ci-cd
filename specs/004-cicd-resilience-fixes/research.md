# Research: CI/CD Resilience Fixes

**Date**: 2026-04-10
**Feature**: [spec.md](./spec.md)

---

## Decision 1: Secondary Loop Guard — Commit Message vs. Actor Identity

**Decision**: Use a job-level `if` condition checking that the triggering commit was not authored by the automation account (`github.actor != 'github-actions[bot]'`) rather than inspecting the commit message text.

**Rationale**: The commit message approach (`!contains(github.event.head_commit.message, 'chore: bump version')`) only works reliably with **squash merges** and **rebase merges**, where the PR commit message becomes the merge commit message. With GitHub's default **merge commit** strategy, the commit message is `"Merge pull request #N from user/version/bump-X.Y.Z"` — which does NOT contain `"chore: bump version"`. The message guard would fail silently for merge-commit repositories, providing false protection.

The actor identity check (`github.actor != 'github-actions[bot]'`) works for all merge strategies because the automated versioning pipeline always commits as `github-actions[bot]`, regardless of how the PR is merged.

**When to apply**: Add as a job-level `if` on the `bump-version` job in `versioning.yml`. The `paths-ignore` trigger filter remains the primary guard; this is the secondary, independent guard.

**Note on `github.actor`**: For push events from a merged PR, `github.actor` reflects the user who merged the PR, not the committer. For direct bot-authored pushes, it reflects the token owner. Since the versioning pipeline commits via `GITHUB_TOKEN` (which appears as `github-actions[bot]`), when the version bump PR is merged by a human, `github.actor` is the human — so the guard would NOT fire. However, the `paths-ignore` filter still fires (the merge only touches `version.json`). The actor guard is a fallback for edge cases where `paths-ignore` is bypassed.

**Revised decision**: Use both the actor check AND a branch-based check together for maximum coverage. If the push comes from a branch named `version/bump-*` (detectable via `github.event.head_commit.message` containing the branch reference, or by checking `github.ref`... but this isn't directly available on push events the same way). 

**Final decision**: Add the job-level `if` using the actor identity: `if: github.actor != 'github-actions[bot]'`. This is the most reliable belt-and-suspenders guard for all merge strategies. The `paths-ignore` remains the primary filter.

**Alternatives considered**:
- **Commit message check (`!contains(...)`)**: Only works reliably with squash/rebase merge. Fragile if the commit message convention changes. Ruled out as the primary guard; acceptable as an additional guard if desired.
- **No secondary guard**: Rely solely on `paths-ignore`. Acceptable for most cases but leaves a gap if `paths-ignore` is ever accidentally removed or the filter is bypassed by a merge strategy edge case.

---

## Decision 2: NuGet Dependency Caching — Integrated Setup vs. Separate Cache Step

**Decision**: Use `actions/setup-dotnet@v4`'s built-in `cache: 'nuget'` parameter to enable NuGet caching, rather than a separate `actions/cache@v4` step.

**Rationale**: `actions/setup-dotnet@v4` (v4+) natively supports NuGet package caching via the `cache: 'nuget'` parameter. It automatically:
- Determines the correct cache path (`~/.nuget/packages`)
- Generates the cache key from all `*.csproj`, `packages.lock.json`, and `global.json` files
- Handles cache save and restore transparently

This is simpler and less error-prone than a manual `actions/cache@v4` step (which requires manually specifying paths, key patterns, and restore keys). The built-in approach is maintained by the GitHub Actions team and stays up to date with NuGet and .NET changes.

**Cache key behavior**:
- Key: `setup-dotnet-${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/packages.lock.json', '**/global.json') }}`
- If key matches: packages restored from cache (fast path)
- If key misses: packages downloaded fresh, new cache entry saved

**Alternatives considered**:
- **`actions/cache@v4` with manual configuration**: More control over cache key and path, but adds complexity. The built-in approach is preferred for .NET projects.
- **No caching**: Every run downloads all packages. Slower but simpler. Ruled out — the performance improvement is significant with no correctness risk.

---

## Decision 3: Version Freshness — `git pull` After Checkout vs. `ref: main` in Checkout

**Decision**: After the checkout step in `versioning.yml`, add an explicit `git pull origin main` step to ensure the working directory reflects the current HEAD of `main` at the time the step runs.

**Rationale**: GitHub Actions `push` event triggers include a specific SHA (the commit that triggered the event). `actions/checkout@v4` checks out that exact SHA. For the first queued run, this is fine. For subsequent queued runs, by the time they start, `main` may have advanced (e.g., the first run's version bump PR was merged). The triggered run's SHA is from before that merge, so checkout alone would produce an outdated `version.json`.

`git pull origin main` fetches and merges the latest from `origin/main` into the local checkout, ensuring the run always reads the most current version regardless of when it was queued.

**Alternative considered — `ref: main` in checkout**:
```yaml
- uses: actions/checkout@v4
  with:
    ref: main
    token: ${{ secrets.GITHUB_TOKEN }}
```
This is equally valid and slightly cleaner (one step instead of two). However, it changes the semantics of the checkout — if `main` has advanced beyond the triggering SHA, we'd be building from a different point than what triggered the run. For versioning, this is desirable; for other pipelines (like build-and-test), it would be inappropriate. Documenting `git pull` as the approach keeps the intent explicit.

**Placement**: The pull must occur after checkout (to have git state) and before the "Increment patch version" step (to read current version.json). The concurrency queue ensures only one run executes at a time, but the pull closes the gap between queue entry and run start.

---

## Decision 4: Scope Confirmation — What Is Already Implemented

Confirmed by reading the current workflow files that the following are already in place:

| Item | File | Status |
|------|------|--------|
| `paths-ignore: ['version.json']` | `versioning.yml` | ✅ Already present |
| `if: github.event.workflow_run.conclusion == 'success'` | `sonar-analysis.yml` | ✅ Already present |
| `concurrency: cancel-in-progress: false` | `versioning.yml` | ✅ Already present |
| `peter-evans/create-pull-request@v6` idempotency | `versioning.yml` | ✅ Feature 002 |
| Version bump PR CI skip | `pr-build-test.yml` | Planned in feature 003 |

This feature adds only the three items not yet addressed: actor-based loop guard, NuGet caching, and version freshness pull.
