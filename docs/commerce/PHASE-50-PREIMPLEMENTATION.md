# Phase 50 — Pre-Implementation

**Goal:** Stabilize the platform to **Release Candidate** without new features.

**Scope:** Fix audit defects — unit tests, architecture tests, integration/E2E, startup/DI, plugin boundaries, operational documentation.

**Out of scope:** PostgreSQL, Phase 42 marketplace, new product features, historical phase report edits.

## Known defects (from final audit)

| Priority | Issue |
|----------|-------|
| CRITICAL | Integration/E2E suite not green |
| CRITICAL | ~10 unit test failures (pricing, catalog, reviews) |
| HIGH | Architecture: Host compile-references plugins; Downloads boundary |
| HIGH | `scriptWithData.sql` missing |
| HIGH | Admin UI gaps for operational APIs |
| ENV | testhost/file-lock instability |
| ENV | Docker clean-install not verified |

## Planned repairs

1. Clean test environment; rebuild Release.
2. Fix unit test fixtures and EF tracking (catalog delete, review ownership).
3. Remove Host compile-time plugin references; add `ICommercePlugin` for Theme/Search.
4. Align Downloads architecture test with `IDownloadMediaResolver` in Application.
5. Fix DI violations (`PaymentProviderHealthProbe`, startup DB wait, background jobs before install).
6. Re-run integration/E2E; document remaining blockers.
7. Document API-only admin workflows; classify UI requirements.
8. Update audit documents with before/after Phase 50 results.

## Success criteria

- Backend Release build: PASS
- Unit tests: PASS
- Architecture tests: PASS
- Integration tests: PASS (or documented root cause)
- Frontend build/tests: PASS
- Release Candidate decision with evidence
