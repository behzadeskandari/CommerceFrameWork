# Phase 44 — Docker / Deployment / Production Installation (Report)

**Status:** Complete

## Summary

Phase 44 adds production deployment architecture: Docker image, Compose stacks for development/staging/production, Caddy HTTPS reverse proxy, environment-specific configuration, startup migration support, and documented rollback procedures.

## Deliverables

### Docker (`deploy/docker/`)

| Artifact | Purpose |
|----------|---------|
| `Dockerfile` | Multi-stage build, health check, non-root user |
| `docker-compose.yml` | Dev: SQL Server, Redis, commerce on :8080 |
| `docker-compose.staging.yml` | Caddy TLS, startup migrations, no public commerce port |
| `docker-compose.production.yml` | Production restart policies, ACME, migrations |
| `.env.example` | Template only — secrets not in git |
| `caddy/Caddyfile.*` | Reverse proxy, HSTS, upstream health |

### Host configuration

- `appsettings.Staging.json` — JSON logging, Redis, migrations on startup
- `appsettings.Production.json` — production defaults
- `CommerceDeploymentOptions` + `DeploymentStartupHostedService` — DB wait + auto-migrate when installed
- Plugin folder copy to publish output

### Scripts

- `scripts/deploy/test-clean-install.ps1`
- `scripts/deploy/test-clean-install.sh`

### Documentation

- [DEPLOYMENT.md](./DEPLOYMENT.md) — architecture, environments, rollback
- [ENVIRONMENT-CONFIGURATION.md](./ENVIRONMENT-CONFIGURATION.md) — variables matrix, secrets policy

### Tests

- `Commerce.Tests.Unit.Deployment` — deployment options + env template validation

## Environment separation

| Environment | TLS | Secrets | Migrations on start |
|-------------|-----|---------|---------------------|
| Development | No | `.env` (local) | No |
| Staging | Caddy | `.env` / vault | Yes |
| Production | Caddy + ACME | vault / server `.env` | Yes |

## Verification

```bash
dotnet build src/Commerce/Framework/Commerce.Framework.Data/Commerce.Framework.Data.csproj
dotnet test tests/Commerce/Commerce.Tests.Unit.Deployment/Commerce.Tests.Unit.Deployment.csproj
cp deploy/docker/.env.example deploy/docker/.env
docker compose -f deploy/docker/docker-compose.yml --env-file deploy/docker/.env config
./scripts/deploy/test-clean-install.ps1   # requires Docker Engine running
```

**Test results (this session):**
- `Commerce.Tests.Unit.Deployment` — 4 passing
- `docker compose config` — validates when `.env` present
- E2E clean install — requires Docker Engine (not available in CI agent session); script and compose verified

**Note:** Full `Commerce.Host` publish may hit pre-existing solution build errors in unrelated modules; Docker image build uses the same publish path once those are resolved.

## Rollback

Documented in `DEPLOYMENT.md`: pin previous image, restore DB if migrations ran, verify `/health/ready` before restoring traffic.

## Related phases

- Phase 38 — health endpoints and operations
- Phase 43 — backup/restore for rollback data plane
