# SteamOpenIdConnectProxy
Steam OpenID 2.0 -> OpenID Connect Proxy

## About
Steam still uses the old OpenID 2.0 authentication protocol. Since ImperialPlugins.com has migrated to KeyCloak we were unable to migrate our old Steam logins as KeyCloak does not support OpenID 2.0.

This server will act as an OpenID Connect provider which will provide Steam authentication for you. This way you can use Steam logins in KeyCloak or any other OpenID Connect based authentication client. 

Note: only "openid" and "profile" scopes are supported due limitations by Valve/Steam.

## Setup
Add your Steam API Key as user-secrets like this:
`dotnet user-secrets set "Authentication:Steam:ApplicationKey" "MySteamApiKey"`

After that set up your redirect URI, ClientID and ClientSecret in the appsettings.json.

## License
[MIT](https://github.com/ImperialPlugins/SteamOpenIdConnectProxy/blob/master/LICENSE)
