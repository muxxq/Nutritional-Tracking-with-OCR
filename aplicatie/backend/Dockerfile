# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files for caching restore layer
COPY NutriScan.sln ./
COPY src/Domain/NutriScan.Domain.csproj src/Domain/
COPY src/Application/NutriScan.Application.csproj src/Application/
COPY src/Infrastructure/NutriScan.Infrastructure.csproj src/Infrastructure/
COPY src/WebApi/NutriScan.WebApi.csproj src/WebApi/
COPY tests/Domain.UnitTests/NutriScan.Domain.UnitTests.csproj tests/Domain.UnitTests/
COPY tests/Application.UnitTests/NutriScan.Application.UnitTests.csproj tests/Application.UnitTests/

RUN dotnet restore

# Copy full source and publish
COPY . .
WORKDIR /src/src/WebApi
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NutriScan.WebApi.dll"]
