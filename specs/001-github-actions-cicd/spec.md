# Feature Specification: CI/CD Pipelines with GitHub Actions (.NET + Sonar + Versioning)

**Feature Branch**: `001-github-actions-cicd`  
**Created**: 2026-04-09  
**Status**: Draft  

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Automated PR Validation (Priority: P1)

A developer opens a Pull Request targeting the main branch. The system automatically triggers a build and test pipeline that validates the code compiles successfully and all tests pass, providing early feedback before any human review begins.

**Why this priority**: This is the foundational quality gate. Without it, broken code can reach main. It blocks all other stories and delivers the highest immediate value by catching regressions automatically.

**Independent Test**: Can be fully tested by opening a PR against main and observing that the pipeline starts, runs build and tests, and reports a pass/fail status back to the PR — without any other pipeline being active.

**Acceptance Scenarios**:

1. **Given** a developer opens a PR against main, **When** the PR is created or updated with new commits, **Then** the build-and-test pipeline starts automatically within 60 seconds.
2. **Given** the build succeeds and all tests pass, **When** the pipeline completes, **Then** the PR shows a green status check and the developer is notified.
3. **Given** the build fails or a test fails, **When** the pipeline completes, **Then** the PR shows a failed status check, blocking merge, and the failure details are visible to the developer.
4. **Given** a PR is updated with a new commit, **When** the push occurs, **Then** the pipeline re-runs automatically for the latest commit.

---

### User Story 2 - Code Quality Analysis on PR (Priority: P2)

After the build-and-test pipeline succeeds on a PR, a code quality analysis pipeline runs automatically using SonarQube, posts analysis results as comments on the Pull Request, and optionally blocks merge if quality gates are not met.

**Why this priority**: Adds a governance layer on top of basic build validation. Depends on P1 completing successfully, so it is sequentially second in value.

**Independent Test**: Can be fully tested by ensuring the Sonar pipeline triggers after the build pipeline completes on a PR, and that quality metrics (code coverage, code smells, bugs) are reported as PR comments.

**Acceptance Scenarios**:

1. **Given** the build-and-test pipeline completes successfully for a PR, **When** the Sonar pipeline is triggered, **Then** the code analysis begins automatically without manual intervention.
2. **Given** the Sonar analysis completes, **When** results are available, **Then** a comment is posted on the PR with quality metrics (coverage percentage, issue count, quality gate status).
3. **Given** the configured Quality Gate fails (e.g., coverage below threshold, new critical issues), **When** the analysis finishes, **Then** the PR shows a failed Sonar status check, preventing merge.
4. **Given** the Quality Gate passes, **When** the analysis finishes, **Then** the PR shows a passing Sonar status check.

---

### User Story 3 - Automatic Version Bump on Merge (Priority: P3)

When code is merged into the main branch, the system automatically increments the patch version number in a central version file, commits the change back to main, and ensures concurrent merges are processed sequentially without version collisions.

**Why this priority**: Reduces human error in versioning and frees the team from manual version management. Depends on a healthy main branch (P1 and P2), so it is third in priority.

**Independent Test**: Can be fully tested by merging a PR into main (excluding version.json changes) and confirming that the version file is updated with a higher patch number and a commit appears in the git history with the new version.

**Acceptance Scenarios**:

1. **Given** a PR is merged into main, **When** the merge does not affect version.json, **Then** the versioning pipeline triggers automatically.
2. **Given** the versioning pipeline runs, **When** it reads the current version (e.g., `1.0.0`), **Then** it increments the patch segment and writes `1.0.1` back to version.json.
3. **Given** the version file is updated, **When** the pipeline commits the change, **Then** a new commit appears in main with message `chore: bump version to X.Y.Z` authored by `github-actions`.
4. **Given** two PRs are merged into main in quick succession, **When** both trigger the versioning pipeline, **Then** they run sequentially (not concurrently) and produce two distinct version increments without conflicts.
5. **Given** the pipeline itself commits the version bump, **When** the commit is pushed to main, **Then** the versioning pipeline does NOT re-trigger (no infinite loop).

---

### User Story 4 - Version Exposed via API (Priority: P4)

The running .NET API reads the current version from the version file at startup and exposes it through the Swagger/OpenAPI documentation interface, so developers and operators can always verify which version of the API is deployed.

**Why this priority**: This is a developer-experience feature that depends on the versioning pipeline (P3) being in place. It adds observability but is not a prerequisite for CI/CD functioning.

**Independent Test**: Can be fully tested by deploying the API and opening the Swagger UI, confirming the displayed version matches the value in version.json.

**Acceptance Scenarios**:

