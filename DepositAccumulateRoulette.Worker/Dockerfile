FROM hub.lumigame.com/base/dotnet/core/sdk:8.0 AS builder

ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["DepositAccumulateRoulette.Worker/DepositAccumulateRoulette.Worker.csproj", "DepositAccumulateRoulette.Worker/"]
COPY ["DepositAccumulateRoulette.Repositories/DepositAccumulateRoulette.Repositories.csproj", "DepositAccumulateRoulette.Repositories/"]
COPY ["DepositAccumulateRoulette.Worker.Core/DepositAccumulateRoulette.Worker.Core.csproj", "DepositAccumulateRoulette.Worker.Core/"]
COPY ["DepositAccumulateRoulette.Common/DepositAccumulateRoulette.Common.csproj", "DepositAccumulateRoulette.Common/"]
COPY ["DepositAccumulateRoulette.Worker.Cache/DepositAccumulateRoulette.Worker.Cache.csproj", "DepositAccumulateRoulette.Worker.Cache/"]
RUN dotnet restore "./DepositAccumulateRoulette.Worker/./DepositAccumulateRoulette.Worker.csproj"
COPY . .
WORKDIR "/src/DepositAccumulateRoulette.Worker"
RUN dotnet publish "./DepositAccumulateRoulette.Worker.csproj" -c $BUILD_CONFIGURATION -o /app /p:UseAppHost=false

FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
WORKDIR /app
COPY --from=builder /app .

ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "DepositAccumulateRoulette.Worker.dll"]