# Data Model: CI/CD Resilience Fixes

**Feature**: [spec.md](./spec.md)
**Date**: 2026-04-10

---

## Modified Workflow Entities

This feature introduces no new data entities. It hardens three existing workflow behaviors.

---

### Versioning Workflow (`versioning.yml`) — Two Changes

**Change A: Secondary Loop Guard**

New job-level condition added to the `bump-version` job:

| Condition | Before | After |
|---|---|---|
| Any push to main | Job runs | Job runs only if `github.actor != 'github-actions[bot]'` |
| Push by automation bot | Job runs (loop risk) | Job skipped immediately |
| Push by developer | Job runs | Job runs (unchanged) |

**Change B: Version Freshness**

New step added between `Checkout` and `Increment patch version`:

| Step | Position | Purpose |
|---|---|---|
| `Checkout` | 1 | Checks out the triggering SHA (may be stale for queued runs) |
| `Sync with latest main` | 2 (NEW) | Fetches and merges current `origin/main` into working directory |
| `Increment patch version` | 3 | Reads `version.json` — now guaranteed to be current |

---

### Build & Test Workflow (`pr-build-test.yml`) — One Change

**Change: NuGet Package Cache**

New configuration on the `Setup .NET` step:

| Parameter | Before | After |
|---|---|---|
| `dotnet-version` | `'8.0.x'` | `'8.0.x'` (unchanged) |
| `cache` | Not set (no caching) | `'nuget'` (caching enabled) |

**Cache behavior**:

```
First run (cold cache):
  → Packages downloaded fresh
  → Cache stored keyed on dependency definition file hashes

Subsequent runs (warm cache, same dependencies):
  → Packages restored from cache (~seconds vs. ~minutes)
  → Build proceeds with cached packages

Subsequent runs (dependency files changed):
  → Cache key mismatch → packages re-downloaded
  → New cache entry saved for future runs
```

**Cache key**: Derived from all project dependency definition files (`.csproj`, lock files). Changes to any dependency definition file invalidate the cache.
