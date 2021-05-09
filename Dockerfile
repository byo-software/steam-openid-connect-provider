FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /src

# Copy the project file to create layer with packages
COPY src/SteamOpenIdConnectProvider.csproj .
RUN dotnet restore ./SteamOpenIdConnectProvider.csproj

# Copy the rest of the source
COPY src/* .
RUN dotnet build ./SteamOpenIdConnectProvider.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish ./SteamOpenIdConnectProvider.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
COPY --from=publish /app .
EXPOSE 80

HEALTHCHECK CMD curl --fail http://localhost/health || exit 1
ENTRYPOINT ["dotnet", "SteamOpenIdConnectProvider.dll"]