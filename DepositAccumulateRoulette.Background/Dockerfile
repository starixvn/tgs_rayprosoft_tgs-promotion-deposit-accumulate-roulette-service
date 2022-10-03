#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["DepositAccumulateRoulette.Background/DepositAccumulateRoulette.Background.csproj", "DepositAccumulateRoulette.Background/"]
COPY ["DepositAccumulateRoulette.Background.Core/DepositAccumulateRoulette.Background.Core.csproj", "DepositAccumulateRoulette.Background.Core/"]
COPY ["DepositAccumulateRoulette.Common/DepositAccumulateRoulette.Common.csproj", "DepositAccumulateRoulette.Common/"]
COPY ["DepositAccumulateRoulette.Background.Repositories/DepositAccumulateRoulette.Background.Repositories.csproj", "DepositAccumulateRoulette.Background.Repositories/"]
RUN dotnet restore "DepositAccumulateRoulette.Background/DepositAccumulateRoulette.Background.csproj"
COPY . .
WORKDIR "/src/DepositAccumulateRoulette.Background"
RUN dotnet build "DepositAccumulateRoulette.Background.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "DepositAccumulateRoulette.Background.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DepositAccumulateRoulette.Background.dll"]