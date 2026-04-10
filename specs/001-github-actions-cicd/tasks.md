# Tasks: CI/CD Pipelines with GitHub Actions (.NET + Sonar + Versioning)

**Input**: Design documents from `specs/001-github-actions-cicd/`
**Branch**: `001-github-actions-cicd`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Organization**: Tasks grouped by user story for independent implementation and testing.  
**Tests**: Not requested in spec — no test tasks generated.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US4)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Repository scaffolding and the version file that all stories depend on.

- [ ] T001 Create `.github/workflows/` directory at repository root
- [ ] T002 Create `version.json` at repository root with initial content `{ "version": "1.0.0" }`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: No shared code infrastructure is needed — each workflow is self-contained. The only foundational requirement is that `version.json` (T002) exists before US3/US4 can be tested.

**⚠️ CRITICAL**: Complete Phase 1 before beginning any user story.

**Checkpoint**: `version.json` committed to main → user story phases can begin.

---

## Phase 3: User Story 1 — Automated PR Validation (Priority: P1) 🎯 MVP

**Goal**: Every PR against main automatically triggers a build-and-test pipeline and reports a status check.

**Independent Test**: Open a test PR against main → confirm pipeline starts within 60 seconds → observe green/red status check on the PR → confirm broken code produces a red check.

### Implementation for User Story 1

- [ ] T003 [US1] Create `.github/workflows/pr-build-test.yml` with `pull_request` trigger on branch `main`
- [ ] T004 [US1] Add `actions/checkout@v4` and `actions/setup-dotnet@v4` (dotnet-version: `8.0.x`) steps to `pr-build-test.yml`
- [ ] T005 [US1] Add `dotnet restore`, `dotnet build --no-restore --configuration Release` steps to `pr-build-test.yml`
- [ ] T006 [US1] Add `dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage"` step to `pr-build-test.yml`
- [ ] T007 [US1] Add `actions/upload-artifact@v4` step to upload `TestResults/` directory in `pr-build-test.yml` (required for Sonar pipeline)
- [ ] T008 [US1] Set `permissions: contents: read, checks: write, pull-requests: read` in `pr-build-test.yml`

**Checkpoint**: US1 complete — PRs against main now receive automated build-and-test status checks.

---

## Phase 4: User Story 2 — Code Quality Analysis on PR (Priority: P2)

**Goal**: After the build pipeline succeeds, a Sonar analysis runs automatically and posts quality metrics on the PR.

**Independent Test**: Merge a test PR with a passing build → confirm Sonar pipeline starts → confirm PR receives a Sonar status check and a quality comment → confirm a failing Quality Gate produces a red check.

