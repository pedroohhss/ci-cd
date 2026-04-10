# Tasks: Automatic Versioning via Pull Request

**Input**: Design documents from `specs/002-auto-version-pr/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**Note**: This feature modifies a single file — `.github/workflows/versioning.yml`. Tasks are granular to map each user story to its verifiable step in the workflow.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to
- No test tasks — feature has no unit-testable code; validation is done via smoke testing in quickstart.md

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm prerequisites are in place before touching the workflow file.

- [ ] T001 Verify `version.json` exists at repository root and contains a valid semver string (e.g., `{ "version": "1.0.0" }`)
- [ ] T002 Enable "Allow GitHub Actions to create and approve pull requests" in repository **Settings → Actions → General → Workflow permissions** (manual step — required for `pull-requests: write` to work)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Apply the one structural change that all user stories depend on — adding `pull-requests: write` permission to `.github/workflows/versioning.yml`.

**⚠️ CRITICAL**: US1, US2, and US3 all require this to be in place.

- [x] T003 Add `pull-requests: write` to the `permissions` block in `.github/workflows/versioning.yml` so the token can open Pull Requests (currently only has `contents: write`)

**Checkpoint**: Permission block updated — user story implementation can now begin.

---

## Phase 3: User Story 1 — Push Triggers Version Bump via PR (Priority: P1) 🎯 MVP

**Goal**: On any push to `main` (excluding `version.json` changes), the pipeline increments the patch version and opens a Pull Request with the change instead of pushing directly to `main`.

**Independent Test**: Push any non-`version.json` file to `main`. Verify in the Actions tab that the `Versioning` workflow completes, and in the Pull Requests tab that a new PR titled `chore: bump version to X.Y.Z` appears with only `version.json` changed.

### Implementation for User Story 1

- [x] T004 [US1] Remove the `Commit version bump` step (the `git config / git add / git commit / git push` block) from `.github/workflows/versioning.yml` — this step is replaced entirely by the peter-evans action
- [x] T005 [US1] Add `peter-evans/create-pull-request@v6` step to `.github/workflows/versioning.yml` after the `Increment patch version` step, with the following configuration:
  - `token: ${{ secrets.GITHUB_TOKEN }}`
  - `branch: version/bump-${{ env.NEW_VERSION }}`
  - `commit-message: "chore: bump version to ${{ env.NEW_VERSION }}"`
  - `title: "chore: bump version to ${{ env.NEW_VERSION }}"`
  - `body: "Automated version bump"`
  - `base: main`

**Checkpoint**: User Story 1 complete — pushing to `main` now opens a PR instead of committing directly.

---

## Phase 4: User Story 2 — Concurrent Pushes Are Queued (Priority: P2)

**Goal**: Multiple rapid pushes to `main` trigger versioning runs that execute sequentially, preventing version collisions.

**Independent Test**: Trigger two fast pushes to `main` in succession. In the Actions tab, confirm the second `Versioning` run shows "Waiting" until the first completes, and two distinct PRs are opened (e.g., `1.0.1` then `1.0.2`).

### Implementation for User Story 2

- [x] T006 [US2] Verify the `concurrency` block in `.github/workflows/versioning.yml` is intact and correct after the T003–T005 edits:
  ```yaml
  concurrency:
    group: versioning
    cancel-in-progress: false
  ```
  If it was accidentally removed during editing, restore it.

**Checkpoint**: Concurrency confirmed — sequential queuing guaranteed.

---

## Phase 5: User Story 3 — Auditable Version History via PRs (Priority: P3)

**Goal**: Every version increment is traceable as a distinct Pull Request with a clear title, single-file diff, and automated author.

**Independent Test**: After several merges to `main`, review the Pull Requests list and confirm each version bump is a separate PR with title `chore: bump version to X.Y.Z`, body `Automated version bump`, author `github-actions[bot]`, and only `version.json` changed.

### Implementation for User Story 3

- [x] T007 [US3] Confirm the `peter-evans/create-pull-request@v6` step (added in T005) does NOT include any extra files beyond `version.json` in the commit — verify the step does not use `add-paths` pointing to other directories or files
- [x] T008 [US3] Confirm loop prevention is intact: verify `.github/workflows/versioning.yml` still has `paths-ignore: ['version.json']` under the `push` trigger — this prevents the pipeline from re-triggering when the version bump PR is merged to `main`

**Checkpoint**: All three user stories are independently functional and auditable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validation, documentation alignment, and cleanup.

- [ ] T009 Run smoke test from `specs/002-auto-version-pr/quickstart.md` — push a trivial change to `main`, confirm PR appears within 2 minutes, merge it, confirm no re-trigger
- [x] T010 [P] Update `specs/001-github-actions-cicd/contracts/workflow-versioning.md` to note it is superseded by `specs/002-auto-version-pr/contracts/workflow-versioning-pr.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — blocks all user stories
- **US1 (Phase 3)**: Depends on Phase 2
- **US2 (Phase 4)**: Depends on Phase 2; can run in parallel with US1 (different concern — verify existing block)
- **US3 (Phase 5)**: Depends on US1 completion (T005 must exist before T007/T008 verify it)
- **Polish (Phase 6)**: Depends on all user stories complete

### User Story Dependencies

- **US1 (P1)**: Core implementation — all other stories verify or extend it
- **US2 (P2)**: Independent of US1's implementation; checks a pre-existing block
- **US3 (P3)**: Depends on US1 (verifies that peter-evans config is correct and loop prevention is in place)

### Within Each User Story

- T004 (remove old step) must complete before T005 (add new step) — same file
- T007 and T008 (US3) can run in parallel after T005

---

## Parallel Example: US3

```bash
# After T005 is done, launch T007 and T008 together:
Task T007: "Verify peter-evans step has no extra files"
Task T008: "Verify paths-ignore: ['version.json'] is present"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001, T002)
2. Complete Phase 2: Foundational (T003)
3. Complete Phase 3: User Story 1 (T004, T005)
4. **STOP and VALIDATE**: Push to `main`, confirm PR appears → MVP delivered

### Incremental Delivery

1. Setup + Foundational → permission in place
2. US1 → PR-based versioning works → **MVP**
3. US2 → concurrency verified (minimal effort — just a check)
4. US3 → audit properties confirmed + loop prevention verified
5. Polish → smoke test + doc cleanup

---

## Notes

- All implementation is in a single file: `.github/workflows/versioning.yml`
- US2 and US3 are primarily verification tasks — the concurrency block and `paths-ignore` already exist and just need to be preserved through the edits
- The `peter-evans/create-pull-request@v6` action handles all git operations (branch, commit, push, PR) — no manual `git` shell commands are needed in the new step
- PRs opened by `GITHUB_TOKEN` will not trigger `pr-build-test.yml` CI — this is expected and safe (only `version.json` changes)
- Commit after T003 and again after T005 to keep changes reviewable
