# Feature Specification: Skip CI Checks for Version Bump PRs

**Feature Branch**: `003-skip-version-bump-ci`
**Created**: 2026-04-10
**Status**: Draft
**Input**: User description: "A PR específica do version bump não deve ter que passar pela obrigatoriedade de build e sonar novamente."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Version Bump PR Merges Without Build or Sonar Gates (Priority: P1)

The automated version bump Pull Request — which only changes the version number file and contains no application code — should be able to be merged into `main` without being blocked by the mandatory build-and-test or Sonar quality analysis gates. Since no functional code was changed, running these checks brings no value and unnecessarily delays the merge.

**Why this priority**: This is the entire purpose of the feature. Without it, every version bump requires either a human to manually bypass the checks or waiting for a full CI run on a change that cannot possibly break anything.

**Independent Test**: Can be fully tested by opening a Pull Request that only modifies the version number file and verifying that the PR shows a passing status and can be merged without the build or Sonar checks running or being required.

**Acceptance Scenarios**:

1. **Given** a Pull Request is opened that only modifies the version number file, **When** the PR is created, **Then** the build-and-test gate is not required and does not block the merge.
2. **Given** a Pull Request is opened that only modifies the version number file, **When** the PR is created, **Then** the Sonar analysis gate is not required and does not block the merge.
3. **Given** the version bump PR is in a mergeable state, **When** a reviewer approves (if required), **Then** the PR can be merged immediately without waiting for build or Sonar results.
4. **Given** the version bump PR is opened, **When** the CI system evaluates it, **Then** a visible, passing status check appears on the PR (not a missing check), confirming it was intentionally skipped — not forgotten.

---

### User Story 2 - Regular PRs Are Unaffected (Priority: P2)

All other Pull Requests that contain application code changes continue to require the build-and-test and Sonar analysis checks before merging. The skip behavior is exclusive to version-only PRs.

**Why this priority**: If the change accidentally removes CI requirements from regular developer PRs, it defeats the purpose of the CI pipelines. This story protects the integrity of the existing gates.

**Independent Test**: Can be fully tested by opening a PR that modifies any file other than the version number file and confirming the full build-and-test and Sonar pipelines are triggered and remain mandatory.

**Acceptance Scenarios**:

1. **Given** a Pull Request contains changes to application code (any file other than the version number file), **When** the PR is created or updated, **Then** the build-and-test pipeline triggers and is required to pass before merge.
2. **Given** a Pull Request contains changes to application code, **When** the build pipeline completes, **Then** the Sonar analysis pipeline is triggered and is required to pass before merge.
3. **Given** a Pull Request contains changes to both application code and the version number file, **When** the PR is created, **Then** the full build and Sonar checks are required (treated as a regular PR).

---

### Edge Cases

- What happens when a version bump PR is somehow modified to also include application code changes after being opened (e.g., a follow-up commit is added)?
- How is the system's behavior communicated to developers so they understand why the version bump PR has no build check? (Visible confirmation in the PR is expected — see Acceptance Scenario 4 of US1.)
- What if a future pipeline is added that runs on all PRs — does this feature's logic extend to it automatically, or does it require explicit configuration per pipeline?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST NOT require the build-and-test pipeline to pass for Pull Requests that exclusively modify the version number file.
- **FR-002**: The system MUST NOT require the Sonar quality analysis pipeline to pass for Pull Requests that exclusively modify the version number file.
- **FR-003**: The system MUST display a visible, passing status check on version-number-only PRs to confirm the skip was intentional — no status check should appear as "pending" or "missing".
- **FR-004**: The system MUST continue to require the build-and-test and Sonar pipelines for all Pull Requests that modify any file other than the version number file.
- **FR-005**: The system MUST treat PRs that modify both the version number file and other files as regular PRs subject to full CI requirements.
- **FR-006**: The skip behavior MUST be automatic — no developer or reviewer should need to manually bypass, label, or approve the skip on each version bump PR.
- **FR-007**: The skip behavior MUST be scoped exclusively to the version bump PRs and MUST NOT affect any other automated or manual PRs.

### Key Entities

- **Version Number File**: The single file at the repository root that stores the current semantic version. Only modifications to this file trigger the skip behavior.
- **Version Bump PR**: An automatically created Pull Request whose entire content is a single change to the version number file. Subject to the CI skip behavior defined in this feature.
- **Regular PR**: Any Pull Request opened by a developer that modifies application code or any file other than the version number file. Subject to full CI requirements.
- **Intentional Skip Check**: A synthetic, automatically passing status check that appears on version bump PRs to confirm the CI skip was deliberate and the PR is safe to merge.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of version bump PRs (containing only the version number file change) can be merged without a build or Sonar check blocking them.
- **SC-002**: 0% of regular developer PRs lose their build or Sonar requirements as a result of this change.
- **SC-003**: Every version bump PR shows at least one passing status check confirming the skip is intentional — zero version bump PRs show a "missing" or "pending" required check.
- **SC-004**: The skip behavior requires zero manual actions per PR — no labeling, no manual bypass, no reviewer override.

## Assumptions

- The version number file is `version.json` at the repository root. Only changes to this exact file trigger the skip logic.
- The version bump PRs are always opened from a predictably named branch (e.g., `version/bump-*`), which can serve as an identifier if file-path detection alone is insufficient.
- Branch protection rules currently require the build-and-test and Sonar pipelines to pass before any PR can be merged; this requirement must remain in place for all non-version-bump PRs.
- The project uses the same CI pipeline infrastructure established in feature `001-github-actions-cicd` (two pipelines: build-and-test and Sonar analysis).
- A PR that includes changes to `version.json` alongside other files is treated as a regular PR — the skip is only valid when `version.json` is the sole changed file.
