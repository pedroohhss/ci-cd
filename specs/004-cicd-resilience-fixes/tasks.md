# Tasks: CI/CD Resilience Fixes

**Input**: Design documents from `specs/004-cicd-resilience-fixes/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, quickstart.md ✓

**Note**: Three targeted edits across two workflow files. No new files, no new actions, no tests (manual pipeline observation is the validation strategy per quickstart.md).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify current workflow state before making changes.

- [x] T001 Read and confirm current state of `.github/workflows/versioning.yml` — verify `paths-ignore: ['version.json']` is present, `concurrency` block is intact, and there is currently NO actor guard or `git pull` step
- [x] T002 [P] Read and confirm current state of `.github/workflows/pr-build-test.yml` — verify `actions/setup-dotnet@v4` does NOT currently have `cache: 'nuget'` parameter

---

## Phase 2: Foundational (Blocking Prerequisites)

No foundational phase needed — all three user stories are independent of each other and affect different concerns (two affect `versioning.yml`, one affects `pr-build-test.yml`). US1 and US3 both modify `versioning.yml` and must be applied sequentially to avoid conflicts; US2 is fully independent.

---

## Phase 3: User Story 1 — Versioning Loop Belt-and-Suspenders Guard (Priority: P1) 🎯 MVP

**Goal**: Add a secondary, independent loop-prevention guard to the versioning pipeline so that even if the primary `paths-ignore` filter were bypassed, the pipeline will not run for bot-authored commits.

**Independent Test**: Verify the `bump-version` job in `.github/workflows/versioning.yml` has `if: github.actor != 'github-actions[bot]'` at the job level. Smoke test: merge a version bump PR and confirm no new Versioning run appears in the Actions tab.

### Implementation for User Story 1

- [x] T003 [US1] Add `if: github.actor != 'github-actions[bot]'` as a job-level condition on the `bump-version` job in `.github/workflows/versioning.yml` — insert it immediately after the `bump-version:` job declaration line

**Checkpoint**: US1 complete — secondary loop guard is in place. ✅

---

## Phase 4: User Story 2 — Build Pipeline Dependency Caching (Priority: P2)

**Goal**: Enable NuGet package caching in the build-and-test pipeline so dependency restore uses cached packages on cache-hit runs, reducing the step from ~60 seconds to under 10 seconds.

**Independent Test**: Open a PR, wait for first build (cold cache), push a second commit without changing `.csproj` files, wait for second build — confirm "Cache restored" appears in the Setup .NET step log and the restore step is significantly faster.

### Implementation for User Story 2

- [x] T004 [P] [US2] Add `cache: 'nuget'` parameter to the `actions/setup-dotnet@v4` step in `.github/workflows/pr-build-test.yml` (inside the `with:` block, alongside `dotnet-version: '8.0.x'`)

**Checkpoint**: US2 complete — NuGet caching enabled. US1 and US2 can be applied in parallel (different files). ✅

---

## Phase 5: User Story 3 — Version Freshness Guarantee (Priority: P3)

**Goal**: Ensure the versioning pipeline always reads the latest `version.json` from `main` — even for queued runs where the checkout SHA may predate a previous run's version bump merge.

**Independent Test**: Inspect the `bump-version` job logs after a run — confirm a "Sync with latest main" step appears between `Checkout` and `Increment patch version` and shows either `Already up to date.` or a pull/merge operation.

### Implementation for User Story 3

- [x] T005 [US3] Add a `Sync with latest main` step to `.github/workflows/versioning.yml` immediately after the `Checkout` step and before the `Increment patch version` step:
  ```yaml
  - name: Sync with latest main
    run: git pull origin main
  ```

**Checkpoint**: US3 complete — version freshness guaranteed for queued runs. ✅

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T006 Run smoke test per `specs/004-cicd-resilience-fixes/quickstart.md` — merge a version bump PR and confirm no new Versioning run fires; also push a non-dependency PR and confirm cache is used on re-run

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — run T001 and T002 in parallel
- **US1 (Phase 3)**: Depends on T001 (read versioning.yml first)
- **US2 (Phase 4)**: Depends on T002 (read pr-build-test.yml first); independent of US1
- **US3 (Phase 5)**: Depends on T003 (US1 must complete first — both modify the same file `versioning.yml`)
- **Polish (Phase 6)**: Depends on all user stories complete

### User Story Dependencies

- **US1 (P1)** and **US2 (P2)**: Fully independent — different files, can be done in parallel
- **US3 (P3)**: Must come after US1 — same file (`versioning.yml`), avoid edit conflicts

### Within Each User Story

- Each story is a single focused edit — no internal sequencing needed

---

## Parallel Example

```bash
# T001 and T002 can run in parallel (read-only, different files):
Task T001: "Read versioning.yml"
Task T002: "Read pr-build-test.yml"

# T003 and T004 can run in parallel (different files, reads done):
Task T003: "Add actor guard to versioning.yml"     → US1
Task T004: "Add cache: 'nuget' to pr-build-test.yml" → US2
# Then sequentially:
Task T005: "Add git pull step to versioning.yml"   → US3 (same file as T003)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. T001 — read versioning.yml
2. T003 — add actor guard
3. Smoke test: merge version bump PR → confirm no loop

### Incremental Delivery

1. T001 + T002 in parallel → both files read
2. T003 + T004 in parallel → US1 (loop guard) + US2 (cache) applied
3. T005 → US3 (version freshness) applied
4. T006 → smoke test all three

---

## Notes

- T003 and T005 both modify `versioning.yml` — apply them sequentially, not simultaneously
- T004 is a one-line addition inside an existing `with:` block in `pr-build-test.yml`
- T003 is a one-line addition at the job level in `versioning.yml`
- T005 is a 2-line YAML block inserted between two existing steps in `versioning.yml`
- All changes are additive — no existing lines are deleted