1. **Given** the API starts up, **When** version.json is present and contains a valid version string, **Then** the Swagger UI displays the version in the API title/info section.
2. **Given** the version in version.json is updated (e.g., `1.0.1`), **When** the API is restarted, **Then** the Swagger UI reflects the new version.

---

### Edge Cases

- What happens when the version.json file is missing or malformed when the versioning pipeline runs?
- How does the pipeline handle a scenario where the git push of the version bump fails (e.g., merge conflict with a fast concurrent merge)?
- What happens if the Sonar server is unreachable during analysis — does it fail the PR or is it treated as a non-blocking warning?
- How does the system behave when a developer pushes directly to main (bypassing a PR), triggering both a Sonar skip (no PR to comment on) and a version bump?

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST automatically trigger a build-and-test pipeline on every Pull Request opened or updated against the main branch.
- **FR-002**: The build pipeline MUST restore dependencies, compile the project in Release configuration, and execute all unit and integration tests with code coverage collection.
- **FR-003**: The system MUST report the pipeline result (pass/fail) as a status check on the Pull Request, visible to reviewers.
- **FR-004**: The system MUST automatically trigger a Sonar analysis pipeline after the build-and-test pipeline completes successfully for a PR.
- **FR-005**: The Sonar pipeline MUST post quality metrics as a comment or decoration on the Pull Request.
- **FR-006**: The system MUST support optionally blocking PR merges when the Sonar Quality Gate fails.
- **FR-007**: The system MUST automatically trigger a versioning pipeline on every push to main that does not modify version.json.
- **FR-008**: The versioning pipeline MUST increment the patch segment of the semantic version in version.json (e.g., `1.0.0` → `1.0.1`).
- **FR-009**: The versioning pipeline MUST commit the updated version.json back to main with a standardized commit message.
- **FR-010**: The system MUST guarantee sequential (non-concurrent) execution of versioning pipeline runs to prevent version collisions.
- **FR-011**: The system MUST prevent the versioning pipeline from triggering itself in an infinite loop after committing the version bump.
- **FR-012**: The .NET API MUST read the version from version.json at startup and expose it in the Swagger/OpenAPI documentation.
- **FR-013**: The main branch MUST be protected, requiring at least one passing pipeline status check before a PR can be merged.

### Key Entities

- **version.json**: Central version file at the repository root containing `{ "version": "MAJOR.MINOR.PATCH" }`. Single source of truth for the application version.
- **Pipeline Run**: An execution of a GitHub Actions workflow triggered by a specific event (PR open/update, workflow completion, push to main).
- **Quality Gate**: A SonarQube-defined threshold configuration that determines whether code quality is acceptable for merge.
- **Status Check**: A GitHub PR status indicator (pass/fail/pending) reported by a pipeline, used to enforce branch protection rules.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every PR against main receives an automated build-and-test result within 5 minutes of the triggering event, with no manual action required from the developer.
- **SC-002**: Code quality analysis results are available as PR feedback within 10 minutes of the build pipeline completing successfully.
- **SC-003**: 100% of merges to main result in a correctly incremented patch version in version.json, with zero duplicate version numbers even under concurrent merge activity.
- **SC-004**: The versioning pipeline never triggers more than once per developer-initiated merge event (no infinite loops).
- **SC-005**: The deployed API Swagger UI always displays a version string that matches the version.json at the time of the last deployment.
- **SC-006**: Developers have zero manual steps required for versioning — all version increments are handled automatically after merge.

---

## Assumptions

- The project is a single .NET 8 solution; multi-solution or monorepo setups with multiple independently versioned services are out of scope.
- A SonarQube server (or SonarCloud) is available and accessible from GitHub Actions runners; provisioning the Sonar server itself is out of scope.
- `SONAR_TOKEN` and `SONAR_HOST_URL` secrets are configured in the GitHub repository settings before the Sonar pipeline is activated.
- GitHub branch protection rules will be configured by a repository administrator after the pipelines are set up.
- The version.json file starts at `1.0.0` and uses strict semantic versioning (`MAJOR.MINOR.PATCH`); only the patch segment is auto-incremented (manual changes handle major/minor bumps).
- The `.NET` API uses ASP.NET Core with Swagger/Swashbuckle; reading the version file at startup is standard and does not require a custom hosting model.
- GitHub-hosted `ubuntu-latest` runners are used; self-hosted runners are out of scope.
- The `github-actions[bot]` user is identified as the committer for version bump commits; the loop prevention strategy relies on `paths-ignore: ['version.json']` as the primary mechanism.
- Semantic versioning automation (feat → minor, breaking → major) is a future enhancement and not part of this specification.
