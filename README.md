# RandWise

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![React](https://img.shields.io/badge/React-19-149ECA)
![Vite](https://img.shields.io/badge/Vite-8-646CFF)
![TypeScript](https://img.shields.io/badge/TypeScript-6-3178C6)
![GitHub Pages](https://img.shields.io/badge/GitHub%20Pages-ready-222222)

RandWise is a South African personal finance app for payday budgeting, safe-to-spend tracking, transaction capture, category groups, and weekly or monthly savings targets. The current implementation is a modular .NET API with a React/Vite frontend that can run against the API locally or as a self-contained GitHub Pages demo.

## What Works Now

- Account registration, login, logout, JWT auth, and refresh-token persistence.
- Financial profile onboarding.
- Transaction listing and quick expense capture.
- Dashboard with safe-to-spend, Money Pulse, category groups, savings targets, streaks, and achievements.
- Privacy mode for redacting visible money amounts.
- Persistent dark mode.
- Static GitHub Pages demo mode with local browser storage.
- CI for backend and frontend validation.
- GitHub Pages deployment workflow for the frontend.

## Repository Layout

```text
.
+-- .github/workflows
|   +-- ci.yml
|   +-- pages.yml
+-- docs
+-- frontend/randwise-web
|   +-- public
|   +-- src
+-- src
|   +-- RandWise.Api
|   +-- RandWise.Application
|   +-- RandWise.Contracts
|   +-- RandWise.Domain
|   +-- RandWise.Infrastructure
+-- tests
    +-- RandWise.ArchitectureTests
    +-- RandWise.IntegrationTests
    +-- RandWise.UnitTests
```

## Local Development

### Prerequisites

- .NET SDK 10.x
- Node.js 22.x
- npm

### Backend

```powershell
dotnet restore RandWise.sln
dotnet run --project src/RandWise.Api --launch-profile http
```

The local API runs at:

```text
http://localhost:5241/api/v1
```

Health check:

```powershell
Invoke-RestMethod http://localhost:5241/api/v1/health
```

### Frontend

```powershell
cd frontend/randwise-web
npm ci
npm run dev
```

Open:

```text
http://127.0.0.1:5173
```

By default the frontend calls the local API at `http://localhost:5241/api/v1`.

## Frontend Environment

Create `frontend/randwise-web/.env.local` when you need to override defaults.

```env
VITE_API_BASE_URL=http://localhost:5241/api/v1
VITE_DEMO_MODE=false
VITE_BASE_PATH=/
```

Use demo mode when there is no hosted API:

```env
VITE_DEMO_MODE=true
```

Demo mode keeps users, tokens, financial profile values, and transactions in browser storage. It is intended for GitHub Pages and product previews, not production finance data.

## GitHub Pages Deployment

This repository includes `.github/workflows/pages.yml`.

The workflow:

1. Runs on pushes to `main` and manual `workflow_dispatch`.
2. Installs frontend dependencies with `npm ci`.
3. Runs lint, typecheck, and tests.
4. Builds the frontend with `VITE_DEMO_MODE=true`.
5. Copies `index.html` to `404.html` so React routes can reload on GitHub Pages.
6. Uploads `frontend/randwise-web/dist`.
7. Deploys through GitHub Pages Actions.

### Enable Pages

In GitHub:

1. Open the repository.
2. Go to `Settings` -> `Pages`.
3. Set `Build and deployment` source to `GitHub Actions`.
4. Push to `main`, or run `Deploy GitHub Pages` manually from the Actions tab.

For a normal project repository, Vite automatically uses:

```text
/<repository-name>/
```

For an `owner.github.io` user site, Vite automatically uses:

```text
/
```

Override the base path only if needed:

```env
VITE_BASE_PATH=/custom-path/
```

## GitHub Publishing From This Machine

This folder is currently not a git repository. To publish it:

```powershell
cd C:\Users\henko\Downloads\RandWise\randwise-codex-blueprint
git init
git branch -M main
git add .
git commit -m "Initial RandWise app"
git remote add origin https://github.com/<owner>/<repo>.git
git push -u origin main
```

The GitHub CLI is not installed on this machine, so Codex cannot create the remote repository or open a PR from here until `gh` is installed and authenticated.

## Validation

Backend:

```powershell
dotnet format RandWise.sln --verify-no-changes
dotnet test RandWise.sln
```

Frontend:

```powershell
cd frontend/randwise-web
npm run lint
npm run typecheck
npm run test
npm run build
```

Pages demo build:

```powershell
cd frontend/randwise-web
$env:VITE_DEMO_MODE="true"
npm run build
```

## Security And Privacy Notes

- Money values can be redacted in the UI with Privacy Mode.
- GitHub Pages demo mode stores data only in the browser and should not be used for real financial data.
- Production deployments should use the .NET API with HTTPS, a persistent database, secure JWT settings, and a hosted API URL configured through `VITE_API_BASE_URL`.

## Product Direction

RandWise is being built wave by wave from the implementation graph in `docs/TASK_GRAPH.md`. The canonical API contract is `docs/API_CONTRACT.md`; frontend work should not invent divergent backend contracts.
