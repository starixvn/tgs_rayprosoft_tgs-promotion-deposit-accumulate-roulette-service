#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["DepositAccumulateRoulette.Worker/DepositAccumulateRoulette.Worker.csproj", "DepositAccumulateRoulette.Worker/"]
COPY ["DepositAccumulateRoulette.Repositories/DepositAccumulateRoulette.Repositories.csproj", "DepositAccumulateRoulette.Repositories/"]
COPY ["DepositAccumulateRoulette.Worker.Core/DepositAccumulateRoulette.Worker.Core.csproj", "DepositAccumulateRoulette.Worker.Core/"]
COPY ["DepositAccumulateRoulette.Common/DepositAccumulateRoulette.Common.csproj", "DepositAccumulateRoulette.Common/"]
COPY ["DepositAccumulateRoulette.Worker.Cache/DepositAccumulateRoulette.Worker.Cache.csproj", "DepositAccumulateRoulette.Worker.Cache/"]
RUN dotnet restore "DepositAccumulateRoulette.Worker/DepositAccumulateRoulette.Worker.csproj"
COPY . .
WORKDIR "/src/DepositAccumulateRoulette.Worker"
RUN dotnet build "DepositAccumulateRoulette.Worker.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "DepositAccumulateRoulette.Worker.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DepositAccumulateRoulette.Worker.dll"]