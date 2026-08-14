# Developer Workflow Guide

**Purpose:** Safe patterns for extending the Commerce platform without breaking module boundaries, plugin isolation, or frontend/backend contracts.

**Dependency direction (always inward):**

```
Host → Modules → Framework
Host → Plugins (runtime + optional compile refs)
Modules: Domain ← Application ← Infrastructure
Frontend → libs/api → Backend HTTP
```

---

## 1. Adding a Module

1. **Create projects** under `src/Commerce/Modules/<Name>/`:
   - `Commerce.<Name>.Domain`
   - `Commerce.<Name>.Application`
   - `Commerce.<Name>.Infrastructure` (if persistence needed)
   - `Commerce.<Name>.Contracts` (optional, for cross-module reads)

2. **Register** in `src/Commerce/Host/Commerce.Host/Program.cs`:
   ```csharp
   builder.Services.Add<Name>Module(builder.Configuration);
   ```

3. **Add DbContext configuration** if entities exist:
   - Implement `IEntityTypeConfiguration<T>` in Infrastructure
   - Add migration via EF tools targeting `CommerceDbContext`

4. **Add tests:**
   - Unit tests in `tests/Commerce/Commerce.Tests.Unit/<Name>/`
   - Architecture test if new cross-module references

5. **Add to solution:** `dotnet sln Commerce.sln add <path>`

---

## 2. Adding an Entity

1. Define entity in **Domain** project (no EF attributes in domain if avoidable).
2. Add **EF configuration** in module Infrastructure (`IEntityTypeConfiguration<T>`).
3. Register in module's `AddDbContext` or shared `CommerceDbContext` extension.
4. Create **migration:**
   ```powershell
   dotnet ef migrations add <Name>_<Description> `
     --project src/Commerce/Framework/Commerce.Framework.Data `
     --startup-project src/Commerce/Host/Commerce.Host
   ```
   (Adjust paths if module uses own DbContext — most use shared `CommerceDbContext`.)

5. Add **repository/service** in Application; register in module DI extension.

---

## 3. Adding a Migration

- Migrations live in `Commerce.Framework.Data` (system) or module Infrastructure (module-specific).
- **Plugin migrations:** implement `IPluginMigration` in plugin; discovered at install/upgrade.
- Never edit applied migrations in production — add new migration instead.
- Run locally:
  ```powershell
  dotnet ef database update --startup-project src/Commerce/Host/Commerce.Host
  ```
- Installation wizard also runs migrations via `POST /installation/migrate`.

---

## 4. Adding an API Endpoint

1. **Admin:** Add controller under `src/Commerce/Host/Commerce.Host/` or module's `Admin*Controller`.
2. **Storefront:** Use `*StorefrontController` naming convention.
3. Apply:
   - `[Authorize]` / permission attributes
   - Input validation (FluentValidation or model validation)
   - `CancellationToken` on async actions
   - Consistent route prefix (`/api/admin/...` or `/api/storefront/...`)

4. Call **application service** — no business logic in controller.
5. Return typed results; use shared `ToActionResult` helpers where present.

---

## 5. Adding a Permission

1. Define permission constant in module (e.g. `CatalogPermissions.ManageProducts`).
2. Register in module's permission provider / discovery.
3. Seed default role mappings in installation seed or module seeder.
4. Enforce on controller: `[Authorize(Policy = "...")]`.
5. **Admin frontend:** add guard/check in route or component using permission service.

---

## 6. Adding a Setting

1. Define setting class implementing `ISettings` or module pattern.
2. Register in settings service / module configuration.
3. Expose via admin settings API if user-configurable.
4. Read via `ISettingService` in application code — not hard-coded values.

---

## 7. Adding Localization

1. Add resource keys in module localization JSON or `.resx`.
2. Register locale resources in module startup.
3. **Admin/Storefront:** use Angular i18n or shared translation service if wired.
4. Store-scoped strings: use CMS/localization tables for content; code strings for UI labels.

---

## 8. Adding a Plugin

