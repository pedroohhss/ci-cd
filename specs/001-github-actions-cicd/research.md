# Research: CI/CD Pipelines with GitHub Actions (.NET + Sonar + Versioning)

**Date**: 2026-04-09  
**Feature**: [spec.md](./spec.md)

---

## Decision 1: Sonar Pipeline Trigger Strategy

**Decision**: Use `workflow_run` trigger listening to `"PR - Build & Test"` completion.

**Rationale**: The Sonar analysis must run after the build succeeds to access the compiled output and coverage reports. The `workflow_run` trigger provides this sequencing without tight coupling in a single workflow. It also runs in the context of the target branch (main), which allows access to secrets — a requirement because `pull_request` workflows from forks cannot access secrets by default. This is the officially recommended pattern for SonarCloud/SonarQube PR decoration on public or organization repos.

**Alternatives considered**:
- **Single workflow with sequential jobs**: Would require secrets access from a `pull_request` trigger context, which is restricted for fork PRs. Ruled out for security model incompatibility.
- **`pull_request` direct trigger**: Simpler but blocks secret access from forked PRs. Acceptable only for private repos with no forks.

**Assumption for this project**: Internal/private repository with no fork PRs. The `workflow_run` approach is still preferred because it clearly separates concerns and makes the Sonar pipeline independently re-runnable.

---

## Decision 2: Loop Prevention Strategy for Versioning Pipeline

**Decision**: Use `paths-ignore: ['version.json']` on the `push` trigger (Option 1 from spec).

**Rationale**: `paths-ignore` is evaluated by GitHub before the runner starts, making it the cheapest and most reliable prevention mechanism. It requires zero logic inside the workflow and has no race condition risks. The `github.actor != 'github-actions[bot]'` conditional (Option 2) is evaluated at job level, meaning the workflow still starts and consumes a runner slot before being skipped.

**Alternatives considered**:
- **`if: github.actor != 'github-actions[bot]'`**: Works correctly but wastes a runner slot. Also fragile if the committer identity ever changes (e.g., custom app token).
- **Commit message check (`[skip ci]`)**: Requires the commit message convention to be consistently applied. More error-prone.

---

## Decision 3: Versioning — patch-only vs. semantic versioning

**Decision**: Implement patch-only auto-increment for now. Semantic versioning (feat → minor, breaking → major) is deferred to a future enhancement.

**Rationale**: The spec explicitly marks semantic versioning as a future improvement. Implementing it now would require commit message parsing (conventional commits format), which adds a dependency on commit discipline. The patch-only approach is fully self-contained using `jq` (pre-installed on ubuntu-latest) with no external actions needed.

**Future path**: When semantic versioning is added, use the `conventional-commits` specification with a parser step before the version increment step. The `version.json` format is already compatible — no schema changes needed.

---

## Decision 4: Code Coverage Format and Sonar Integration

**Decision**: Use `--collect:"XPlat Code Coverage"` (Coverlet) and pass the report path to Sonar via `/d:sonar.cs.opencover.reportsPaths`.

**Rationale**: XPlat Code Coverage (Coverlet) is the standard coverage collector for .NET and produces OpenCover XML format by default when configured with `coverlet.collector`. SonarQube/SonarCloud natively consumes OpenCover format. This requires the `coverlet.collector` NuGet package in test projects but no external tools.

**Implementation note**: Coverage reports land in `TestResults/` directories. The sonar scanner step must be told where to find them via the `/d:sonar.cs.opencover.reportsPaths` parameter. A glob pattern like `**/*.opencover.xml` is recommended.

---

## Decision 5: .NET Version File Loading Strategy

**Decision**: Read `version.json` at application startup using `File.ReadAllText` + `JsonSerializer.Deserialize<VersionModel>`. Register as a singleton in the DI container. Inject into Swagger configuration.

**Rationale**: Simple and idiomatic for .NET 8. No custom middleware or build-time code generation required. The file is small (< 100 bytes), so reading it once at startup has negligible overhead.

**Risk**: If `version.json` is missing from the deployment package, the API will fail to start. Mitigation: add `version.json` to the `.csproj` as `<Content CopyToOutputDirectory="Always">` so it is always included in the build output.

---

## Decision 6: Sonar Project Key

**Decision**: Use a configurable project key placeholder (`YOUR_PROJECT_KEY`) that must be set during repository setup. Document this in `quickstart.md`.

**Rationale**: The project key is specific to each SonarQube/SonarCloud organization and cannot be auto-generated. It is not a secret (it can be in the YAML), so it does not need to be a repository secret — but it must be provided before the pipeline is usable.

---

## Resolved Clarifications

All specification items were fully defined. No `[NEEDS CLARIFICATION]` markers were present. Research focused on confirming best practices for the already-defined approach rather than resolving ambiguity.
