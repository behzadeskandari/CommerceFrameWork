# Docker deployment (quick reference)

Full documentation: [`docs/commerce/DEPLOYMENT.md`](../../docs/commerce/DEPLOYMENT.md)

```bash
cp .env.example .env
# Edit .env — set MSSQL_SA_PASSWORD (never commit .env)

# Development
docker compose --env-file .env up -d --build

# Staging
docker compose -f docker-compose.yml -f docker-compose.staging.yml --env-file .env up -d --build

# Production
docker compose -f docker-compose.yml -f docker-compose.production.yml --env-file .env up -d --build
```

Clean install test: [`scripts/deploy/test-clean-install.ps1`](../../scripts/deploy/test-clean-install.ps1)

Environment variables: [`docs/commerce/ENVIRONMENT-CONFIGURATION.md`](../../docs/commerce/ENVIRONMENT-CONFIGURATION.md)
