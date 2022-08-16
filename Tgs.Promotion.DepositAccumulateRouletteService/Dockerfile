#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Promotion.DepositAccumulateRouletteService/Tgs.Promotion.DepositAccumulateRouletteService.csproj", "Tgs.Promotion.DepositAccumulateRouletteService/"]
COPY ["Tgs.Promotion.DepositAccumulateRoulette.Cache/Tgs.Promotion.DepositAccumulateRoulette.Cache.csproj", "Tgs.Promotion.DepositAccumulateRoulette.Cache/"]
COPY ["Tgs.Promotion.DepositAccumulateRoulette.Core/Tgs.Promotion.DepositAccumulateRoulette.Core.csproj", "Tgs.Promotion.DepositAccumulateRoulette.Core/"]
COPY ["Tgs.Promotion.DepositAccumulateRoulette.Common/Tgs.Promotion.DepositAccumulateRoulette.Common.csproj", "Tgs.Promotion.DepositAccumulateRoulette.Common/"]
COPY ["Tgs.Promotion.DepositAccumulateRoulette.Repositories/Tgs.Promotion.DepositAccumulateRoulette.Repositories.csproj", "Tgs.Promotion.DepositAccumulateRoulette.Repositories/"]
RUN dotnet restore "Tgs.Promotion.DepositAccumulateRouletteService/Tgs.Promotion.DepositAccumulateRouletteService.csproj"
COPY . .
WORKDIR "/src/Tgs.Promotion.DepositAccumulateRouletteService"
RUN dotnet build "Tgs.Promotion.DepositAccumulateRouletteService.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.Promotion.DepositAccumulateRouletteService.csproj" -c $BuildConfiguration -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Promotion.DepositAccumulateRouletteService.dll"]