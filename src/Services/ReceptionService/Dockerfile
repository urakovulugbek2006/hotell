# syntax=docker/dockerfile:1
# Build context is the repository root (see docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared project and the service project, then restore
COPY src/Shared/ ./Shared/
COPY src/Services/ReceptionService/ ./Services/ReceptionService/
RUN dotnet restore ./Services/ReceptionService/ReceptionService.csproj

# Publish
RUN dotnet publish ./Services/ReceptionService/ReceptionService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001
ENTRYPOINT ["dotnet", "ReceptionService.dll"]