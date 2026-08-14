# Phase 44 — Docker / Deployment / Production Installation (Pre-Implementation)

## Goal

Production deployment architecture with Docker, Compose, SQL Server, Redis, HTTPS reverse proxy, environment separation, and documented rollback.

## Scope

- Multi-stage Dockerfile for `Commerce.Host`
- Docker Compose: development, staging overlay, production overlay
- Caddy reverse proxy + HTTPS
- Environment-specific appsettings (Staging, Production)
- Secrets via `.env.example` only — no production secrets in git
- Startup database wait + optional migration on startup
- Persistent volumes: media, backups, SQL, Redis
- Health checks, restart policies, JSON logging
- Deployment and environment documentation
- Clean install test script

## Out of scope

- Kubernetes manifests (future)
- Frontend containerization (document static deploy)
- CI/CD pipeline wiring

## Tests

- `Commerce.Tests.Unit.Deployment` — configuration contracts
- `scripts/deploy/test-clean-install.ps1` — end-to-end Docker install

## Documentation

- `docs/commerce/DEPLOYMENT.md`
- `docs/commerce/ENVIRONMENT-CONFIGURATION.md`
