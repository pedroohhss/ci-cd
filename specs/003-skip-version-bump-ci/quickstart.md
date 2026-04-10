# Quickstart: Skip CI Checks for Version Bump PRs

**Feature**: [spec.md](./spec.md)
**Date**: 2026-04-10

---

## How It Works

When a Pull Request is opened from a branch starting with `version/bump-*`:

1. `pr-build-test.yml` triggers (same as always)
2. The `build-test` job starts and immediately checks the branch name
3. Since the branch matches `version/bump-*`, all build/test steps are skipped
4. The job exits with **success** in seconds
5. The required status check `build-test` is satisfied ✓
6. `sonar-analysis.yml` triggers (because build "succeeded") but skips its `sonar` job due to the branch condition
7. The PR is now mergeable without having run a full build or Sonar analysis

---

## Testing the Changes

### Test 1: Version Bump PR — CI Should Skip

1. Create a branch `version/bump-99.0.0` locally (or let the versioning pipeline create it)
2. Modify only `version.json` on that branch
3. Open a PR from `version/bump-99.0.0` → `main`
4. Navigate to **Actions** tab → find the `PR - Build & Test` run for this PR
5. **Confirm**: The `build-test` job completed quickly (< 30 seconds) with a green check
6. **Confirm**: The steps "Setup .NET", "Build", "Test with coverage" show as **Skipped** in the job log
7. **Confirm**: The PR shows `build-test ✓` as a passing status check and is mergeable
8. Navigate to **Actions** tab → `Sonar Analysis` — **Confirm**: no Sonar run was triggered for this PR (or the job was skipped)

### Test 2: Regular PR — CI Should Run Normally

1. Create a branch `feature/test-ci-gate` with a trivial code change
2. Open a PR from `feature/test-ci-gate` → `main`
3. **Confirm**: The `build-test` job runs the full pipeline — all steps execute (Checkout, Setup .NET, Restore, Build, Test, Upload)
4. **Confirm**: Sonar analysis triggers after build completes

### Test 3: Concurrent Version Bump — Verify No Interference

1. Trigger two fast merges to `main` (from the versioning pipeline) to produce two version bump PRs
2. **Confirm**: Both PRs get a fast-passing `build-test` check independently
3. **Confirm**: Regular open PRs (if any) are unaffected

---

## Naming Convention Warning

The skip logic relies on the PR's source branch starting with `version/bump-`. If the branch naming convention in `002-auto-version-pr` is ever changed, the detection logic in both workflows must be updated to match.

**Do not open PRs from `version/bump-*` branches that contain code changes** — those PRs will bypass the CI checks even though they contain real code.

---

## Branch Protection Settings

Ensure branch protection on `main` is configured as:

- **Required status checks**: `build-test` (from `PR - Build & Test` workflow)
- **Sonar**: Recommended as **informational only** (not required), since it is absent on version bump PRs

If Sonar is currently a required check and must remain so, a fast-pass Sonar pattern (similar to the build-test approach) would need to be added to `sonar-analysis.yml`.
