# syntax=docker/dockerfile:1
# Build context is the repository root (see docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Shared/ ./Shared/
COPY src/Services/HousekeepingService/ ./Services/HousekeepingService/
RUN dotnet restore ./Services/HousekeepingService/HousekeepingService.csproj

RUN dotnet publish ./Services/HousekeepingService/HousekeepingService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5002
EXPOSE 5002
ENTRYPOINT ["dotnet", "HousekeepingService.dll"]