1. Create project under `src/Commerce/Plugins/` referencing `Commerce.PluginSdk`.
2. Implement:
   - `IPlugin` / manifest (`plugin.json`)
   - `IPluginStartup` for DI
   - Optional: controllers, migrations, permissions
3. **Do NOT** reference `Commerce.Host` from plugin.
4. Build outputs to `Plugins/<SystemName>/` (SDK copy target).
5. Test lifecycle: discover → install → migrate → enable → disable → uninstall.
6. Validate with `Commerce.Tests.Unit.Plugins` patterns.

**Architecture note:** Host currently compile-references some plugins for dev convenience — architecture tests flag this; prefer runtime-only loading for production plugins.

---

## 9. Adding an Angular Admin Page

1. Generate component under `frontend/commerce-ui/apps/admin/src/app/features/<area>/`.
2. Add **API client** in `frontend/commerce-ui/libs/api/` (or feature service).
3. Add **route** in `apps/admin/src/app/app.routes.ts`.
4. Add **navigation** entry in admin shell/menu config.
5. Wire permissions guard if required.
6. Match DTO property names to backend JSON (camelCase).

---

## 10. Adding a Storefront Page

1. Component under `apps/storefront/src/app/`.
2. API client in `libs/api` for storefront endpoints.
3. Route in storefront `app.routes.ts`.
4. Use shared cart/auth/store context from `libs/core`.

---

## 11. Adding a Widget

1. Define widget in CMS module (widget type + zone).
2. Admin: configure instance via `/cms/widgets`.
3. Storefront: render via theme widget zone + CMS widget renderer.
4. Do not duplicate theme layout in CMS — widgets are content slots only.

---

## 12. Adding a Theme

1. Plugin under `Commerce.Plugin.Theme.*` or theme module.
2. Register theme descriptor; admin selects via `/themes`.
3. Storefront applies theme assets and layout templates.

---

## 13. Adding Tests

| Type | Location | When |
|------|----------|------|
| Unit | `tests/Commerce/Commerce.Tests.Unit/` | Domain, application logic |
| Architecture | `tests/Commerce/Commerce.Tests.Architecture/` | Dependency rules |
| Integration | `tests/Commerce/Commerce.Tests.Integration/` | Full HTTP + DB flows |
| Frontend | `frontend/commerce-ui` `*.spec.ts` | Components, services |

Run:

```powershell
dotnet test Commerce.sln -c Release
.\scripts\test\run-verification.ps1
cd frontend/commerce-ui && npm run test:admin && npm run test:storefront
```

**Rules:**
- Use in-memory DB or test factory for integration tests when possible.
- Do not link duplicate test files into wrong projects (see Phase 49 NotificationTests fix).
- Architecture tests must pass before merging boundary changes.

---

## 14. Common Pitfalls

| Pitfall | Prevention |
|---------|------------|
| Circular DI | Prefer `IServiceScopeFactory` for event handlers probing other services |
| Controller without permission | Mirror every admin API with permission + frontend guard |
| API without frontend route | Document API-only ops or add route |
| Plugin Host reference | Keep plugins isolated; use SDK contracts only |
| Money as `double` | Use `decimal` and shared money value objects |
| Frontend totals | Never trust client-calculated order totals |

---

## 15. Pull Request Checklist

- [ ] `dotnet build Commerce.sln -c Release` passes
- [ ] Relevant tests added/updated
- [ ] Migration included if schema changed
- [ ] Permission + seed if new admin capability
- [ ] Admin route + API client if new admin feature
- [ ] No secrets in appsettings or committed `.env`
- [ ] Architecture tests pass for new cross-project references

---

## Related documentation

- [PLUGIN-ARCHITECTURE.md](./PLUGIN-ARCHITECTURE.md)
- [RUNNING-AND-USING-COMMERCE.md](./RUNNING-AND-USING-COMMERCE.md)
- [FINAL-COMPREHENSIVE-AUDIT.md](./FINAL-COMPREHENSIVE-AUDIT.md)
