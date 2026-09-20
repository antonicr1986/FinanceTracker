# 💰 FinanceTracker

[English](README.md) · **Español**

![CI/CD](https://img.shields.io/github/actions/workflow/status/antonicr1986/FinanceTracker/ci.yml?style=for-the-badge&label=CI%2FCD&logo=githubactions&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
[![Container](https://img.shields.io/badge/ghcr.io-financetracker-24292E?style=for-the-badge&logo=github&logoColor=white)](https://github.com/antonicr1986/FinanceTracker/pkgs/container/financetracker)

FinanceTracker es una API de finanzas personales construida con .NET 8, Entity Framework Core y SQL Server. En produccion corre en Azure App Service contra Azure SQL; en local, sobre SQL Server LocalDB o un contenedor.

El objetivo del proyecto es practicar y demostrar desarrollo de backend con arquitectura por capas, DTOs, servicios, validacion, Entity Framework Core y pruebas automatizadas.

**Cliente web:** esta API tiene un frontend en Next.js y TypeScript en
[financetracker-web](https://github.com/antonicr1986/financetracker-web),
desplegado en **[financetracker-web-tau.vercel.app](https://financetracker-web-tau.vercel.app/login)**.

**API en vivo:** el despliegue es publico — pruebalo en
[Swagger](https://financetracker-api-cpctbta0gddddge5.belgiumcentral-01.azurewebsites.net/swagger).
Se duerme tras 20 minutos sin uso, asi que la primera peticion del dia tarda unos
segundos en despertar la aplicacion y la base de datos.

## ✨ Funcionalidades

- Registro y acceso de usuarios con autenticacion JWT
- Todos los endpoints de finanzas exigen un token valido
- Gestion de categorias de ingresos y gastos
- Gestion de movimientos
- Gestion de presupuestos mensuales
- Filtrado de movimientos por tipo, categoria y rango de fechas
- Resultados paginados
- Calculo del resumen financiero:
  - Ingresos totales
  - Gastos totales
  - Balance
- Endpoint de resumen para el panel
- Calculo del consumo de presupuesto:
  - Importe gastado
  - Importe restante
  - Porcentaje de uso
- Validacion con Data Annotations
- Reglas de negocio:
  - No se puede crear un movimiento con una categoria inexistente
  - No se puede crear un movimiento cuyo tipo no coincida con el de la categoria
  - No se puede actualizar un movimiento con una categoria inexistente
  - No se puede actualizar un movimiento cuyo tipo no coincida con el de la categoria
  - No se puede crear un presupuesto con una categoria inexistente
  - No se puede crear un presupuesto cuyo tipo no coincida con el de la categoria
  - No se puede actualizar un presupuesto cuyo tipo no coincida con el de la categoria
  - No se puede borrar una categoria con movimientos asociados
- Los datos financieros estan acotados al usuario autenticado
- Categorias de partida sembradas al registrar una cuenta
- Cuenta de demostracion publica, sembrada al arrancar, para probar sin registrarse
- Errores como ProblemDetails con un `code` independiente del idioma
- Origenes CORS leidos de configuracion, modificables sin volver a desplegar
- Reintentos ante fallos transitorios de SQL, para que una base serverless en
  pausa no convierta la primera peticion del dia en un error 500
- Endpoints de health check y registro estructurado con Serilog
- Manejo global de excepciones
- Pruebas automatizadas con xUnit y EF Core InMemory

## 🛠️ Tecnologias

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server (Azure SQL en produccion, LocalDB o contenedor en local)
- Autenticacion JWT (bearer tokens)
- xUnit
- EF Core InMemory
- Swagger / OpenAPI
- Git / GitHub
- GitHub Actions (CI/CD)
- Docker / Docker Compose
- GitHub Container Registry (GHCR)

## 🧱 Arquitectura

La solucion sigue una arquitectura por capas:

FinanceTracker
├── FinanceTracker.Api
├── FinanceTracker.Application
├── FinanceTracker.Domain
├── FinanceTracker.Infrastructure
└── FinanceTracker.Tests

### 🌐 FinanceTracker.Api

Controladores REST y configuracion de arranque de la aplicacion.

### 📦 FinanceTracker.Application

DTOs, interfaces y contratos de la capa de aplicacion.

### 🧠 FinanceTracker.Domain

Entidades del dominio y enumeraciones.

### 🗄️ FinanceTracker.Infrastructure

Configuracion de Entity Framework Core, contexto de base de datos, migraciones e implementaciones de los servicios.

### 🧪 FinanceTracker.Tests

Pruebas automatizadas de la logica de aplicacion.

## 🔗 Endpoints principales

### 📁 Categorias

GET /api/Categories  
GET /api/Categories/{id}  
POST /api/Categories  
PUT /api/Categories/{id}  
DELETE /api/Categories/{id}

### 💳 Movimientos

GET /api/Transactions  
GET /api/Transactions/{id}  
POST /api/Transactions  
PUT /api/Transactions/{id}  
DELETE /api/Transactions/{id}

### 💰 Presupuestos

GET /api/Budgets  
GET /api/Budgets/{id}  
POST /api/Budgets  
PUT /api/Budgets/{id}  
DELETE /api/Budgets/{id}

### 🔎 Filtros de movimientos

GET /api/Transactions?type=2  
GET /api/Transactions?categoryId=4  
GET /api/Transactions?fromDate=2026-05-01&toDate=2026-05-31

### 📄 Paginacion de movimientos

GET /api/Transactions?pageNumber=1&pageSize=10

Respuesta de ejemplo:

{
  "items": [],
  "totalCount": 25,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 3
}

### 📊 Resumen financiero

GET /api/Transactions/summary  
GET /api/Transactions/summary?fromDate=2026-05-01&toDate=2026-05-31  
GET /api/Transactions/summary?categoryId=4

Respuesta de ejemplo:

{
  "totalIncome": 2000,
  "totalExpense": 350,
  "balance": 1650
}

### 📈 Resumen del panel

GET /api/Dashboard/summary  
GET /api/Dashboard/summary?fromDate=2026-05-01&toDate=2026-05-31  
GET /api/Dashboard/summary?categoryId=4

Respuesta de ejemplo:

{
  "totalIncome": 2000,
  "totalExpense": 350,
  "balance": 1650,
  "transactionCount": 5,
  "latestTransactions": []
}

### 💼 Consumo de presupuesto

GET /api/Budgets

Respuesta de ejemplo:

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

## 📊 Panel

La API incluye un endpoint de resumen que da una vista general de la situacion financiera actual.

GET /api/Dashboard/summary

## 🚀 Puesta en marcha

### ✅ Requisitos

- Visual Studio 2022
- SDK de .NET 8
- Una instancia de SQL Server: LocalDB, el contenedor del compose, o Azure SQL

### 🔐 Configuracion y secretos

`appsettings.json` esta versionado y contiene solo configuracion: registro,
origenes CORS, emisor y audiencia del JWT. **Ahi no vive ningun secreto.** La
cadena de conexion y la clave de firma del JWT entran por fuera.

En local, con user secrets (el proyecto ya tiene `UserSecretsId`):

    cd FinanceTracker.Api
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<tu cadena de conexion>"
    dotnet user-secrets set "Jwt:Key" "<una cadena larga y aleatoria>"

En Azure, como variables de entorno del App Service llamadas
`ConnectionStrings__DefaultConnection` y `Jwt__Key`. El doble guion bajo es como
la plataforma escribe el `:` de la configuracion de .NET.

### 🗃️ Base de datos

Vale cualquier SQL Server: LocalDB, el contenedor del compose o Azure SQL. Apunta
la cadena de conexion y aplica las migraciones.

Desde la consola del administrador de paquetes:

Update-Database

O con la CLI de .NET:

dotnet ef database update

La API aplica sola las migraciones pendientes al arrancar, asi que una instancia
de SQL Server recien creada queda lista sin ningun paso manual. Tambien siembra
la cuenta de demostracion en el primer arranque.

## ▶️ Ejecutar la API

Marca `FinanceTracker.Api` como proyecto de inicio y ejecuta la aplicacion.

Swagger queda disponible en:

https://localhost:{puerto}/swagger

## 🐳 Ejecutar con Docker

El proyecto tambien corre completamente en contenedores (API + SQL Server) con Docker Compose.

### ✅ Requisitos

- Docker Desktop

### 🔐 Variables de entorno

Crea un archivo `.env` en la raiz del proyecto (esta ignorado por git y nunca se commitea) con:

MSSQL_SA_PASSWORD=TuContrasenaFuerte123!
JWT_KEY=tu-clave-jwt-generada

### ▶️ Ejecutar

docker compose up --build

Esto levanta dos contenedores:

- `api` — la API de FinanceTracker, en `http://localhost:8080`
- `db` — SQL Server 2022, en el puerto `1433`

Swagger queda disponible en:

http://localhost:8080/swagger

## 📦 Ejecutar desde la imagen publicada

Cada push a `master` publica una imagen en GitHub Container Registry, de modo que
la aplicacion se puede ejecutar sin clonar el repositorio ni compilar nada.

docker pull ghcr.io/antonicr1986/financetracker:latest

`docker-compose.prod.yml` levanta la imagen publicada junto a SQL Server:

docker compose -f docker-compose.prod.yml up -d

A diferencia del compose de desarrollo, este:

- Descarga la imagen en lugar de compilar desde el codigo
- Guarda la base de datos en un volumen con nombre, asi los datos sobreviven a un reinicio
- Espera a que SQL Server pase su health check antes de arrancar la API
- No expone el puerto 1433 al anfitrion

Para desplegar un commit concreto en lugar de la ultima version:

IMAGE_TAG=sha-<commit-sha> docker compose -f docker-compose.prod.yml up -d

## ⚙️ CI/CD

El proyecto usa GitHub Actions. En cada push y pull request a `master`, el
pipeline automaticamente:

- Restaura y compila la solucion
- Ejecuta las pruebas automatizadas
- Construye la imagen Docker

Ademas escanea **todo el historial del repositorio** en busca de credenciales
filtradas con gitleaks, en un job que no depende de la compilacion: si algo se ha
filtrado, da igual que el codigo compile.

En los push a `master`, ademas:

- Publica la imagen en GitHub Container Registry
- La etiqueta con el SHA completo del commit y con `latest`
- Despliega la API en Azure App Service, solo si las pruebas estan en verde

Las pull requests construyen la imagen para validar el Dockerfile, pero nunca
publican. Como cada compilacion queda etiquetada por SHA, cualquier version
anterior se puede volver a desplegar tal cual, lo que convierte una vuelta atras
en un cambio de una linea.

Archivo del workflow: `.github/workflows/ci.yml`

## 🧪 Ejecutar las pruebas

Desde el explorador de pruebas de Visual Studio o con:

dotnet test

Pruebas automatizadas actuales: 43 en verde.

La cobertura incluye hoy:

- Logica del servicio de categorias
- Logica del servicio de movimientos
- Logica del servicio del panel
- Logica del servicio de presupuestos
- Filtrado
- Actualizacion y borrado de movimientos
- Filtrado de movimientos por categoria y rango de fechas
- Casos de "no encontrado" para presupuestos y movimientos

## 📌 Estado del proyecto

Implementado:

- Estructura por capas
- Entity Framework Core configurado
- Migraciones, aplicadas automaticamente al arrancar
- CRUD de categorias
- CRUD de movimientos
- CRUD de presupuestos
- Endpoint de resumen del panel
- Filtros de movimientos
- Resultados paginados
- Endpoint de resumen financiero
- Calculo del consumo de presupuesto
- Validacion de DTOs con Data Annotations
- Reglas de negocio sobre categorias, movimientos y presupuestos
- Pruebas desde Swagger / OpenAPI
- Pruebas automatizadas con xUnit y EF Core InMemory
- Autenticacion JWT (registro y acceso)
- Aplicacion en contenedores (API + SQL Server con Docker Compose)
- Pipeline de CI/CD con GitHub Actions (compilar, probar, publicar)
- Imagen publicada en GHCR, etiquetada por SHA de commit
- Compose de produccion con health checks y volumen persistente
- Datos financieros acotados al usuario autenticado
- Categorias de partida al registrarse, y cuenta de demostracion sembrada
- Manejo global de excepciones
- Health checks (`/health` liveness, `/health/ready` readiness)
- Registro estructurado con Serilog
- Escaneo de secretos sobre todo el historial con gitleaks en el pipeline
- Desplegado en Azure App Service, publicado automaticamente desde el pipeline
- Errores como ProblemDetails con un `code` independiente del idioma
- Resiliencia ante fallos transitorios de SQL (base serverless en pausa)

Mejoras previstas:

- Informes de presupuesto mas avanzados
- Pruebas de controladores
- Mensajes de validacion traducidos (los que genera ASP.NET siguen en ingles)
- Escaneo de vulnerabilidades de dependencias en el pipeline
- Despliegue en Kubernetes

## 🎯 Proposito

Este proyecto forma parte de mi portfolio como desarrollador .NET.

Busca demostrar estructura de proyecto limpia, desarrollo de APIs, uso de Entity Framework Core, validacion, pruebas y flujos de trabajo con GitHub.
