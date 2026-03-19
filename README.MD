# BudgetFlow API

BudgetFlow is a personal finance backend built with ASP.NET Core, Entity Framework Core, PostgreSQL, and JWT authentication.

It currently supports:

- User registration, login, refresh, and logout
- Role-based authorization for users and admins
- Wallet management
- Transaction management with filtering
- User and admin dashboards
- Admin user management: role changes, block, unblock
- Audit logs with recent system events

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- JWT Bearer authentication
- Serilog
- Docker Compose

## Solution Structure

```text
src/
  BudgetFlow.Api             Web API layer
  BudgetFlow.Application     Contracts and service abstractions
  BudgetFlow.Domain          Domain entities and business types
  BudgetFlow.Infrastructure  EF Core, auth, services, persistence
tests/
  BudgetFlow.Tests           Test project scaffold
```

## Features

### Authentication

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`

### Users

- `GET /api/users/me`
- `GET /api/users` admin only
- `PUT /api/users/{id}/role` admin only
- `PUT /api/users/{id}/block` admin only
- `PUT /api/users/{id}/unblock` admin only

### Wallets

- `GET /api/wallets`
- `POST /api/wallets`
- `PUT /api/wallets/{id}`
- `DELETE /api/wallets/{id}`

### Transactions

- `GET /api/transactions`
- `POST /api/transactions`
- `PUT /api/transactions/{id}`
- `DELETE /api/transactions/{id}`

Supported filters:

- `walletId`
- `type`
- `categoryId`
- `from`
- `to`

### Dashboards

- `GET /api/dashboard` current user dashboard
- `GET /api/dashboard/admin` admin system totals

### Audit Logs

- `GET /api/audit-logs/recent?limit=20` admin only

## Running the Project

### Option 1: Docker Compose

1. Copy `.env.example` to `.env`
2. Start the stack:

```powershell
docker compose up --build
```

Services:

- API: `http://localhost:8080`
- PostgreSQL: `localhost:5432`
- pgAdmin: `http://localhost:5050`

Default pgAdmin credentials from `docker-compose.yml`:

- Email: `someemail@gmail.com`
- Password: `admin123`

### Option 2: Run locally

Make sure PostgreSQL is running and the connection string in [appsettings.json](src/BudgetFlow.Api/appsettings.json) matches your local database.

Run the API:

```powershell
dotnet run --project src/BudgetFlow.Api
```

On first startup, the app applies EF Core migrations and seeds demo data if the database has no users yet.

Demo credentials:

- Admin: `admin@budgetflow.local` / `Admin123!`
- User: `alice@budgetflow.local` / `User123!`
- User: `bob@budgetflow.local` / `User123!`

The demo seed also creates sample wallets, categories, and transactions so dashboards and filtered transaction queries have useful data immediately.

## Environment Variables

Example values are provided in `.env.example`:

```env
POSTGRES_DB=budgetflow
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
DB_HOST=budgetflow-db
DB_PORT=5432
```

## Database Migrations

Add migration:

```powershell
dotnet ef migrations add <MigrationName> --project src/BudgetFlow.Infrastructure --startup-project src/BudgetFlow.Api
```

Update database:

```powershell
dotnet ef database update --project src/BudgetFlow.Infrastructure --startup-project src/BudgetFlow.Api
```

Drop database:

```powershell
dotnet ef database drop --project src/BudgetFlow.Infrastructure --startup-project src/BudgetFlow.Api
```

## OpenAPI and Postman

In development, the app exposes an OpenAPI document that you can import into Postman:

`http://localhost:8080/openapi/v1.json`

There is also a ready-to-use request file for IDE-based API testing:

[BudgetFlow.Api.http](/src/BudgetFlow.Api/BudgetFlow.Api.http)

## Authentication Notes

- Most endpoints require a Bearer token
- `UserOnly` policy allows both `User` and `Admin`
- `AdminOnly` policy allows only `Admin`
- Access tokens and refresh tokens are issued through `/api/auth/login`

## Current Architecture Notes

- Controllers stay thin and delegate to application/infrastructure services
- EF Core is used directly in infrastructure services
- Audit logs are written for important auth, wallet, user, and transaction actions
- Global exception middleware is configured in the API project
