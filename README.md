# Steam OpenId Connect Provider
Steam OpenID 2.0 -> OpenID Connect Provider Proxy

## About
Steam still uses the old OpenID 2.0 authentication protocol. Since ImperialPlugins.com has migrated to KeyCloak we were unable to migrate our old Steam logins as KeyCloak does not support OpenID 2.0.

This server will act as an OpenID Connect provider which will provide Steam authentication for you. This way you can use Steam logins in KeyCloak or any other OpenID Connect based authentication client. 

Note: only "openid" and "profile" scopes are supported due limitations by Valve/Steam.

## Setup
Add your Steam API Key as user-secrets like this:
`dotnet user-secrets set "Authentication:Steam:ApplicationKey" "MySteamApiKey"`

Alternatively you can set it up via environment variables:
`-e Authentication__Steam__ApplicationKey: 'Your Steam ApiKey‘`
(Keep in mind that this is easier but more insecure)

After that set up your redirect URI, ClientID and ClientSecret in the appsettings.json.

## OpenID Configuration
You can access Authorization and Token endpoint details in
`http://<Your Host>/.well-known/openid-configuration`

## HTTPS Support
This server does not support https connections. If you want to use https connections please use a reverse proxy like nginx.

## Running behind reverse proxies
Some reverse proxy setups might require additional configuration, like setting the path base or origin.

To set the origin, set `Hosting:PublicOrigin` in the appsettings.json or the `Hosting__PublicOrigin` environment variable to your desired origin. If not set, the origin name is inferred from the request.

Similary, you can use the `Hosting:PathBase` in the appsettings.json or the `Hosting__PathBase` environment variable to set the path base. If not set, it will default to "/".

## Health checks
This service contains a health check endpoint at `/health`. It checks if the Steam login server is up.

## Docker
[A docker image](https://hub.docker.com/r/imperialplugins/steam-openid-connect-provider) is also available.

```
docker run -it \
    -e OpenID__RedirectUri=http://localhost:8080/auth/realms/master/broker/steam/endpoint \
    -e OpenID__ClientID=steamidp \ 
    -e OpenID__ClientSecret=mysecret \
    -e Authentication__Steam__ApplicationKey=MySteamApiKey \
    --restart unless-stopped \
    --name steamidp \
    imperialplugins/steam-openid-connect-provider
```
## License
[MIT](https://github.com/ImperialPlugins/steam-openid-connect-provider/blob/master/LICENSE)
