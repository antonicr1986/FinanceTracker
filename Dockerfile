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
# Aplica los parches de seguridad de Debian que aun no trae la imagen base de
# Microsoft (p. ej. perl-base), para que Trivy no bloquee el pipeline.
RUN apt-get update \
 && apt-get upgrade -y --no-install-recommends \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "FinanceTracker.Api.dll"]