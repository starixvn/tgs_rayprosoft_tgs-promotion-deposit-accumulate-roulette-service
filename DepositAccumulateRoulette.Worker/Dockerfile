#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BuildConfiguration=Release
RUN --mount=type=secret,id=nuget,dst=/pat cp /pat ~/.nuget/NuGet/NuGet.Config
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Promotion.DepositAccumulateRoulette.Worker/Tgs.Promotion.DepositAccumulateRoulette.Worker.csproj", "Tgs.Promotion.DepositAccumulateRoulette.Worker/"]
COPY ["Tgs.Promotion.DepositAccumulateRoulette.Repositories/Tgs.Promotion.DepositAccumulateRoulette.Repositories.csproj", "Tgs.Promotion.DepositAccumulateRoulette.Repositories/"]
COPY ["Tgs.Promotion.DepositAccumulateRoulette.Worker.Core/Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.csproj", "Tgs.Promotion.DepositAccumulateRoulette.Worker.Core/"]
COPY ["Tgs.Promotion.DepositAccumulateRoulette.Common/Tgs.Promotion.DepositAccumulateRoulette.Common.csproj", "Tgs.Promotion.DepositAccumulateRoulette.Common/"]
COPY ["Tgs.Promotion.DepositAccumulateRoulette.Worker.Cache/Tgs.Promotion.DepositAccumulateRoulette.Worker.Cache.csproj", "Tgs.Promotion.DepositAccumulateRoulette.Worker.Cache/"]
RUN dotnet restore "Tgs.Promotion.DepositAccumulateRoulette.Worker/Tgs.Promotion.DepositAccumulateRoulette.Worker.csproj"
COPY . .
WORKDIR "/src/Tgs.Promotion.DepositAccumulateRoulette.Worker"
RUN dotnet build "Tgs.Promotion.DepositAccumulateRoulette.Worker.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.Promotion.DepositAccumulateRoulette.Worker.csproj" -c $BuildConfiguration -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Promotion.DepositAccumulateRoulette.Worker.dll"]