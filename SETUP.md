# Repository Setup Checklist

Complete these one-time manual steps after cloning this repository.

## 1. Configure Repository Secrets (T029)

Go to **GitHub → Settings → Secrets and variables → Actions → New repository secret** and add:

| Secret Name      | Value                                                          |
|------------------|----------------------------------------------------------------|
| `SONAR_TOKEN`    | Token from your SonarQube/SonarCloud account                  |
| `SONAR_HOST_URL` | e.g., `https://sonarcloud.io` or your self-hosted Sonar URL   |

## 2. Configure Branch Protection on `main` (T030)

Go to **GitHub → Settings → Branches → Add branch protection rule** for `main`:

- [x] Require a pull request before merging
- [x] Require status checks to pass before merging
  - Required check: `build-test`
  - Optional check: `sonar` (add only if Quality Gate blocking is desired)
- [x] Require branches to be up to date before merging

## 3. Set Sonar Project Key

Open `.github/workflows/sonar-analysis.yml` and replace `YOUR_PROJECT_KEY` with your actual SonarQube project key.

## 4. Add `coverlet.collector` to Test Projects (T032)

Each test `.csproj` must include:

```xml
<PackageReference Include="coverlet.collector" Version="6.*" />
```

## 5. Add version.json to .NET Build Output

Copy the snippet from `src/YourProject/version-json-csproj-snippet.xml` into your API's `.csproj`.  
Adjust the relative path to `version.json` based on your solution layout.

## 6. Validate End-to-End (T031)

Follow the steps in `specs/001-github-actions-cicd/quickstart.md` to verify all pipelines work correctly.
