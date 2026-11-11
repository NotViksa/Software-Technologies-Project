# Software Technologies Project

Web platform for a board game café with a public catalogue and table/game reservations.

The project is a modular monolith built with ASP.NET Core MVC. It has four layers (Presentation, Application, Domain, Infrastructure) and five functional modules.

## Team and module ownership

| Member | Responsibility |
| --- | --- |
| Member 1 (Viktor) | Public site and catalogue, base dashboard, seed data, Docker, CI, health check |
| Member 2 (Veselin) | Availability, allocation, reservations, state machine, concurrency |
| Member 3 (Ivan) | Inventory, game copies, loans, returns, damage |
| Member 4 (Nadezhda) | Identity, roles, access control, café administration, staff confirmation, audit |
| Member 5 (Daniel) | Email outbox, Brevo integration, acceptance/E2E tests, demo |

The base provides shared entities and infrastructure for modules 2-5. Services, DTOs and domain rules for those modules belong to the responsible member.

## Technology

- .NET 10 LTS, C#
- ASP.NET Core MVC with Razor views
- ASP.NET Core Identity
- Entity Framework Core and SQL Server
- Bootstrap 5
- xUnit
- Docker Compose and GitHub Actions

## Repository structure

```
src/BoardGameCafe.Web/
  Domain/            entities, enums, domain rules
  Application/       contracts, DTOs, shared result/error types
  Infrastructure/    EF Core, Identity, services, seed data, migrations
  Presentation/      controllers and view models
  Views/             Razor views
  wwwroot/           static assets
tests/BoardGameCafe.Tests/
docs/                proposal, requirements and architecture documents
```

## Local development

Requirements: .NET 10 SDK and SQL Server LocalDB or SQL Server Express.

```powershell
dotnet tool restore
dotnet restore
dotnet build BoardGameCafe.sln
dotnet run --project src/BoardGameCafe.Web
```

The default connection string uses LocalDB. Change `ConnectionStrings:DefaultConnection` in `appsettings.json` if needed.

The application applies migrations and seeds demo data at startup. Local health check: `http://localhost:5093/health` (see `Properties/launchSettings.json` for the exact port).

## Docker

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Open `http://localhost:8080`. Health check: `http://localhost:8080/health`.

`SQL_SA_PASSWORD` must be set in `.env`. The file is ignored by git.

## Demo accounts

| Role | Email | Password |
| --- | --- | --- |
| Manager | manager@boardgamecafe.local | Passw0rd! |
| Employee | employee@boardgamecafe.local | Passw0rd! |
| Customer | customer@boardgamecafe.local | Passw0rd! |

## Database and migrations

```powershell
dotnet tool run dotnet-ef migrations add <Name> --project src/BoardGameCafe.Web --startup-project src/BoardGameCafe.Web --output-dir Infrastructure/Persistence/Migrations
dotnet tool run dotnet-ef database update --project src/BoardGameCafe.Web --startup-project src/BoardGameCafe.Web
```

Only one member should add a migration at a time to avoid merge conflicts.

## Branching and workflow

- `main` is protected and always buildable.
- Work happens on `feature/<short-description>` branches.
- Every change goes through a pull request and at least one review.
- CI restores, builds and tests the solution on every push and pull request.
- A task is complete when it has code, tests, review and short documentation.

## Google and Facebook login

External login is optional and enabled only when credentials are configured. Set `Authentication:Google:ClientId`/`ClientSecret` or `Authentication:Facebook:AppId`/`AppSecret` in configuration or environment variables. Local login remains available.
