FROM hub.lumigame.com/base/dotnet/core/sdk:8.0 AS builder

ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["DepositAccumulateRouletteService/DepositAccumulateRouletteService.csproj", "DepositAccumulateRouletteService/"]
COPY ["DepositAccumulateRoulette.Common/DepositAccumulateRoulette.Common.csproj", "DepositAccumulateRoulette.Common/"]
COPY ["DepositAccumulateRouletteService.Core/DepositAccumulateRouletteService.Core.csproj", "DepositAccumulateRouletteService.Core/"]
RUN dotnet restore "./DepositAccumulateRouletteService/./DepositAccumulateRouletteService.csproj"
COPY . .
WORKDIR "/src/DepositAccumulateRouletteService"

RUN dotnet publish "./DepositAccumulateRouletteService.csproj" -c $BUILD_CONFIGURATION -o /app /p:UseAppHost=false

FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
WORKDIR /app
COPY --from=builder /app .

ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "DepositAccumulateRouletteService.dll"]