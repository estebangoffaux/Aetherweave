# Demo apps

Three runnable demos, one per Aetherweave auth package — see [CLAUDE.md](../CLAUDE.md#authentication-model--which-package-for-which-case)
for the full model.

| Project | Demos | Role |
|---|---|---|
| `Zwedze.Aetherweave.Test.Api` | `Security.Jwt` | The protected API all three demos talk to |
| `Zwedze.Aetherweave.Test.Blazor` | `Security.Oidc` | A human logs in via Authorization Code + PKCE, then calls the API as themselves |
| `Zwedze.Aetherweave.Test.Console` | `Security.ClientCredentials` | No human — authenticates as itself and calls the API unattended |

All three assume one shared Keycloak realm. Wherever you see `<YOUR_KEYCLOAK_HOST>` / `<YOUR_REALM>` in
an `appsettings.json`, replace it with your own instance.

## 1. Keycloak realm setup

Create a realm (any name — it just needs to match `<YOUR_REALM>` everywhere below), then three clients
in it:

### `test-api` — the API resource

The audience `Test.Api` checks for (`Aetherweave:Security:JwtBearer:Audience`). In Keycloak this is
usually represented as a **client scope** named `test-api` with an **Audience mapper** ("Included Client
Audience" → the resource you're protecting, or "Included Custom Audience" → `test-api`), so that any
client requesting the `test-api` scope gets that audience in its issued token.

### `test-blazor` — public PKCE client

- Client authentication: **off** (public client)
- Standard flow (Authorization Code): **on**, with PKCE method `S256`
- Valid redirect URIs: `https://localhost:7116/authentication/login-callback`
- Valid post logout redirect URIs: `https://localhost:7116/authentication/logged-out`
- Web origins: `https://localhost:7116`
- Assign the `test-api` client scope to this client (so its tokens carry the `test-api` audience)

### `test-console` — confidential client-credentials client

- Client authentication: **on** (confidential)
- Service accounts roles: **on** (enables the client-credentials grant)
- Standard flow: off (not needed — no browser involved)
- Assign the `test-api` client scope to this client too
- Note the generated **client secret** — you'll need it below

## 2. Fill in placeholders

| File | Placeholder | Replace with |
|---|---|---|
| `Zwedze.Aetherweave.Test.Api/appsettings.json` | `Authority`, `Audience` | your realm's issuer URL, `test-api` |
| `Zwedze.Aetherweave.Test.Blazor/wwwroot/appsettings.json` | `Authority` | your realm's issuer URL |
| `Zwedze.Aetherweave.Test.Console/appsettings.json` | `TokenEndpoint` | your realm's token endpoint |

`Test.Console`'s `ClientSecret` is committed as the obvious placeholder `"CHANGE_ME"` — **never put a
real secret there**. Override it locally instead:

```bash
cd tests/Zwedze.Aetherweave.Test.Console
dotnet user-secrets init
dotnet user-secrets set "Aetherweave:Security:ClientCredentials:test-console:ClientSecret" "<real-secret-from-keycloak>"
```

(or set the equivalent environment variable, `Aetherweave__Security__ClientCredentials__test-console__ClientSecret`)

## 3. Run order

1. **`Test.Api`** — `dotnet run` from `tests/Zwedze.Aetherweave.Test.Api`. Confirm:
   - `GET https://localhost:7055/weatherforecast` → 200, no token needed.
   - `GET https://localhost:7055/weatherforecast/secure` → 401 without a token.
2. **`Test.Blazor`** — `dotnet run` from `tests/Zwedze.Aetherweave.Test.Blazor`, open `https://localhost:7116`.
   Click "Log in", authenticate against Keycloak, then "Fetch weather" — you should see the forecast plus
   your token's claims echoed back from `/weatherforecast/secure`.
3. **`Test.Console`** — `dotnet run` from `tests/Zwedze.Aetherweave.Test.Console`. No browser opens; it
   authenticates as itself via client credentials, calls the same endpoint, prints the result, and exits.
