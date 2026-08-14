# Environment Configuration

How Commerce is configured across **Development**, **Staging**, and **Production**. Production secrets must **never** be committed to source control.

## Configuration layers

ASP.NET Core loads settings in this order (later wins):

1. `appsettings.json`
2. `appsettings.{Environment}.json` (`Development`, `Staging`, `Production`)
3. Environment variables (`Commerce__Section__Key`)
4. `App_Data/commerce.database.json` (persisted after installation database step)

Docker Compose supplies connection strings and secrets via `.env` → environment variables.

## Environment matrix

| Setting | Development | Staging | Production |
|---------|-------------|---------|------------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Staging` | `Production` |
| `Commerce:Environment` | `Development` | `Staging` | `Production` |
| `Commerce:BaseUrl` | `http://localhost:8080` | `https://staging.example.com` | `https://shop.example.com` |
| `Commerce:Database:ConnectionString` | `.env` / install wizard | `.env` / secrets | secrets only |
| `Commerce:Cache:Provider` | `Redis` (compose) or `Memory` (local IDE) | `Redis` | `Redis` |
| `Commerce:Cache:RedisConnectionString` | `redis:6379` | `redis:6379` | `redis:6379` |
| `Commerce:Deployment:ApplyMigrationsOnStartup` | `false` | `true` | `true` |
| `Commerce:Deployment:WaitForDatabaseSeconds` | `90` | `90` | `120` |
| `Commerce:Media:StorageRoot` | `App_Data/media` | `App_Data/media` | `App_Data/media` |
| `Commerce:DisasterRecovery:EnableScheduledBackups` | `false` | `true` | `true` |
| Logging | Console (default) | JSON console | JSON console |
| TLS | None (direct port) | Caddy | Caddy + HSTS |

## Environment variables reference

Use double underscore for nested keys:

| Variable | Required | Description |
|----------|----------|-------------|
| `MSSQL_SA_PASSWORD` | Docker SQL | SQL Server SA password |
| `COMMERCE_BASE_URL` | Yes | Public URL of the API (install + callbacks) |
| `COMMERCE_DOMAIN` | Staging/prod | Hostname for Caddy |
| `CADDY_EMAIL` | Staging/prod | ACME account email |
| `Commerce__Database__ConnectionString` | After install | Full SQL connection string |
| `Commerce__Cache__RedisConnectionString` | Staging/prod | Redis host:port |
| `Commerce__Deployment__ApplyMigrationsOnStartup` | Optional | `true` for auto-migrate |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Behind proxy | `true` in Docker image |

Template: [`deploy/docker/.env.example`](../../deploy/docker/.env.example)

## Storage paths

| Path | Purpose | Docker volume |
|------|---------|---------------|
| `App_Data/media/` | Product images, CMS assets, **digital download files** | `commerce-app-data` |
| `App_Data/backups/` | DR module backup sets | `commerce-app-data` |
| `App_Data/commerce.database.json` | Persisted DB config (post-install) | `commerce-app-data` |
| `Plugins/` | Plugin packages (DLL + manifest) | Image baked; optional RO mount |

Downloads use the same media storage backend (`MediaDownloadStorage`).

## Secrets (do not commit)

| Secret | Where to store |
|--------|----------------|
| SQL SA / app login password | `.env`, vault, Docker secret |
| Redis password (if enabled) | env / vault |
| JWT / auth signing keys | env / vault |
| Payment provider API keys | Admin settings (DB), not appsettings |
| Webhook HMAC secrets | Admin API clients UI |
| TLS private keys | Caddy volume or mounted certs |

`.gitignore` excludes:

- `deploy/docker/.env`
- `deploy/docker/secrets/`
- `*.pfx`, `*.pem` (except documented examples)

## Development (local IDE)

Run the host without Docker:

```bash
dotnet run --project src/Commerce/Host/Commerce.Host
```

Use `appsettings.json` + User Secrets for connection strings:

```bash
dotnet user-secrets set "Commerce:Database:ConnectionString" "Server=localhost;..."
```

Default CORS allows `http://localhost:4200` and `4201` for Angular dev servers.

## Development (Docker Compose)

See [DEPLOYMENT.md](./DEPLOYMENT.md). Connection string is injected from `.env`; installation wizard available at `/installation`.

## Staging

1. `ASPNETCORE_ENVIRONMENT=Staging` — loads `appsettings.Staging.json`
2. Redis required for cache parity with production
3. Caddy terminates TLS; commerce listens on internal `:8080` only
4. Enable scheduled backups for DR rehearsal

## Production

1. `ASPNETCORE_ENVIRONMENT=Production` — loads `appsettings.Production.json`
2. All secrets via environment or secret store — **empty** `ConnectionString` in committed JSON
3. `ApplyMigrationsOnStartup=true` for rolling upgrades
4. Forwarded headers enabled for correct scheme/host behind Caddy
5. Set `Commerce:BaseUrl` to public HTTPS URL (payment callbacks, emails, SEO)

## Frontend configuration

| App | Dev | Production build |
|-----|-----|------------------|
| Storefront | `environment.ts` → API proxy | `environment.production.ts` → `apiBaseUrl: '/'` (same-origin) or CDN URL |
| Admin | `localhost:4201` | Served behind same domain or subdomain |

Deploy Angular builds to static hosting or serve via reverse proxy; API remains Commerce.Host.

## Post-install settings

Many operational values live in the database `Settings` table (editable in admin). Environment variables bootstrap the host; admin UI owns runtime business configuration.

## Validation checklist

- [ ] `.env` not tracked by git
- [ ] `Commerce:BaseUrl` matches public URL
- [ ] Redis reachable from commerce container
- [ ] SQL reachable; migrations applied
- [ ] `/health/ready` Healthy after install
- [ ] Media upload writes to persistent volume
- [ ] CORS origins include storefront/admin origins

## Related

- [DEPLOYMENT.md](./DEPLOYMENT.md) — Docker Compose, rollback, health
- [COMMERCE-OPERATIONS.md](./COMMERCE-OPERATIONS.md) — logging and monitoring
- [DISASTER-RECOVERY.md](./DISASTER-RECOVERY.md) — backups
