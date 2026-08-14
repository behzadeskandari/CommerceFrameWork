# PHASE 37 — Audit / Security / Compliance — Pre-implementation

**Status:** Pre-implementation  
**Date:** 2026-08-13

---

## Scope

Dedicated tamper-resistant audit subsystem with security hardening: rate limiting, security headers, secret masking, authorization enforcement, and retention policy.

### Audit categories

| Category | Events |
|---|---|
| Security | Login success/failure, logout |
| Admin | Mutating `/api/admin` HTTP requests |
| Authorization | Permission access denied |
| Order | Admin order cancellation |
| Payment | Capture, void, refund |
| Customer | Profile update, deactivation |
| Settings | Setting value changes (masked) |
| Plugin | Install, enable, disable, uninstall, settings |

### Tamper resistance

- Append-only `AuditEntry` table
- SHA-256 hash chain: each entry stores `PreviousEntryHash` + `EntryHash`
- `VerifyChainAsync` recomputes hashes and detects modification
- No update/delete API on audit entries (retention job only)

### Sensitive data protection

- `AuditSanitizer` masks keys containing password, secret, token, apikey, cvv, cardnumber, etc.
- Passwords and payment secrets are never logged
- Bearer tokens and connection strings redacted in free text

### Security controls

| Control | Implementation |
|---|---|
| Rate limiting | Global 300 req/min per IP on `/api/admin`; 20 req/min on auth endpoints |
| Security headers | CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy |
| Authorization audit | `AuditingPermissionAuthorizationHandler` logs denied access |
| Retention | Configurable `Audit:Retention:RetentionDays` (default 365) |

### Permissions

- `Audit.View` — list entries
- `Audit.VerifyChain` — verify hash chain
- `Audit.ManageRetention` — apply retention policy
- `Audit.Export` — reserved for future export

### API

- `GET /api/admin/audit`
- `GET /api/admin/audit/verify-chain`
- `POST /api/admin/audit/retention/apply`

---

## Module layout

```
Commerce.Framework.Contracts.Audit   → IAuditPublisher, AuditActions
Commerce.Audit.Domain                → AuditEntry entity
Commerce.Audit.Contracts             → IAuditQueryService, DTOs
Commerce.Audit.Application           → AuditWriter, AuditSanitizer, AuditQueryService
Commerce.Audit.Infrastructure        → EF repo, middleware, permissions
Commerce.Modules.Audit               → module registration
```

Cross-module hooks publish via `IAuditPublisher` (default `NullAuditPublisher` when Audit module disabled).

---

## Out of scope (Phase 37)

- External SIEM integration
- Admin UI for audit log browsing
- WORM storage / HSM signing
- GDPR data subject export automation
