# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for caching restore layer (only the WebApi project graph; tests are not part of the image)
COPY src/Domain/NutriScan.Domain.csproj src/Domain/
COPY src/Application/NutriScan.Application.csproj src/Application/
COPY src/Infrastructure/NutriScan.Infrastructure.csproj src/Infrastructure/
COPY src/WebApi/NutriScan.WebApi.csproj src/WebApi/

RUN dotnet restore src/WebApi/NutriScan.WebApi.csproj

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
