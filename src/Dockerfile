FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy the project file to create layer with packages
COPY SteamOpenIdConnectProvider.csproj .
RUN dotnet restore ./SteamOpenIdConnectProvider.csproj

# Copy the rest of the source
COPY . .
RUN dotnet build ./SteamOpenIdConnectProvider.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish ./SteamOpenIdConnectProvider.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0

RUN apt-get update && \
    apt-get install -y curl

WORKDIR /app
COPY --from=publish /app .
EXPOSE 80

HEALTHCHECK CMD curl --fail http://localhost/health || exit 1
ENTRYPOINT ["dotnet", "SteamOpenIdConnectProvider.dll"]