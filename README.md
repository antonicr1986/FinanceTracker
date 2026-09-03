# 💰 FinanceTracker
![CI/CD](https://img.shields.io/github/actions/workflow/status/antonicr1986/FinanceTracker/ci.yml?style=for-the-badge&label=CI%2FCD&logo=githubactions&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
[![Container](https://img.shields.io/badge/ghcr.io-financetracker-24292E?style=for-the-badge&logo=github&logoColor=white)](https://github.com/antonicr1986/FinanceTracker/pkgs/container/financetracker)

FinanceTracker is a personal finance tracking API built with .NET 8, Entity Framework Core and SQL Server LocalDB.

The goal of this project is to practice and demonstrate backend development skills using a layered architecture, DTOs, services, validation, Entity Framework Core and automated tests.

## ✨ Features

- User registration and login with JWT authentication
- All finance endpoints require a valid bearer token
- Manage income and expense categories
- Manage financial transactions
- Manage monthly budgets
- Filter transactions by type, category and date range
- Paginated transaction results
- Calculate financial summary:
  - Total income
  - Total expenses
  - Balance
- Dashboard summary endpoint
- Budget usage calculations:
  - Spent amount
  - Remaining amount
  - Usage percentage
- Basic validation using Data Annotations
- Business rules:
  - Transactions cannot be created with a non-existing category
  - Transactions cannot be created when the category type does not match the transaction type
  - Transactions cannot be updated with a non-existing category
  - Transactions cannot be updated when the category type does not match the transaction type
  - Budgets cannot be created with a non-existing category
  - Budgets cannot be created when the category type does not match the budget type
  - Budgets cannot be updated when the category type does not match the budget type
  - Categories with associated transactions cannot be deleted
- Automated tests with xUnit and EF Core InMemory

## 🛠️ Technologies

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server LocalDB
- JWT authentication (bearer tokens)
- xUnit
- EF Core InMemory
- Swagger / OpenAPI
- Git / GitHub
- GitHub Actions (CI/CD)
- Docker / Docker Compose
- GitHub Container Registry (GHCR)


## 🧱 Architecture

The solution follows a layered architecture:

FinanceTracker
├── FinanceTracker.Api
├── FinanceTracker.Application
├── FinanceTracker.Domain
├── FinanceTracker.Infrastructure
└── FinanceTracker.Tests

### 🌐 FinanceTracker.Api

Contains the REST API controllers and application startup configuration.

### 📦 FinanceTracker.Application

Contains DTOs, interfaces and application-level contracts.

### 🧠 FinanceTracker.Domain

Contains the main domain entities and enums.

### 🗄️ FinanceTracker.Infrastructure

Contains Entity Framework Core configuration, database context, migrations and service implementations.

### 🧪 FinanceTracker.Tests

Contains automated tests for the application logic.

## 🔗 Main Endpoints

### 📁 Categories

GET /api/Categories  
GET /api/Categories/{id}  
POST /api/Categories  
PUT /api/Categories/{id}  
DELETE /api/Categories/{id}

### 💳 Transactions

GET /api/Transactions  
GET /api/Transactions/{id}  
POST /api/Transactions  
PUT /api/Transactions/{id}  
DELETE /api/Transactions/{id}

### 💰 Budgets

GET /api/Budgets  
GET /api/Budgets/{id}  
POST /api/Budgets  
PUT /api/Budgets/{id}  
DELETE /api/Budgets/{id}

### 🔎 Transaction Filters

GET /api/Transactions?type=2  
GET /api/Transactions?categoryId=4  
GET /api/Transactions?fromDate=2026-05-01&toDate=2026-05-31

### 📄 Transaction Pagination

GET /api/Transactions?pageNumber=1&pageSize=10

Example response:

{
  "items": [],
  "totalCount": 25,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 3
}

### 📊 Financial Summary

GET /api/Transactions/summary  
GET /api/Transactions/summary?fromDate=2026-05-01&toDate=2026-05-31  
GET /api/Transactions/summary?categoryId=4

Example response:

{
  "totalIncome": 2000,
  "totalExpense": 350,
  "balance": 1650
}

### 📈 Dashboard Summary

GET /api/Dashboard/summary  
GET /api/Dashboard/summary?fromDate=2026-05-01&toDate=2026-05-31  
GET /api/Dashboard/summary?categoryId=4

Example response:

{
  "totalIncome": 2000,
  "totalExpense": 350,
  "balance": 1650,
  "transactionCount": 5,
  "latestTransactions": []
}

### 💼 Budget Usage

GET /api/Budgets

Example response:

{
  "id": 1,
  "name": "Food budget June",
  "amount": 300,
  "spentAmount": 100,
  "remainingAmount": 200,
  "usagePercentage": 33.33,
  "month": 6,
  "year": 2026,
  "type": 2,
  "categoryId": 1,
  "categoryName": "Food"
}

## 📊 Dashboard

The API includes a dashboard summary endpoint that provides an overview of the current financial situation.

GET /api/Dashboard/summary

Example response:

{
  "totalIncome": 2000,
  "totalExpense": 350,
  "balance": 1650,
  "transactionCount": 5,
  "latestTransactions": []
}

## 🚀 Getting Started

### ✅ Prerequisites

- Visual Studio 2022
- .NET 8 SDK
- SQL Server LocalDB

### 🗃️ Database Setup

The project uses SQL Server LocalDB.

Connection string example:

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=FinanceTrackerDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}

Apply migrations using the Package Manager Console:

Update-Database

Or using the .NET CLI:

dotnet ef database update

When the API runs in a container it applies any pending migrations automatically
at startup, so a fresh SQL Server instance is set up without any manual step.

## ▶️ Running the API

Set `FinanceTracker.Api` as the startup project and run the application.

Swagger will be available at:

https://localhost:{port}/swagger

## 🐳 Running with Docker

The project can also run fully containerized (API + SQL Server) using Docker Compose.

### ✅ Prerequisites

- Docker Desktop

### 🔐 Environment variables

Create a `.env` file in the root of the project (it is git-ignored and never committed) with:

MSSQL_SA_PASSWORD=YourStrongPassword123!
JWT_KEY=your-generated-jwt-key

### ▶️ Run

docker compose up --build

This starts two containers:

- `api` — the FinanceTracker API, available at `http://localhost:8080`
- `db` — SQL Server 2022, available on port `1433`

Swagger will be available at:

http://localhost:8080/swagger

## 📦 Running from the published image

Every push to `master` publishes a container image to GitHub Container Registry, so the
application can be run without cloning the repository or building anything.

docker pull ghcr.io/antonicr1986/financetracker:latest

`docker-compose.prod.yml` starts the published image alongside SQL Server:

docker compose -f docker-compose.prod.yml up -d

Unlike the development compose file, this one:

- Pulls the image instead of building from source
- Keeps the database in a named volume, so data survives a restart
- Waits for a SQL Server health check before starting the API
- Does not expose port 1433 to the host

To deploy a specific commit rather than the latest build:

IMAGE_TAG=sha-<commit-sha> docker compose -f docker-compose.prod.yml up -d

## ⚙️ CI/CD

This project uses GitHub Actions. On every push and pull request to `master`, the
pipeline automatically:

- Restores and builds the solution
- Runs the automated test suite
- Builds the Docker image

On pushes to `master` it additionally:

- Publishes the image to GitHub Container Registry
- Tags it with the full commit SHA and with `latest`

Pull requests build the image to validate the Dockerfile, but never publish. Because
every build is tagged by commit SHA, any previous version can be redeployed as-is,
which makes rollbacks a one-line change.

Workflow file: `.github/workflows/ci.yml`

## 🧪 Running Tests

Tests can be executed from Visual Studio Test Explorer or with:

dotnet test

Current automated tests: 43 passing tests.

Test coverage currently includes:

- Category service logic
- Transaction service logic
- Dashboard service logic
- Budget service logic
- Filtering
- Transaction update and delete scenarios
- Transaction filtering by category and date range
- Not found scenarios for budgets and transactions

## 📌 Project Status

Implemented:

- Layered solution structure
- Entity Framework Core setup
- SQL Server LocalDB database
- Database migrations
- Category CRUD
- Transaction CRUD
- Budget CRUD
- Dashboard summary endpoint
- Transaction filters
- Paginated transaction results
- Financial summary endpoint
- Budget usage calculations:
  - Spent amount
  - Remaining amount
  - Usage percentage
- DTO validation with Data Annotations
- Business rules:
  - Transactions cannot be created with a non-existing category
  - Transactions cannot be updated with a non-existing category
  - Transactions must match the selected category type
  - Budgets cannot be created with a non-existing category
  - Budgets must match the selected category type
  - Categories with associated transactions cannot be deleted
- Swagger / OpenAPI testing
- Automated tests with xUnit and EF Core InMemory
- JWT authentication (register and login)
- Migrations applied automatically on startup
- Dockerized application (API + SQL Server via Docker Compose)
- CI/CD pipeline with GitHub Actions (build, test, publish)
- Container image published to GHCR, tagged by commit SHA
- Production compose file with health checks and a persistent database volume

Planned improvements:

- User-scoped finance data
- More advanced budget reports
- Controller tests
- Global error handling
- Health check endpoint and structured logging
- Vulnerability scanning in the pipeline
- Automated deployment to a hosted environment
- Kubernetes deployment

## 🎯 Purpose

This project is part of my portfolio as a .NET developer.

It is intended to demonstrate clean project structure, backend API development, Entity Framework Core usage, validation, testing and GitHub workflow.
