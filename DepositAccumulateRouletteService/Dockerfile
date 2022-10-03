#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["DepositAccumulateRouletteService/DepositAccumulateRouletteService.csproj", "DepositAccumulateRouletteService/"]
COPY ["DepositAccumulateRoulette.Common/DepositAccumulateRoulette.Common.csproj", "DepositAccumulateRoulette.Common/"]
COPY ["DepositAccumulateRoulette.Core/DepositAccumulateRoulette.Core.csproj", "DepositAccumulateRoulette.Core/"]
RUN dotnet restore "DepositAccumulateRouletteService/DepositAccumulateRouletteService.csproj"
COPY . .
WORKDIR "/src/DepositAccumulateRouletteService"
RUN dotnet build "DepositAccumulateRouletteService.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "DepositAccumulateRouletteService.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DepositAccumulateRouletteService.dll"]