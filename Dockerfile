FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["src/SteamOpenIdConnectProxy.csproj", "SteamOpenIdConnectProxy/"]
RUN dotnet restore "SteamOpenIdConnectProxy/SteamOpenIdConnectProxy.csproj"
COPY ["src/", "SteamOpenIdConnectProxy/"]
WORKDIR "/src/SteamOpenIdConnectProxy"
RUN dotnet build "SteamOpenIdConnectProxy.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "SteamOpenIdConnectProxy.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SteamOpenIdConnectProxy.dll"]