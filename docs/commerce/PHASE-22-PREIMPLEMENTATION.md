# PHASE 22 — CMS / Topics / Pages / Widgets — Pre-Implementation

**Date:** 2026-08-12

---

## 1. Objective

Build a CMS subsystem for pages, topics, widgets, menus, SEO metadata, localization, and multi-store content — integrated with the existing Angular storefront and admin architecture.

**Out of scope:** Arbitrary JavaScript widgets from plugin ZIPs; advanced visual page builders.

---

## 2. Current State

- Storefront routes are static commerce routes only
- EN/FA via Store `Language` entity + UI dictionaries
- Media module ready for assets
- No CMS entities or APIs exist

---

## 3. Content Model

| Entity | Role |
|---|---|
| `ContentPage` | Store-scoped page with publish schedule |
| `ContentPageLocalization` | Per-language title, slug, body, SEO fields |
| `Topic` | Reusable content block (also serves as ContentBlock) |
| `TopicLocalization` | Per-language topic content |
| `WidgetZone` | Named placement area (header, footer, etc.) |
| `WidgetInstance` | Server-defined widget in a zone |
| `Menu` | Navigation container |
| `MenuItem` | Hierarchical nav item with localization |

Localization uses `LanguageId` FK — not hard-coded EN/FA columns.

---

## 4. Widget Architecture

Predefined widget types (enum, server-driven):

- `HtmlBlock` — sanitized HTML from config
- `TopicEmbed` — renders a topic by system name
- `MenuEmbed` — renders a menu by system name

No user-supplied script; configuration validated server-side.

---

## 5. Security

- `IContentHtmlSanitizer` strips scripts, event handlers, iframes
- Sanitize on admin save and before storefront render
- Slug validation prevents path traversal patterns

---

## 6. Storefront

- Route: `/pages/:slug` (before wildcard)
- API: `/api/cms/pages/by-slug/{slug}`, `/api/cms/menus/{systemName}`, `/api/cms/widgets/{zone}`
- Storefront layout loads header/footer menus from API

---

## 7. API

| Route | Purpose |
|---|---|
| `/api/admin/cms/pages` | Page CRUD + publish |
| `/api/admin/cms/topics` | Topic CRUD |
| `/api/admin/cms/menus` | Menu + items |
| `/api/admin/cms/widgets` | Widget instances |
| `/api/cms/*` | Storefront read APIs |

---

## 8. Permissions

`Cms.Pages.View/Manage`, `Cms.Topics.View/Manage`, `Cms.Menus.View/Manage`, `Cms.Widgets.View/Manage`
