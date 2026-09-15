# Steam OpenId Connect Provider

Steam OpenID 2.0 -> OpenID Connect Provider Proxy

## About

This server allows you to use Steam as an OpenID Connect Identity provider (OIDC IDP). This way you can use Steam logins in KeyCloak or any other OpenID Connect based authentication client.

## Setup

Add your Steam API Key as user-secrets like this:
`dotnet user-secrets set "Steam:ApplicationKey" "MySteamApiKey"`

Alternatively you can set it up via environment variables:
`Steam__ApplicationKey=MySteamApiKey`
(Keep in mind that this is easier but more insecure)

After that set up your redirect URI, ClientID and ClientSecret in the appsettings.json.

If you need to set up multiple redirect URIs, you can set them separated by a comma.
`OpenId__RedirectUri="http://localhost:8080/auth/realms/master/broker/steam/endpoint, http://localhost:8080/auth/realms/dev/broker/steam/endpoint"`

## OpenID Configuration

You can access Authorization and Token endpoint details in
`http://<Your Host>/.well-known/openid-configuration`

## Supported Scopes

The following scopes are supported: `openid`, `profile`.

If you use the `profile` scope, you get access to the `picture`, `given_name`, `website` and `nickname` claims too.

## HTTPS Support

The server itself does not support https connections. If you want to use https connections please use a reverse proxy like nginx. The [example docker-compose.yml](https://github.com/byo-software/steam-openid-connect-provider/blob/master/docker-compose.yml) contains an example setup.

## Running behind reverse proxies

Some reverse proxy setups might require additional configuration, like setting the path base or origin.

To set the origin, set `Hosting:PublicOrigin` in the appsettings.json or the `Hosting__PublicOrigin` environment variable to your desired origin. If not set, the origin name is inferred from the request.

Similary, you can use the `Hosting:PathBase` in the appsettings.json or the `Hosting__PathBase` environment variable to set the path base. If not set, it will default to "/".

## Login timeout

Users have a limited amount of time to complete the Steam login flow before it times out and has to be restarted. To change this, set `Steam:LoginTimeoutMinutes` in the appsettings.json or the `Steam__LoginTimeoutMinutes` environment variable to the desired number of minutes. If not set, it defaults to 15 minutes.

Keep in mind that this timeout also controls how long the login's anti-forgery (correlation) cookie stays valid. A longer value widens the window for login CSRF and replay of an in-flight login, so it is best to keep it as short as your users can tolerate and to avoid values of hours or days.

## Returning login errors to the client

When a Steam login cannot be completed (for example the user takes too long and the login times out), the request fails with an HTTP 500 error by default. This is often fine: a reverse proxy in front of the service can catch the 500 and show a custom error page, and failed logins are easy to spot in logs and monitoring.

If you would rather send the user back to the client (Keycloak or any other OpenID Connect client) with a standard error response instead, set `OpenId:ReturnErrorsToClient` in the appsettings.json or the `OpenId__ReturnErrorsToClient` environment variable to `true`. It defaults to `false`. When enabled, the user is redirected back to the client's redirect URI with an `error=temporarily_unavailable` response and the original `state`, so the client can show a message and let the user try again. If the original request cannot be determined, the login falls back to the HTTP 500 error.

## Health checks

This service contains a health check endpoint at `/health`. It checks if the Steam login servers are working.

## Docker

[A docker image](https://github.com/byo-software/steam-openid-connect-provider/pkgs/container/steam-openid-connect-provider) is also available.

```
docker run -it \
    -e OpenId__RedirectUri=http://localhost:8080/auth/realms/master/broker/steam/endpoint \
    -e OpenId__ClientId=steamidp \
    -e OpenId__ClientSecret=mysecret \
    -e OpenId__ClientName=myclientname \
    -e Steam__ApplicationKey=MySteamApiKey \
    --restart unless-stopped \
    --name steamidp \
    ghcr.io/byo-software/steam-openid-connect-provider
```

## License

[MIT](https://github.com/byo-software/steam-openid-connect-provider/blob/master/LICENSE)
