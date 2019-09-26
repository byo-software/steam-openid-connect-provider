FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["src/SteamOpenIdConnectProxy.csproj", "SteamOpenIdConnectProxy/"]
RUN dotnet restore "SteamOpenIdConnectProxy/SteamOpenIdConnectProxy.csproj"
COPY ["src/", "SteamOpenIdConnectProxy/"]
WORKDIR "/src/SteamOpenIdConnectProxy"
RUN dotnet build "SteamOpenIdConnectProxy.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "SteamOpenIdConnectProxy.csproj" -c Release -o /app

FROM bmcr.microsoft.com/dotnet/core/aspnet:3.0 AS base
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SteamOpenIdConnectProxy.dll"]