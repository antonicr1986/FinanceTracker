FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos solo los .csproj primero para aprovechar la cache de Docker
COPY FinanceTracker.Api/FinanceTracker.Api.csproj FinanceTracker.Api/
COPY FinanceTracker.Application/FinanceTracker.Application.csproj FinanceTracker.Application/
COPY FinanceTracker.Domain/FinanceTracker.Domain.csproj FinanceTracker.Domain/
COPY FinanceTracker.Infraestructure/FinanceTracker.Infrastructure.csproj FinanceTracker.Infraestructure/
COPY FinanceTracker.Tests/FinanceTracker.Tests.csproj FinanceTracker.Tests/

RUN dotnet restore FinanceTracker.Api/FinanceTracker.Api.csproj

# Ahora copiamos todo el código y publicamos
COPY . .
RUN dotnet publish FinanceTracker.Api/FinanceTracker.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "FinanceTracker.Api.dll"]