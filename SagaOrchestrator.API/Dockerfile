FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY nuget.config .
RUN --mount=type=secret,id=nuget_token \
    sed -i "s|%NUGET_TOKEN%|$(cat /run/secrets/nuget_token)|g" nuget.config


COPY ["SagaOrchestrator.API/SagaOrchestrator.API.csproj", "SagaOrchestrator.API/"]
COPY ["SagaOrchestrator.Application/SagaOrchestrator.Application.csproj", "SagaOrchestrator.Application/"]
COPY ["SagaOrchestrator.Infrastructure/SagaOrchestrator.Infrastructure.csproj", "SagaOrchestrator.Infrastructure/"]

RUN dotnet restore "SagaOrchestrator.API/SagaOrchestrator.API.csproj" --configfile nuget.config

COPY . .

WORKDIR "/src/SagaOrchestrator.API"
RUN dotnet build "./SagaOrchestrator.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SagaOrchestrator.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SagaOrchestrator.API.dll"]
