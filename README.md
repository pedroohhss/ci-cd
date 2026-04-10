# ci-cd

Reference implementation of a production-grade CI/CD pipeline for .NET 8 projects using GitHub Actions.

**Current version**: `1.0.1` (auto-managed via versioning pipeline)

---

## Pipelines

Three workflows run automatically — no manual steps required after initial setup.

### PR - Build & Test (`pr-build-test.yml`)

Triggers on every Pull Request against `main`.

| Step | Detail |
|---|---|
| Checkout | Latest PR head |
| Setup .NET | 8.0.x with NuGet cache |
| Restore | From cache or fresh download |
| Build | Release configuration |
| Test + Coverage | XPlat Code Coverage (Coverlet) |
| Upload artifacts | Coverage XML for Sonar |

Version bump PRs (`version/bump-*` branches) skip build/test and pass immediately — no wasted CI time on a one-line change.

### Sonar Analysis (`sonar-analysis.yml`)

Triggers after `PR - Build & Test` completes successfully.

- Runs `dotnet-sonarscanner` with coverage report
- Posts quality metrics to the PR
- Skips automatically for version bump PRs

### Versioning (`versioning.yml`)

Triggers on every push to `main` (excluding `version.json` changes).

| Behavior | Detail |
|---|---|
| Reads | `version.json` at repo root |
| Increments | Patch segment only (`1.0.0` → `1.0.1`) |
| Creates | Branch `version/bump-X.Y.Z` |
| Opens | PR titled `chore: bump version to X.Y.Z` |
| Concurrency | Queued — never cancelled, never parallel |
| Loop guard (primary) | `paths-ignore: ['version.json']` on trigger |
| Loop guard (secondary) | Skips if actor is `github-actions[bot]` |
| Freshness | `git pull origin main` before reading version |

---

## Repository Structure

```text
.github/
└── workflows/
    ├── pr-build-test.yml    # Build & test on every PR
    ├── sonar-analysis.yml   # Code quality analysis after build
    └── versioning.yml       # Auto version bump on merge to main

src/
└── YourProject/
    ├── Models/              # VersionModel — reads version.json at startup
    ├── Program.cs           # Injects version into Swagger/OpenAPI title
    └── version-json-csproj-snippet.xml  # Add this to .csproj for build output

version.json                 # Single source of truth for current version
SETUP.md                     # One-time manual configuration checklist
```

---

## Branch Protection (required)

Configure in **GitHub → Settings → Branches → main**:

| Setting | Value |
|---|---|
| Require PR before merging | Yes |
| Required status check | `build-test` |
| Allow GitHub Actions to create PRs | Yes (Settings → Actions → General) |
| Sonar check | Optional — add as required only if Quality Gate blocking is desired |

---

## Secrets Required

| Secret | Description |
|---|---|
| `SONAR_TOKEN` | Token from SonarQube or SonarCloud |
| `SONAR_HOST_URL` | e.g., `https://sonarcloud.io` |

Add via **GitHub → Settings → Secrets and variables → Actions**.

---

## First-Time Setup

See [`SETUP.md`](./SETUP.md) for the complete checklist, including:
- Configuring Sonar secrets
- Setting branch protection rules
- Adding `coverlet.collector` to test projects
- Including `version.json` in the .NET build output

---

## Specifications

Detailed design documents for each feature are in [`specs/`](./specs/):

| Spec | Description |
|---|---|
| [`001-github-actions-cicd`](specs/001-github-actions-cicd/) | Base CI/CD pipelines: build, Sonar, versioning |
| [`002-auto-version-pr`](specs/002-auto-version-pr/) | PR-based versioning (replaces direct push to main) |
| [`003-skip-version-bump-ci`](specs/003-skip-version-bump-ci/) | Skip build/Sonar for version bump PRs |
| [`004-cicd-resilience-fixes`](specs/004-cicd-resilience-fixes/) | Loop guard, NuGet caching, version freshness |

Each spec includes `plan.md`, `tasks.md`, `research.md`, `quickstart.md`, and `contracts/`.

---

## How Versioning Works End-to-End

```
Developer merges PR to main
        ↓
Versioning pipeline triggers
        ↓
git pull origin main  (ensures freshest version)
        ↓
Reads version.json → increments patch
        ↓
peter-evans/create-pull-request opens PR:
  branch: version/bump-X.Y.Z
  title:  chore: bump version to X.Y.Z
        ↓
Team merges version bump PR
        ↓
Push to main (only version.json changed)
→ paths-ignore fires → pipeline does NOT re-trigger
```

---

## License

See [LICENSE](./LICENSE).
