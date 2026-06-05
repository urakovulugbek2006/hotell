# syntax=docker/dockerfile:1
# Build context is the repository root (see docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Shared/ ./Shared/
COPY src/Services/FrontendService/ ./Services/FrontendService/
RUN dotnet restore ./Services/FrontendService/FrontendService.csproj

RUN dotnet publish ./Services/FrontendService/FrontendService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5006
EXPOSE 5006
ENTRYPOINT ["dotnet", "FrontendService.dll"]