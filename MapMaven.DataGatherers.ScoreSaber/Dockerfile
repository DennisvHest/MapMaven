#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MapMaven.DataGatherers.ScoreSaber/MapMaven.DataGatherers.ScoreSaber.csproj", "MapMaven.DataGatherers.ScoreSaber/"]
RUN dotnet restore "MapMaven.DataGatherers.ScoreSaber/MapMaven.DataGatherers.ScoreSaber.csproj"
COPY . .
WORKDIR "/src/MapMaven.DataGatherers.ScoreSaber"
RUN dotnet build "MapMaven.DataGatherers.ScoreSaber.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "MapMaven.DataGatherers.ScoreSaber.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MapMaven.DataGatherers.ScoreSaber.dll"]