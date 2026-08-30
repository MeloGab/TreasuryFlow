# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["global.json", "./"]
COPY ["src/TreasuryFlow.Worker/TreasuryFlow.Worker.csproj", "src/TreasuryFlow.Worker/"]
COPY ["src/TreasuryFlow.Application/TreasuryFlow.Application.csproj", "src/TreasuryFlow.Application/"]
COPY ["src/TreasuryFlow.Contracts/TreasuryFlow.Contracts.csproj", "src/TreasuryFlow.Contracts/"]
COPY ["src/TreasuryFlow.Domain/TreasuryFlow.Domain.csproj", "src/TreasuryFlow.Domain/"]
COPY ["src/TreasuryFlow.Infrastructure/TreasuryFlow.Infrastructure.csproj", "src/TreasuryFlow.Infrastructure/"]

RUN dotnet restore "src/TreasuryFlow.Worker/TreasuryFlow.Worker.csproj"

COPY ["src", "src"]

RUN dotnet publish "src/TreasuryFlow.Worker/TreasuryFlow.Worker.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "TreasuryFlow.Worker.dll"]
