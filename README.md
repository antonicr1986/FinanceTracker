# FinanceTracker

FinanceTracker is a personal finance tracking API built with .NET 8, Entity Framework Core and SQL Server LocalDB.

The goal of this project is to practice and demonstrate backend development skills using a layered architecture, DTOs, services, validation, Entity Framework Core and automated tests.

## Features

- Manage income and expense categories
- Manage financial transactions
- Filter transactions by type, category and date range
- Calculate financial summary:
  - Total income
  - Total expenses
  - Balance
- Basic validation using Data Annotations
- Business rules:
  - Transactions cannot be created with a non-existing category
  - Transactions cannot be updated with a non-existing category
  - Categories with associated transactions cannot be deleted
- Automated tests with xUnit and EF Core InMemory

## Technologies

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server LocalDB
- xUnit
- EF Core InMemory
- Swagger / OpenAPI
- Git / GitHub

## Architecture

The solution follows a layered architecture:

FinanceTracker
├── FinanceTracker.Api
├── FinanceTracker.Application
├── FinanceTracker.Domain
├── FinanceTracker.Infrastructure
└── FinanceTracker.Tests

### FinanceTracker.Api

Contains the REST API controllers and application startup configuration.

### FinanceTracker.Application

Contains DTOs, interfaces and application-level contracts.

### FinanceTracker.Domain

Contains the main domain entities and enums.

### FinanceTracker.Infrastructure

Contains Entity Framework Core configuration, database context, migrations and service implementations.

### FinanceTracker.Tests

Contains automated tests for the application logic.

## Main Endpoints

### Categories

GET /api/Categories  
GET /api/Categories/{id}  
POST /api/Categories  
PUT /api/Categories/{id}  
DELETE /api/Categories/{id}

### Transactions

GET /api/Transactions  
GET /api/Transactions/{id}  
POST /api/Transactions  
PUT /api/Transactions/{id}  
DELETE /api/Transactions/{id}

### Transaction Filters

GET /api/Transactions?type=2  
GET /api/Transactions?categoryId=4  
GET /api/Transactions?fromDate=2026-05-01&toDate=2026-05-31

### Financial Summary

GET /api/Transactions/summary  
GET /api/Transactions/summary?fromDate=2026-05-01&toDate=2026-05-31  
GET /api/Transactions/summary?categoryId=4

Example response:

{
  "totalIncome": 2000,
  "totalExpense": 350,
  "balance": 1650
}

## Getting Started

### Prerequisites

- Visual Studio 2022
- .NET 8 SDK
- SQL Server LocalDB

### Database Setup

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

## Running the API

Set `FinanceTracker.Api` as the startup project and run the application.

Swagger will be available at:

https://localhost:{port}/swagger

## Running Tests

Tests can be executed from Visual Studio Test Explorer or with:

dotnet test

## Project Status

Current status: MVP in progress.

Implemented:

- Layered solution structure
- Entity Framework Core setup
- Category CRUD
- Transaction CRUD
- Transaction filters
- Financial summary endpoint
- DTO validation
- Basic business rules
- Initial automated tests

Planned improvements:

- More unit tests
- Pagination
- Better error responses
- Authentication
- User-based finance tracking
- Improved Swagger documentation
- Deployment guide

## Purpose

This project is part of my portfolio as a junior .NET developer.

It is intended to demonstrate clean project structure, backend API development, Entity Framework Core usage, validation, testing and GitHub workflow.