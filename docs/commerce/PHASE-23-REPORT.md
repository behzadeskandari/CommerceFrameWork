# PHASE 23 — Theme Engine + Storefront Layout — Report

**Status:** Complete  
**Date:** 2026-08-12

---

## 1. Summary

Phase 23 delivers a production-grade theme architecture: server-driven theme manifests, per-store theme assignment and branding settings, configurable page layouts with CMS widget zone integration, dynamic RTL/LTR, and admin theme management — without modifying Commerce core for each store theme.

---

## 2. Theme Model

| Concept | Implementation |
|---|---|
| Identity | `ThemeManifest.SystemName` (e.g. `Themes.Default`) |
| Metadata | Name, version, author, description in manifest |
| Settings | Typed definitions (color, size, font, text) with sanitized overrides |
| Store assignment | `StoreThemeConfiguration` entity (unique per store) |
| Assets | Whitelisted CSS paths in manifest (`/themes/default/theme.css`) |
| Layout metadata | Per `ThemeLayoutType` zone lists + sidebar flag |

---

## 3. Framework & Module

**`Commerce.Framework.Themes`**
- `IThemeProvider`, `IThemeRegistry`, `IThemeContext`
- `ThemeValueSanitizer`, `ThemeCssVariableMapper`
- Layout types: Homepage, Product, Category, Search, Cart, Checkout, Account, CmsPage

**`Commerce.Themes.*` module**
- Domain: `StoreThemeConfiguration`
- Admin + storefront services
- Permissions: `Themes.View`, `Themes.Manage`

**`Commerce.Plugin.Theme.Default`**
- Default theme provider with layouts and branding settings

---

## 4. Widget Zones

Extended CMS zones: `product-before`, `product-after`, `category-before`, `category-after`.

Frontend:
- `cmr-widget-zone` component loads server-rendered CMS widgets
- `cmr-storefront-content-shell` places zones by layout type
- Header/footer zones in storefront layout shell

---

## 5. Storefront

- `ThemeRuntimeService` loads `/api/themes/runtime`, applies CSS variables + `dir`
- Route `data.themeLayout` drives layout selection
- RTL from store context; Persian UI preserved via dynamic direction
- Static theme CSS served from host `wwwroot/themes/default/theme.css`

---

## 6. Admin

- `/themes` — theme list
- `/themes/:systemName` — store assignment + branding settings + layout preview list

---

## 7. Security

- Theme settings sanitized (no scripts, no javascript: URLs)
- Themes are manifest + static assets only — no arbitrary server-side theme code
- Widget HTML remains server-sanitized (Phase 22)

---

## 8. Tests

### Unit (`Commerce.Tests.Unit/Themes/`)
- Setting sanitizer (color, text XSS rejection)
- CSS variable mapping
- Theme registry discovery
- Layout fallback for product pages

### Build

| Target | Result |
|---|---|
| `npm test` / `npm run build` | Run locally |
| `dotnet build/test` | Requires .NET 10 SDK |

---

## 9. Known Limitations

1. Single bundled default theme provider (additional themes via new `IThemeProvider` plugins)
2. Layout override JSON editable via API only (no admin JSON editor UI)
3. Theme preview is metadata/settings review, not live iframe preview

---

## 10. Key Files

```
docs/commerce/PHASE-23-PREIMPLEMENTATION.md
src/Commerce/Framework/Themes/Commerce.Framework.Themes/
src/Commerce/Modules/Themes/
src/Commerce/Plugins/Theme/Commerce.Plugin.Theme.Default/
src/Commerce/Host/Commerce.Host/Theme/ThemeControllers.cs
frontend/commerce-ui/libs/theme/
frontend/commerce-ui/apps/admin/src/app/pages/themes/
tests/Commerce/Commerce.Tests.Unit/Themes/ThemeTests.cs
```

---

**Phase 23 complete. Stopped — awaiting explicit approval before Phase 24.**
