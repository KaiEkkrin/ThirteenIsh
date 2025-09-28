#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ThirteenIsh.Database.Entities/ThirteenIsh.Database.Entities.csproj", "ThirteenIsh.Database.Entities/"]
COPY ["ThirteenIsh.Database/ThirteenIsh.Database.csproj", "ThirteenIsh.Database/"]
COPY ["ThirteenIsh.Parsing/ThirteenIsh.Parsing.csproj", "ThirteenIsh.Parsing/"]
COPY ["ThirteenIsh.Game/ThirteenIsh.Game.csproj", "ThirteenIsh.Game/"]
COPY ["ThirteenIsh/ThirteenIsh.csproj", "ThirteenIsh/"]
RUN dotnet restore "./ThirteenIsh/./ThirteenIsh.csproj"
COPY . .
WORKDIR "/src/ThirteenIsh"
RUN dotnet build "./ThirteenIsh.csproj" --no-restore -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ThirteenIsh.csproj" --no-restore -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ThirteenIsh.dll"]