# Quickstart: CI/CD Resilience Fixes

**Feature**: [spec.md](./spec.md)
**Date**: 2026-04-10

---

## Changes Overview

| Workflow | Change | User Story |
|---|---|---|
| `versioning.yml` | Job-level `if` guard: skip if actor is automation bot | US1 |
| `versioning.yml` | `git pull origin main` step before version read | US3 |
| `pr-build-test.yml` | `cache: 'nuget'` on `actions/setup-dotnet@v4` | US2 |

---

## Testing the Changes

### Test 1: Loop Guard (US1)

The loop guard is difficult to test in isolation without simulating the bot actor. Verify indirectly:

1. Merge a version bump PR to `main`
2. Navigate to **Actions → Versioning** — confirm no new `Versioning` run is triggered after the merge
3. The `paths-ignore` is the primary guard; the actor guard is belt-and-suspenders and can be confirmed in the workflow YAML

To verify the actor guard is active: inspect the workflow YAML and confirm the `if: github.actor != 'github-actions[bot]'` condition is present on the `bump-version` job.

### Test 2: Dependency Cache (US2)

1. Open a Pull Request (any code change)
2. Wait for the `PR - Build & Test` pipeline to complete
3. Push a second commit to the same PR (without changing `.csproj` files)
4. Wait for the second `PR - Build & Test` run
5. Compare run times:
   - Run 1: "Restore dependencies" step takes 30–90 seconds (cold cache)
   - Run 2: "Setup .NET" step shows "Cache restored" log line; "Restore dependencies" step takes < 10 seconds
6. Confirm both runs produce the same build and test results

### Test 3: Version Freshness (US3)

This is observable in the pipeline logs:

1. Trigger a push to `main`
2. Navigate to **Actions → Versioning → bump-version job**
3. Expand the "Sync with latest main" step log
4. Confirm the step shows `Already up to date.` or a pull/merge operation — confirming it ran
5. Confirm the "Increment patch version" step reads the correct, current version from `version.json`

---

## Concurrency + Freshness Interaction

With all resilience fixes in place, the full versioning flow under rapid concurrent merges:

```
Merge #1 → versioning run A queued → runs immediately
Merge #2 → versioning run B queued → waits (concurrency)

Run A:
  Checkout (SHA from merge #1)
  git pull origin main → up to date
  Read version: 1.0.0
  Create PR: version/bump-1.0.1
  Run A completes

[Developer merges version/bump-1.0.1 PR]
  → paths-ignore fires → no new versioning run

Run B starts:
  Checkout (SHA from merge #2, which was 1.0.0)
  git pull origin main → fetches 1.0.1 from the merged version bump PR
  Read version: 1.0.1
  Create PR: version/bump-1.0.2
  Run B completes ✓
```

Without the `git pull` step, Run B would read `1.0.0` and create a conflicting PR for `1.0.1` that already exists.
