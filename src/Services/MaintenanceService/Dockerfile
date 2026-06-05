# syntax=docker/dockerfile:1
# Build context is the repository root (see docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Shared/ ./Shared/
COPY src/Services/MaintenanceService/ ./Services/MaintenanceService/
RUN dotnet restore ./Services/MaintenanceService/MaintenanceService.csproj

RUN dotnet publish ./Services/MaintenanceService/MaintenanceService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5004
EXPOSE 5004
ENTRYPOINT ["dotnet", "MaintenanceService.dll"]