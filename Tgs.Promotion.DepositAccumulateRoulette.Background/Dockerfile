#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Promotion.DepositAccumulateRoulette.Background/Tgs.Promotion.DepositAccumulateRoulette.Background.csproj", "Tgs.Promotion.DepositAccumulateRoulette.Background/"]
COPY ["Tgs.Promotion.DepositAccumulateRoulette.Background.Repositories/Tgs.Promotion.DepositAccumulateRoulette.Background.Repositories.csproj", "Tgs.Promotion.DepositAccumulateRoulette.Background.Repositories/"]
COPY ["Tgs.Promotion.DepositAccumulateRoulette.Background.Core/Tgs.Promotion.DepositAccumulateRoulette.Background.Core.csproj", "Tgs.Promotion.DepositAccumulateRoulette.Background.Core/"]
COPY ["Tgs.Promotion.DepositAccumulateRoulette.Common/Tgs.Promotion.DepositAccumulateRoulette.Common.csproj", "Tgs.Promotion.DepositAccumulateRoulette.Common/"]
RUN dotnet restore "Tgs.Promotion.DepositAccumulateRoulette.Background/Tgs.Promotion.DepositAccumulateRoulette.Background.csproj"
COPY . .
WORKDIR "/src/Tgs.Promotion.DepositAccumulateRoulette.Background"
RUN dotnet build "Tgs.Promotion.DepositAccumulateRoulette.Background.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.Promotion.DepositAccumulateRoulette.Background.csproj" -c $BuildConfiguration -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Promotion.DepositAccumulateRoulette.Background.dll"]