# Feature Specification: CI/CD Resilience Fixes

**Feature Branch**: `004-cicd-resilience-fixes`
**Created**: 2026-04-10
**Status**: Implemented
**Input**: User description: "Fix CI/CD failures related to unnecessary executions, loop risks, incorrect execution conditions, and performance."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Versioning Pipeline Does Not Loop Even Under Edge Cases (Priority: P1)

The versioning pipeline has a primary loop-prevention mechanism (it ignores pushes that only modify the version file). This story adds a secondary, independent safety guard: if the identity of the actor who triggered the push is the automation account (not a human developer), the versioning pipeline skips execution even if the primary filter was bypassed or misconfigured. The system becomes resilient to edge cases that the primary filter alone cannot catch.

**Why this priority**: An infinite loop in the versioning pipeline is the most severe failure mode — it creates an unbounded number of Pull Requests and consumes CI resources continuously until manually stopped. Belt-and-suspenders protection against this is the highest-priority fix.

**Independent Test**: Can be fully tested by confirming the versioning pipeline job has an actor identity condition — observable in the workflow configuration. Smoke test: merge a version bump PR and confirm no new versioning run appears in the pipeline history.

**Acceptance Scenarios**:

1. **Given** a push to `main` is triggered by the automation account (e.g., a bot merging a version bump PR), **When** the versioning pipeline is triggered, **Then** the pipeline detects the automation actor and skips execution without creating a new branch or PR.
2. **Given** a push to `main` is triggered by a human developer, **When** the versioning pipeline is triggered, **Then** the pipeline proceeds normally and creates a version bump PR.
3. **Given** both the file-path filter and the actor identity guard are active, **When** a version bump PR is merged to `main`, **Then** neither filter alone nor the combination produces a subsequent pipeline run.

---

### User Story 2 - Build Pipeline Runs Faster Due to Dependency Caching (Priority: P2)

When a developer opens or updates a Pull Request, the build-and-test pipeline caches project dependencies between runs. If the dependencies have not changed since the last cached run, they are restored from cache instead of being downloaded and installed again. This reduces redundant network traffic and shortens pipeline execution time.

**Why this priority**: Faster feedback loops directly benefit every developer who opens a PR. Caching is a pure improvement with no risk to correctness — if the cache is stale, it is simply not used and dependencies are re-downloaded.

**Independent Test**: Can be fully tested by opening a PR twice (or pushing a second commit to an existing PR without changing dependency definitions) and observing that the second build completes faster than the first, with cache restore logged in the pipeline output.

**Acceptance Scenarios**:

1. **Given** a PR is opened for the first time (no existing cache), **When** the build pipeline runs, **Then** dependencies are downloaded and a fresh cache is saved for future runs.
2. **Given** a PR is updated with a new commit that does not change dependency definitions, **When** the build pipeline runs, **Then** dependencies are restored from cache and the dependency-restore step completes in under 10 seconds.
3. **Given** the dependency definition files are modified in a PR commit, **When** the build pipeline runs, **Then** the cache is invalidated and dependencies are re-downloaded fresh.
4. **Given** the cache is restored successfully, **When** the build step runs, **Then** the build output is identical to a non-cached run — caching must not affect build correctness.

---

### User Story 3 - Versioning Pipeline Always Reads the Most Current Version (Priority: P3)

Before reading the version file to determine the next version number, the versioning pipeline ensures it has the absolute latest version of the repository's default branch. This prevents scenarios where two rapid merges to the main branch could result in the pipeline reading an outdated version and producing duplicate or conflicting version numbers.

**Why this priority**: This is a consistency fix for high-concurrency scenarios. The concurrency queue (already in place) serializes pipeline runs, but within a single run, the checkout might be slightly stale if the branch advanced between trigger and execution. This fix closes that gap.

**Independent Test**: Can be fully tested by confirming that the pipeline's checkout step always fetches the latest state of the main branch immediately before reading the version file — observable in the pipeline logs showing a sync operation before the version read.

**Acceptance Scenarios**:

