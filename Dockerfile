FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
WORKDIR /src
COPY ["src/SteamOpenIdConnectProvider.csproj", "SteamOpenIdConnectProvider/"]
RUN dotnet restore "SteamOpenIdConnectProvider/SteamOpenIdConnectProvider.csproj"
COPY ["src/", "SteamOpenIdConnectProvider/"]
WORKDIR "/src/SteamOpenIdConnectProvider"
RUN dotnet build "SteamOpenIdConnectProvider.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "SteamOpenIdConnectProvider.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.0 AS base
WORKDIR /app
COPY --from=publish /app .
HEALTHCHECK CMD curl --fail http://localhost/health || exit 1
ENTRYPOINT ["dotnet", "SteamOpenIdConnectProvider.dll"]
