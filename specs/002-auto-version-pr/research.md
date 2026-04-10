# Research: Automatic Versioning via Pull Request

**Date**: 2026-04-09
**Feature**: [spec.md](./spec.md)

---

## Decision 1: PR Creation — peter-evans/create-pull-request@v6 vs. gh pr create

**Decision**: Use `peter-evans/create-pull-request@v6` to handle all git operations (branch creation, commit, push, and PR creation) as a single step.

**Rationale**: `peter-evans/create-pull-request` is the most widely adopted community action for this use case (used in thousands of public repositories). It natively handles:
- Detecting file changes in the workspace
- Creating or updating the target branch
- Committing the changed files with a configurable message
- Opening (or updating) the Pull Request against the base branch

This eliminates the need for manual `git checkout -b / git add / git commit / git push` shell steps before the action runs, because the action performs those operations internally. Combining manual git push with the action would cause a conflict: the action expects a clean workspace and manages the branch lifecycle itself. The cleaner approach is to let the action own all git operations after the version increment step.

**Alternatives considered**:
- **Manual git steps + `gh pr create`**: Fully explicit and requires no 3rd-party action. Viable but more verbose. The `gh` CLI is pre-installed on `ubuntu-latest`, so this is technically equivalent. However, `peter-evans/create-pull-request` adds idempotency (if the PR already exists, it updates it rather than failing), which is a meaningful safety property.
- **Manual git steps + `peter-evans` action**: The spec draft showed this pattern (separate create-branch + create-PR steps), but it creates a conflict because `peter-evans` manages the branch internally and would not behave correctly with a pre-pushed branch. This approach is ruled out.

---

## Decision 2: Loop Prevention Strategy

**Decision**: Retain `paths-ignore: ['version.json']` on the `push` trigger (same as the original versioning workflow).

**Rationale**: When a version bump PR is merged into `main`, the resulting push to `main` modifies only `version.json`. The `paths-ignore: ['version.json']` filter prevents this push from re-triggering the versioning pipeline. This is the same mechanism used in the original workflow (Decision 2 in `001-github-actions-cicd/research.md`) and remains the correct approach for the PR-based variant.

**Confirmation**: The loop prevention logic is independent of whether versioning commits directly to main or via PR. In both cases, the final commit touching `version.json` lands on `main` via a push (direct or PR merge), and `paths-ignore` catches it.

**Alternatives considered**:
- **`if: github.actor != 'github-actions[bot]'`**: Still wasteful (starts runner before skipping). Not needed when `paths-ignore` is sufficient.

---

## Decision 3: GITHUB_TOKEN Scope for PR Creation

**Decision**: Use the built-in `GITHUB_TOKEN` with `contents: write` and `pull-requests: write` permissions.

**Rationale**: `peter-evans/create-pull-request@v6` requires `contents: write` to push the version bump branch and `pull-requests: write` to open the PR. Both permissions are available on `GITHUB_TOKEN` without a Personal Access Token (PAT). The original workflow only needed `contents: write` (for direct push); the new permission set adds `pull-requests: write`.

**Important caveat**: GitHub Actions workflows triggered by a PR opened by `GITHUB_TOKEN` will **not** automatically run other workflows (e.g., the `pr-build-test.yml` pipeline) unless the repository uses a PAT or GitHub App token. This is a GitHub security restriction to prevent recursive CI runs. For this project, the version bump PR is expected to be reviewed and merged manually, so this limitation is acceptable. If automated CI on the version bump PR is required in the future, a PAT or GitHub App token would need to replace `GITHUB_TOKEN` for the checkout step.

**Alternatives considered**:
- **Personal Access Token (PAT)**: Enables CI on the version bump PR, but requires managing a separate secret and ties the automation to a personal account. Deferred unless required.
- **GitHub App token**: Most robust long-term solution. Deferred unless CI on the version bump PR is required.

---

## Decision 4: Branch Naming Convention

**Decision**: Use `version/bump-X.Y.Z` as the branch name for each version bump.

**Rationale**: The spec explicitly defines this naming convention. It is consistent with common semver bump branch patterns and clearly identifies the purpose and version at a glance. `peter-evans/create-pull-request` accepts a `branch` parameter to enforce this naming.

**Idempotency note**: If a previous version bump PR for the same version was not merged (e.g., was closed without merging), a new push to `main` would attempt to create the same branch again. `peter-evans/create-pull-request@v6` handles this by updating the existing PR rather than failing. This is a safe behavior.

---

## Resolved Clarifications

All specification items were fully defined. No `[NEEDS CLARIFICATION]` markers were present. Research focused on confirming implementation approach and identifying the loop-prevention and token-scope considerations.
