# PHASE 23 — Theme Engine + Storefront Layout — Preimplementation

**Status:** Preimplementation  
**Date:** 2026-08-12

---

## 1. Inspection Summary

| Area | Current state |
|---|---|
| Storefront | Standalone Angular app; `StorefrontLayoutComponent` shell; CSS variables with fallbacks |
| `@commerce/theme` | Stub — sets `body[data-theme]`; variable maps not applied to DOM |
| Store context | `GET /api/store/context` exposes store, language, **isRtl** |
| CMS widgets | Backend zones + API; storefront **not rendering zones** before Phase 23 |
| RTL | `LocalizationService.setLocale` sets `html[dir]`; storefront not wired to store context direction |
| Plugins | Manifest-driven discovery; **no theme provider yet** |
| Store entity | No theme field — assignment via `StoreThemeConfiguration` table |

---

## 2. Theme Model

Themes are **registered server-side** via `IThemeProvider` (controlled manifests). No arbitrary server code from untrusted packages in Phase 23.

| Concept | Storage | Notes |
|---|---|---|
| Theme identity | Manifest (`SystemName`) | e.g. `Themes.Default` |
| Name, version, author, description | Manifest | Server-driven metadata |
| Theme settings | Manifest defaults + per-store JSON override | Sanitized CSS-safe values |
| Store assignment | `StoreThemeConfiguration` entity | One active theme per store |
| Asset references | Manifest (`Assets.Css[]`) | Whitelisted static paths only |
| Layout metadata | Manifest + per-store layout override JSON | Zone slots per page type |

---

## 3. Layout Types

| Layout | Route examples | Default zones |
|---|---|---|
| Homepage | `/` | header, homepage-sections, footer |
| Product | `/product/:slug` | header, product-before, main, product-after, sidebar?, footer |
| Category | `/category/:slug` | header, category-before, main, category-after, sidebar?, footer |
| Search | `/products` | header, main, sidebar?, footer |
| Cart | `/cart` | header, main, footer |
| Checkout | `/checkout` | header, main, footer |
| Account | `/account/*` | header, main, sidebar?, footer |
| CmsPage | `/pages/:slug` | header, main, sidebar?, footer |

---

## 4. Widget Zone Integration (Phase 22)

CMS zone system names (extended):

| Theme slot | CMS zone |
|---|---|
| Header | `header` |
| Main | `main-content` |
| Sidebar | `sidebar` |
| Footer | `footer` |
| HomepageSections | `homepage` |
| ProductBefore | `product-before` |
| ProductAfter | `product-after` |
| CategoryBefore | `category-before` |
| CategoryAfter | `category-after` |

Widget HTML remains **server-rendered and sanitized** (Phase 22). Themes only define **where** zones appear, not arbitrary JS.

---

## 5. Store-Specific Configuration

```
StoreThemeConfiguration
  StoreId (unique)
  ThemeSystemName
  ConfigurationJson   -- branding/settings overrides
  LayoutOverridesJson -- per-layout zone/sidebar overrides
```

Isolated per store. Fallback to `Themes.Default` when unset.

---

## 6. RTL

- Direction from store context (`isRtl`) → applied to `document.documentElement.dir`
- Persian (fa) → RTL; English → LTR
- Theme CSS variables applied on `:root`; responsive layout unchanged
- Typography: `font-family` setting sanitized; no script injection

---

## 7. Security

| Risk | Mitigation |
|---|---|
| Arbitrary server code in themes | Themes are manifest + static assets only; `IThemeProvider` in-process registration |
| JS injection via settings | `ThemeValueSanitizer` — hex colors, px/rem, safe identifiers only |
| XSS via widget HTML | Phase 22 server sanitization unchanged |
| Untrusted theme ZIPs | Out of scope — Phase 23 uses controlled providers |

---

## 8. API Plan

| Route | Purpose |
|---|---|
| `GET /api/themes/runtime` | Storefront theme + layout + CSS variables |
| `GET /api/admin/themes` | List registered themes |
| `GET /api/admin/themes/store/{storeId}` | Store assignment |
| `PUT /api/admin/themes/store/{storeId}` | Activate theme + settings |
| `GET /api/admin/themes/preview/{systemName}` | Preview metadata |

Permissions: `Themes.View`, `Themes.Manage`

---

## 9. Frontend Plan

- Extend `@commerce/theme`: `ThemeRuntimeService`, `WidgetZoneComponent`, `StorefrontContentShellComponent`
- Refactor `StorefrontLayoutComponent` to use theme runtime + widget zones
- Route `data.themeLayout` drives layout shell
- Admin: theme list, store assignment, settings form

---

## 10. Projects

```
src/Commerce/Framework/Themes/Commerce.Framework.Themes/
src/Commerce/Modules/Themes/Commerce.Themes.{Domain,Contracts,Application,Infrastructure}/
src/Commerce/Modules/Themes/Commerce.Modules.Themes/
src/Commerce/Plugins/Theme/Commerce.Plugin.Theme.Default/
```

---

**Ready for implementation.**
