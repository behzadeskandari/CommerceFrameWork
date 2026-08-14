# PHASE 22 — CMS / Topics / Pages / Widgets — Report

**Status:** Complete  
**Date:** 2026-08-12

---

## 1. Summary

Phase 22 delivers a CMS subsystem with localized content pages, reusable topics (content blocks), server-driven widgets, navigation menus, SEO metadata, multi-store scoping, and storefront page rendering at `/pages/{slug}`.

---

## 2. Domain Model

| Entity | Purpose |
|---|---|
| `ContentPage` + `ContentPageLocalization` | Store pages with per-language slug, body, SEO |
| `Topic` + `TopicLocalization` | Reusable content blocks |
| `WidgetZone` + `WidgetInstance` | Zone-based widget placement |
| `Menu` + `MenuItem` + `MenuItemLocalization` | Hierarchical navigation |

Localization uses `LanguageId` FK — not hard-coded EN/FA columns.

---

## 3. Widget Architecture

Predefined `WidgetType` enum (server-driven):

- `HtmlBlock` — sanitized HTML configuration
- `TopicEmbed` — renders topic by system name
- `MenuEmbed` — renders menu by system name

Seeded zones: header, main-content, sidebar, footer, homepage, product-page, category-page.

No arbitrary JavaScript from admin or plugins.

---

## 4. Security

- `ContentHtmlSanitizer` strips scripts, iframes, javascript: URLs, event handlers
- Sanitization on admin save for page/topic body and HtmlBlock widgets
- Slug validation blocks path traversal (`../`, `/`)
- Storefront renders server-sanitized HTML

---

## 5. API

| Route | Purpose |
|---|---|
| `/api/admin/cms/pages` | Page CRUD + publish/unpublish |
| `/api/admin/cms/topics` | Topic CRUD |
| `/api/admin/cms/menus` | Menu CRUD |
| `/api/admin/cms/widgets/zones|instances` | Widget management |
| `/api/cms/pages/by-slug/{slug}` | Storefront page |
| `/api/cms/menus/{systemName}` | Storefront menu |
| `/api/cms/widgets/{zone}` | Storefront widgets |

Permissions: `Cms.Pages/Topics/Menus/Widgets.View|Manage`

---

## 6. Admin UI

- `/cms/pages` — page list + create/edit form
- `/cms/topics` — topic list
- `/cms/menus` — menu list
- `/cms/widgets` — widget zones + instances
- Nav links in admin layout

---

## 7. Storefront

- Route: `/pages/:slug` — CMS page renderer with SEO title/meta
- Existing product/category routes unchanged
- Storefront header loads `main-menu` from CMS API (fallback to static nav)
- `CmsApi` for storefront content loading

---

## 8. Tests

### Unit (`Commerce.Tests.Unit/Cms/`)
- HTML sanitizer (script, iframe, event handlers)
- Slug normalization and path traversal rejection
- Page publish schedule visibility
- Topic system name normalization

### Build

| Target | Result |
|---|---|
| `npm test` / `npm run build` | Run locally |
| `dotnet build/test` | Requires .NET 10 SDK |

---

## 9. Known Limitations

1. Admin topic/menu forms are list-only (API complete; full create/edit forms deferred)
2. Widget admin UI is read-only list (API supports create/update)
3. Multi-language admin tabs — single language field on page form (domain supports multiple)

---

## 10. Key Files

```
docs/commerce/PHASE-22-PREIMPLEMENTATION.md
src/Commerce/Modules/Cms/
src/Commerce/Host/Commerce.Host/Cms/CmsControllers.cs
frontend/commerce-ui/libs/api/src/lib/cms-api.service.ts
frontend/commerce-ui/apps/storefront/src/app/pages/cms-page.page.ts
frontend/commerce-ui/apps/admin/src/app/pages/cms/
tests/Commerce/Commerce.Tests.Unit/Cms/CmsTests.cs
```

---

**Phase 22 complete. Stopped — awaiting explicit approval before Phase 23.**
