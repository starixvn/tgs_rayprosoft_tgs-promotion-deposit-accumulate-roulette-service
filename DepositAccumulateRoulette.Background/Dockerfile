#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["DepositAccumulateRoulette.Background/DepositAccumulateRoulette.Background.csproj", "DepositAccumulateRoulette.Background/"]
COPY ["DepositAccumulateRoulette.Background.Core/DepositAccumulateRoulette.Background.Core.csproj", "DepositAccumulateRoulette.Background.Core/"]
COPY ["DepositAccumulateRoulette.Common/DepositAccumulateRoulette.Common.csproj", "DepositAccumulateRoulette.Common/"]
COPY ["DepositAccumulateRoulette.Background.Repositories/DepositAccumulateRoulette.Background.Repositories.csproj", "DepositAccumulateRoulette.Background.Repositories/"]
RUN dotnet restore "./DepositAccumulateRoulette.Background/./DepositAccumulateRoulette.Background.csproj"
COPY . .
WORKDIR "/src/DepositAccumulateRoulette.Background"
RUN dotnet build "./DepositAccumulateRoulette.Background.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DepositAccumulateRoulette.Background.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DepositAccumulateRoulette.Background.dll"]