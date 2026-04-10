# Research: Skip CI Checks for Version Bump PRs

**Date**: 2026-04-10
**Feature**: [spec.md](./spec.md)

---

## Decision 1: Mechanism for Skipping CI — How to Make Required Checks Pass Without Running

**Decision**: Modify `pr-build-test.yml` so the existing `build-test` job always runs to completion (always exits with success), but all heavy steps (checkout, build, test, upload) are conditionally skipped when the PR originates from a `version/bump-*` branch.

**Rationale**: GitHub branch protection enforces required status checks by **job name**. If the job doesn't run at all (due to a workflow-level `paths-ignore` filter or a skipped job `if`), GitHub shows the check as "Expected — Waiting" or "Pending", which blocks the PR. The only way to satisfy a required check without running the actual work is to have the job run quickly and exit with a passing status.

By keeping the job name (`build-test`) unchanged and routing version bump PRs through a lightweight "detect and skip" path, the required check is satisfied in seconds with no actual build work performed.

**Alternatives considered**:

- **`paths-ignore: ['version.json']` on the workflow trigger**: Simple, but means the `build-test` check never runs for version-only PRs → check appears as "missing" → PR is blocked. Ruled out.
- **A separate `ci-pass.yml` workflow with `paths: ['version.json']`**: Creates a new check name (not `build-test`) that branch protection doesn't know about. Doesn't satisfy the existing required check. Ruled out unless the branch protection required checks are also updated to include the new check name — too fragile.
- **Two jobs in one workflow** (`build-test` + `skip-ci`) with mutually exclusive `if` conditions: Only one job runs per PR. Branch protection can't require "either A or B" — it requires all listed checks. Ruled out.
- **`github.actor != 'github-actions[bot]'` condition on the job**: Skips the job entirely for the bot, causing the same "missing check" problem. Ruled out.

---

## Decision 2: Branch Detection Strategy — `github.head_ref` vs. Changed Files

**Decision**: Use `github.head_ref` (the PR's source branch name) to detect version bump PRs. Specifically: `startsWith(github.head_ref, 'version/bump-')`.

**Rationale**: The version bump PRs are always opened from branches named `version/bump-X.Y.Z` (established in feature `002-auto-version-pr`). Branch name detection is:
- Instantly available at job start (no checkout required)
- Zero cost (no git operations)
- Reliable as long as the naming convention is maintained

Checking changed files would require a checkout step first, which defeats the purpose of skipping — we'd need to checkout just to decide whether to skip the checkout.

**Constraint**: This decision couples the skip logic to the `version/bump-*` branch naming convention. If the naming convention changes in the future, this detection must be updated. Documented in Assumptions.

**Alternatives considered**:

- **Checking changed files via GitHub API**: Requires an API call before deciding to skip, adds latency and complexity. No benefit over branch name for this use case.
- **PR label detection**: Would require the `peter-evans/create-pull-request` action to apply a label when creating the PR. Adds another dependency. Not necessary given the reliable branch naming convention.
- **Commit message convention (`[skip ci]`)**: GitHub Actions natively supports `[skip ci]` in commit messages to skip all workflows, but this would skip the workflow entirely, making the check "missing" rather than "passing". Ruled out.

---

## Decision 3: Sonar Workflow — Skip vs. Fast-Pass

**Decision**: Add an `if` condition to the `sonar` job in `sonar-analysis.yml` to skip execution entirely when the triggering build run was for a `version/bump-*` branch.

**Rationale**: The Sonar workflow uses a `workflow_run` trigger, not `pull_request`. This means it does not directly create a "PR required check" in the same way as `pull_request` workflows. The Sonar scanner posts results as commit statuses or check runs on the head SHA, but branch protection typically lists the Sonar check separately. 

If the Sonar job simply doesn't run for version bump branches, the Sonar status check is absent from those PRs — which is acceptable because:
1. Sonar has no content to analyze (no code changed)
2. The `build-test` check (the primary gate) still passes
3. The project can choose whether Sonar is a required branch protection check or an informational one

If Sonar is configured as a required check, a fast-pass approach (same as Decision 1 — run the job, skip all steps) would be needed. The recommended approach is to not require Sonar as a mandatory gate, using it as an informational check instead.

**Alternatives considered**:

- **Fast-pass Sonar job** (same pattern as build-test): Run the sonar job but skip all steps for version bump branches. Adds complexity to the Sonar workflow without clear benefit — Sonar would produce an empty/trivial result.
- **Do nothing to Sonar workflow**: If Sonar is not a required check, the build-test fix alone is sufficient. Sonar would still trigger (since build-test succeeds), run, and analyze an empty diff. This wastes Sonar quota and runner time.

The chosen approach (skip the Sonar job for version bump branches) balances correctness with minimal complexity.

---

## Decision 4: Scope of Detection — Branch Name Only vs. Files Changed

**Decision**: Detection is based solely on the PR's source branch name (`version/bump-*`). A PR that mixes `version.json` with other files but comes from a `version/bump-*` branch would still be treated as a version bump PR (CI skipped).

**Rationale**: The spec assumption states that the version bump PRs are always opened from `version/bump-*` branches and always contain only `version.json`. In practice, if a human opens a PR from a `version/bump-*` branch with additional changes, it would bypass CI — but this is a naming convention violation, not a pipeline failure. The naming convention is enforced by the `peter-evans/create-pull-request` action in feature `002`.

**Risk mitigation**: Document the naming convention convention clearly in `quickstart.md`.

---

## Resolved Clarifications

No `[NEEDS CLARIFICATION]` markers were present. All decisions above were resolvable from the existing workflow files and the conventions established in features `001` and `002`.
