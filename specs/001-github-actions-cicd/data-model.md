# Data Model: CI/CD Pipelines with GitHub Actions (.NET + Sonar + Versioning)

**Date**: 2026-04-09  
**Feature**: [spec.md](./spec.md)

---

## Entity: Version File (`version.json`)

**Purpose**: Single source of truth for the application version. Stored at the repository root and committed to git.

**Schema**:

```json
{
  "version": "MAJOR.MINOR.PATCH"
}
```

| Field     | Type   | Format                      | Example   | Constraints                                      |
|-----------|--------|-----------------------------|-----------|--------------------------------------------------|
| `version` | string | Semantic version (SemVer 2) | `"1.0.3"` | Required. Format: `\d+\.\d+\.\d+`. No pre-release/build metadata in v1. |

**Lifecycle**:
- **Created**: Manually at repository initialization with value `"1.0.0"`.
- **Updated by**: The versioning pipeline on every push to main (patch increment only).
- **Read by**: The .NET API at startup; the versioning pipeline before incrementing.

**State transitions**:

```
1.0.0 → (merge) → 1.0.1 → (merge) → 1.0.2 → ...
      ↑ patch auto-increment only
```

---

## Entity: Pipeline Status Check

**Purpose**: GitHub status check reported on a Pull Request by a workflow run. Represents the pass/fail result of a pipeline execution.

| Field         | Values                          | Description                                    |
|---------------|---------------------------------|------------------------------------------------|
| `context`     | `"PR - Build & Test"`, `"Sonar Analysis"` | Identifies which pipeline reported the check |
| `state`       | `success`, `failure`, `pending` | Current status                                 |
| `target_url`  | GitHub Actions run URL          | Links to detailed run logs                     |
| `description` | Short human-readable summary    | E.g., "All tests passed", "Quality Gate failed" |

**This entity is managed entirely by GitHub Actions and GitHub's Checks API — no custom storage required.**

---

## Entity: Sonar Quality Gate Result

**Purpose**: The outcome of a SonarQube/SonarCloud quality analysis run. Reported as a PR comment and status check.

| Field              | Type    | Description                                             |
|--------------------|---------|---------------------------------------------------------|
| `status`           | `OK` / `ERROR` | Whether the Quality Gate passed                  |
| `coverage_percent` | float   | Percentage of code covered by tests                     |
| `new_issues`       | int     | Number of new code issues introduced by this PR         |
| `security_hotspots`| int     | Number of new security hotspots                         |
| `duplications`     | float   | Percentage of duplicated lines                          |

**This entity is owned by SonarQube/SonarCloud — no custom storage required.**

---

## Entity: VersionModel (.NET)

**Purpose**: C# class used to deserialize `version.json` in the .NET API. Passed to Swagger configuration.

```csharp
public class VersionModel
{
    public string Version { get; set; } = "0.0.0";
}
```

| Property  | Type   | Source                    | Usage                            |
|-----------|--------|---------------------------|----------------------------------|
| `Version` | string | Deserialized from `version.json` | Injected into `OpenApiInfo.Version` |

**Lifecycle**: Created once at API startup, registered as a singleton in the DI container.

---

## Entity: Workflow Concurrency Group

**Purpose**: A logical grouping that GitHub Actions uses to serialize pipeline runs. Used by the versioning pipeline to prevent concurrent version bumps.

| Field                  | Value          | Description                                          |
|------------------------|----------------|------------------------------------------------------|
| `group`                | `"versioning"` | All versioning pipeline runs share this group name   |
| `cancel-in-progress`   | `false`        | Queued runs wait; they are never cancelled           |

**This is a GitHub Actions configuration entity — no data storage required.**

---

## Relationships

```
Repository Push (to main, excluding version.json)
    └─triggers──► Versioning Pipeline Run
                      └─reads/writes──► version.json

Pull Request (targeting main)
    └─triggers──► Build+Test Pipeline Run
                      └─reports──► PR Status Check ("PR - Build & Test")
                      └─on success, triggers──► Sonar Pipeline Run
                                                    └─reports──► PR Status Check ("Sonar Analysis")
                                                    └─posts──► PR Comment (quality metrics)

API Startup
    └─reads──► version.json
               └─produces──► VersionModel
                                 └─injects into──► Swagger OpenApiInfo.Version
```