**Dependency**: US1 must be complete (Sonar's `workflow_run` trigger listens to `"PR - Build & Test"`).

### Implementation for User Story 2

- [ ] T009 [US2] Create `.github/workflows/sonar-analysis.yml` with `workflow_run` trigger on `"PR - Build & Test"` completion, filtered to `conclusion == 'success'`
- [ ] T010 [US2] Add `actions/checkout@v4` with `fetch-depth: 0` (required by Sonar for blame/history) to `sonar-analysis.yml`
- [ ] T011 [US2] Add `actions/setup-dotnet@v4` (dotnet-version: `8.0.x`) step to `sonar-analysis.yml`
- [ ] T012 [US2] Add `actions/download-artifact@v4` step to download `TestResults` artifact from the triggering build run in `sonar-analysis.yml`
- [ ] T013 [US2] Add `dotnet tool install --global dotnet-sonarscanner` step to `sonar-analysis.yml`
- [ ] T014 [US2] Add Sonar `begin` step to `sonar-analysis.yml`: set `/k:"YOUR_PROJECT_KEY"`, `/d:sonar.login`, `/d:sonar.host.url`, `/d:sonar.cs.opencover.reportsPaths=**/coverage.opencover.xml`
- [ ] T015 [US2] Add `dotnet build --configuration Release` step between Sonar begin and end in `sonar-analysis.yml`
- [ ] T016 [US2] Add Sonar `end` step (`dotnet sonarscanner end /d:sonar.login`) to `sonar-analysis.yml`
- [ ] T017 [US2] Set `permissions: contents: read, checks: write, pull-requests: write, statuses: write` in `sonar-analysis.yml`
- [ ] T018 [US2] Replace `YOUR_PROJECT_KEY` placeholder in `sonar-analysis.yml` with the actual SonarQube project key

**Checkpoint**: US2 complete — PRs now receive automated Sonar quality analysis and PR decoration after a successful build.

---

## Phase 5: User Story 3 — Automatic Version Bump on Merge (Priority: P3)

**Goal**: Every merge to main (excluding version.json changes) auto-increments the patch version and commits it back to main. Concurrent merges run sequentially.

**Independent Test**: Merge a PR to main → confirm versioning pipeline runs → confirm `version.json` has incremented patch → confirm no pipeline re-trigger → merge two PRs in quick succession → confirm two sequential, distinct version bumps.

**Dependency**: None on US1/US2 at runtime, but requires `version.json` from Phase 1 (T002).

### Implementation for User Story 3

- [ ] T019 [US3] Create `.github/workflows/versioning.yml` with `push` trigger on branch `main` and `paths-ignore: ['version.json']`
- [ ] T020 [US3] Add `concurrency: group: versioning, cancel-in-progress: false` block to `versioning.yml`
- [ ] T021 [US3] Add `actions/checkout@v4` step with `token: ${{ secrets.GITHUB_TOKEN }}` to `versioning.yml`
- [ ] T022 [US3] Add version-increment step using `jq` to `versioning.yml`: read `version.json`, split semver, increment PATCH, write back, export `NEW_VERSION` to `$GITHUB_ENV`
- [ ] T023 [US3] Add git commit-and-push step to `versioning.yml`: configure `git config user.name/email`, `git add version.json`, `git commit -m "chore: bump version to $NEW_VERSION"`, `git push`
- [ ] T024 [US3] Set `permissions: contents: write` in `versioning.yml`

**Checkpoint**: US3 complete — merges to main now auto-increment the patch version in `version.json` with no manual steps and no infinite loops.

---

## Phase 6: User Story 4 — Version Exposed via API (Priority: P4)

**Goal**: The .NET API reads `version.json` at startup and displays the current version in the Swagger UI.

**Independent Test**: Start the API after a version bump → open `/swagger` → confirm version string matches `version.json`.

**Dependency**: Requires `version.json` (T002) and an existing .NET 8 project with Swashbuckle configured.

### Implementation for User Story 4

- [ ] T025 [P] [US4] Create `VersionModel.cs` in `src/[YourProject]/Models/VersionModel.cs` with `public string Version { get; set; } = "0.0.0";`
- [ ] T026 [P] [US4] Add `version.json` as `<Content CopyToOutputDirectory="Always">` in the API `.csproj` file (adjust relative path to match solution layout)
- [ ] T027 [US4] Add version file reading and DI registration to `Program.cs`: `File.ReadAllText("version.json")` → `JsonSerializer.Deserialize<VersionModel>` → `builder.Services.AddSingleton(version)` (depends on T025, T026)
- [ ] T028 [US4] Inject `VersionModel` into the `SwaggerDoc` call in `Program.cs`: set `Version = version.Version` in `OpenApiInfo` (depends on T027)

**Checkpoint**: US4 complete — deployed API Swagger UI shows the version from `version.json`.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Repository configuration and validation across all stories.

- [ ] T029 [P] Configure repository secrets in GitHub Settings → Actions: add `SONAR_TOKEN` and `SONAR_HOST_URL`
- [ ] T030 [P] Configure branch protection rule on `main`: require PR, require `build-test` status check to pass before merge
- [ ] T031 Validate end-to-end flow per `quickstart.md`: open a test PR, verify all three pipelines trigger correctly, merge and verify version bump
- [ ] T032 [P] Add `coverlet.collector` NuGet package to all test `.csproj` files if not already present (prerequisite for coverage reports in Sonar)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Implicit — just requires Phase 1 completion
- **US1 (Phase 3)**: Depends on Phase 1 only — no code dependencies
- **US2 (Phase 4)**: Depends on Phase 1 + US1 complete (Sonar triggers on `"PR - Build & Test"` workflow name)
- **US3 (Phase 5)**: Depends on Phase 1 only (`version.json` must exist)
- **US4 (Phase 6)**: Depends on Phase 1 only (`version.json` must exist); no dependency on US1–US3
- **Polish (Phase 7)**: Depends on desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: Independent — only needs `.github/workflows/` directory
- **US2 (P2)**: Depends on US1 name (`"PR - Build & Test"`) — must be implemented after US1
- **US3 (P3)**: Independent of US1/US2 — can be implemented in parallel with US1
- **US4 (P4)**: Independent of US1/US2/US3 — can be implemented in parallel with all

### Parallel Opportunities Within Stories

- **US4**: T025 and T026 can run in parallel (different files)
- **Polish**: T029, T030, and T032 can run in parallel (independent configuration tasks)

---

## Parallel Example: US3 + US4 (can run simultaneously after Phase 1)

```
Developer A → Phase 3 (US1): T003 → T004 → T005 → T006 → T007 → T008
Developer B → Phase 5 (US3): T019 → T020 → T021 → T022 → T023 → T024
Developer C → Phase 6 (US4): T025+T026 (parallel) → T027 → T028
```

Then Developer B can start Phase 4 (US2) after Developer A completes Phase 3 (US1).

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001, T002)
2. Complete Phase 3: US1 (T003–T008)
3. **STOP and VALIDATE**: Open a test PR → verify build-and-test pipeline triggers → verify status check appears
4. Optionally configure branch protection (T030) to enforce the check

### Incremental Delivery

1. Phase 1 → Foundation ready
2. Phase 3 (US1) → PRs validated automatically — **MVP shipped**
3. Phase 4 (US2) → Quality metrics on PRs added
4. Phase 5 (US3) → Version bumps automated
5. Phase 6 (US4) → Version visible in Swagger
6. Phase 7 → Repository fully configured

### Single-Developer Sequential Order

T001 → T002 → T003 → T004 → T005 → T006 → T007 → T008 → T032 → T009 → T010 → T011 → T012 → T013 → T014 → T015 → T016 → T017 → T018 → T019 → T020 → T021 → T022 → T023 → T024 → T025 → T026 → T027 → T028 → T029 → T030 → T031

---

## Notes

- [P] tasks = different files, no blocking dependencies
- US2 has a hard dependency on US1 (workflow name reference) — implement US1 first
- US3 and US4 are independent of each other and of US2 — safe to parallelize
- `YOUR_PROJECT_KEY` in T018 must be replaced with the real Sonar project key before the Sonar pipeline is usable
- The `.csproj` path in T026 will vary per solution — check the actual solution layout before implementing
- Commit after each task group or checkpoint to keep the branch clean