1. **Given** a new commit was pushed to `main` after the versioning pipeline was triggered but before it reached the version-read step, **When** the version read occurs, **Then** it reads the version from the most recent state of `main`, not from the state at trigger time.
2. **Given** two pipeline runs are queued (via the concurrency mechanism), **When** the second run starts after the first completes and merges its PR, **Then** the second run reads the version that includes the first run's bump (not the pre-first-run version).
3. **Given** no other commits have occurred since the pipeline was triggered, **When** the sync step runs, **Then** it completes without error and the version read proceeds normally.

---

### Edge Cases

- What happens if the cache storage is full or unavailable? The pipeline should fall back to downloading dependencies without failing the build.
- What happens if the sync step fails (e.g., due to a network issue or permissions)? Should the pipeline proceed with the potentially stale checkout, or fail immediately?
- What if a developer accidentally uses the automation account identity when pushing? The versioning pipeline would skip — an extremely rare edge case with no loop risk, only a missed version bump that would be caught on the next developer push.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The versioning pipeline MUST check the identity of the actor who triggered the push and skip execution if the actor is the automation account, regardless of other filter mechanisms.
- **FR-002**: The automation account identity used for the actor check MUST be the same account that performs automated operations in the repository (the CI/CD system's service account).
- **FR-003**: The build-and-test pipeline MUST cache project dependencies between runs, keyed on the dependency definition files.
- **FR-004**: When cached dependencies are available and current, the build pipeline MUST restore them from cache instead of re-downloading.
- **FR-005**: Cache invalidation MUST occur automatically when dependency definition files change.
- **FR-006**: Dependency caching MUST NOT affect build correctness — the build output with cached dependencies MUST be identical to the output without cache.
- **FR-007**: Before reading the version file, the versioning pipeline MUST synchronize with the latest state of the main branch.
- **FR-008**: The sync step MUST complete successfully before the version read proceeds; if the sync fails, the pipeline MUST fail fast rather than proceed with a potentially stale version.

### Key Entities

- **Automation Actor**: The identity of the CI/CD service account that performs automated operations (e.g., opening version bump PRs). Used as the secondary loop-prevention signal — the versioning pipeline skips execution when this identity is detected as the trigger actor.
- **Dependency Cache**: A stored snapshot of downloaded project dependencies, keyed by the dependency definition file contents. Restored at the start of each build run when available and current.
- **Dependency Definition Files**: The files that specify project dependencies (e.g., project files listing package references). Their content hash determines cache validity.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 0 instances of infinite versioning loops, including scenarios where the primary file-path filter was bypassed.
- **SC-002**: Build pipeline dependency-restore step completes in under 10 seconds on cache-hit runs (vs. 30–120 seconds on cache-miss runs).
- **SC-003**: 100% of versioning pipeline runs read the most current version from the main branch at the time of the version-read step.
- **SC-004**: Cache hit rate reaches 80% or higher after the first 5 pipeline runs on the same dependency set.
- **SC-005**: 0 build failures caused by stale or corrupt cached dependencies — the cache falls back gracefully to a fresh download on any cache error.

## Assumptions

- The versioning pipeline already has a primary loop-prevention mechanism (`paths-ignore` on `version.json`). This spec adds a secondary, independent guard — not a replacement.
- The secondary guard uses actor identity detection (not commit message matching). The actor identity approach works across all git merge strategies (merge commit, squash, rebase), unlike a commit message check which only works reliably with squash/rebase merges.
- SC-004 (cache hit rate ≥80%) and SC-005 (0 failures from stale cache) are runtime observation targets, not buildable artifacts. Cache fallback on error is handled internally by the dependency setup tooling — no explicit fallback code is required in the pipeline configuration.
- The sync step's fail-fast behavior (FR-008) is handled implicitly: if the sync command exits with a non-zero code, the pipeline job fails automatically per standard CI behavior.
- The build pipeline runs on ephemeral runners where dependencies are not persisted between runs by default — caching is an explicit opt-in step.
- The concurrency queue (already in place) serializes versioning runs; the sync step addresses a narrower gap where the checkout could be slightly stale within a single run.
- The Sonar pipeline already has an existing condition that prevents it from running when the build pipeline fails — this is verified to be in place and is out of scope for this spec.
- Items already addressed by prior features (branch-exists handling via `peter-evans/create-pull-request@v6` idempotency, `paths-ignore` for version.json, and test-skipping for version bump PRs) are out of scope here.
