# Potsure BlazorBlueprint integration

Ground truth in the repo; prefer reading these files over memory.

**Baseline:** Match `dotnet new blazorblueprint -R Server -I false` for App host, static assets, CSS pipeline, layout providers, and light/dark (`BbDarkModeToggle` only — no `BbThemeSwitcher` in v1). Potsure pages sit on top with `Bb*` + Tailwind utilities only (no `.potsure-*` or custom layout CSS).

## Packages (central versions)

`Directory.Packages.props`:

- `BlazorBlueprint.Components` (styled; pulls in Primitives transitively)
- `BlazorBlueprint.Primitives` (optional explicit pin on the same v4 line as Components)
- `BlazorBlueprint.Icons.Lucide`

Do not add icon packs or chart libraries unless the user explicitly requests package changes.

## Service registration

`src/Potsure.Web/Program.cs`:

- `builder.Services.AddBlazorBlueprintComponents(configureTheme: ...)` once (Sunset radius **0.65**, `PersistToLocalStorage`, `DetectSystemPreference`).
- Blazor Web App uses Interactive Server (`AddInteractiveServerComponents` / `AddInteractiveServerRenderMode`).

## Static assets (`App.razor`)

Order (template-aligned):

1. `wwwroot/favicon.svg`
2. `wwwroot/styles/themes/default.css` (Sunset OKLCH tokens; `:root` + `.dark`)
3. `_content/BlazorBlueprint.Components/blazorblueprint.css`
4. `_content/BlazorBlueprint.Components/css/themes.css`
5. `wwwroot/css/app.css` (Tailwind v4 from `app-input.css`; rebuild after markup/CSS input changes)
6. Blocking `_content/BlazorBlueprint.Components/js/theme-init.js` in `<head>` after stylesheets

Regenerate app CSS:

```bash
cd src/Potsure.Web && npm install && npm run build:css
```

`app-input.css` contains only the BlazorBlueprint template pipeline (`@import`, `@source` for `Components/**` and `Features/**`, `@custom-variant dark`, `@layer base` border bridge, `@theme`, `.container`). No Potsure-specific class rules.

Root `<Routes @rendermode="InteractiveServer" />` matches the template interactive host.

## Imports

`src/Potsure.Web/Components/_Imports.razor` includes:

- `BlazorBlueprint.Components`
- `BlazorBlueprint.Primitives`
- `BlazorBlueprint.Primitives.Services`
- `BlazorBlueprint.Icons.Lucide.Components`

## Portal host and providers

`src/Potsure.Web/Components/Layout/MainLayout.razor`:

- `BbToastProvider`, `BbDialogProvider`, `BbPortalHost` (same as template)
- `BbDarkModeToggle` in the header (no theme switcher popover)

## Interactive pages

Feature `.razor` files live under `src/Potsure.Web/Features/{Name}/Pages/` and use `@rendermode InteractiveServer`. Identity account pages use `[ExcludeFromInteractiveRouting]` and `@layout AuthLayout`.

## Account / auth UI (login & signup blueprints)

Anonymous account pages follow [BlazorBlueprint login/signup blueprints](https://blazorblueprintui.com/blueprints#login) ([signup](https://blazorblueprintui.com/blueprints#signup)):

- **`AuthLayout`** — muted full-page panel with dot pattern; centred `max-w-sm` column (`Components/Layout/AuthLayout.razor`).
- **`AuthFormCard`** — branded card header (`text-2xl` title, optional piggy-bank icon), body slot, optional footer link (`FooterPrompt` / `FooterLinkText` / `FooterHref`).
- **Register (`signup-01`)** — card title “Create an account”, subtitle “Enter your information below…”, field helper text under email/password/confirm, full-width “Create Account”, footer “Already have an account? Log in”. No social signup row (no external providers).
- **Login (`login-01`)** — `AuthOrContinueDivider` + outline **`PasskeySubmit`** instead of social login. Do not surface a resend-confirmation link on the login card (the `/Account/ResendEmailConfirmation` route may remain for bookmarks).
- **`AuthOrContinueDivider`** — “Or continue with” separator on the login card.
- **`PasskeySubmit`** — `Outline="true"` on login; primary styling on manage “Add passkey”.

Manage-account pages use **`ManageLayout`** (sidebar nav + `max-w-2xl` cards, no brand icon). The **Profile** page (`Account/Manage`) follows [form-03 profile edit](https://blazorblueprintui.com/blueprints/forms/form-03): card header, separator, **`BbAvatar`** initials, read-only username/email with helper copy, editable phone, footer **Cancel** / **Save changes**. Prefer `AuthFormCard` with `ShowBrand="false"` for other narrow manage forms.

Do not use `PotsurePageHeader` on `AuthLayout` pages; the card title is the page heading.

## Potsure-only UI exceptions

- **`PasskeySubmit`** — native `<button name="__passkeySubmit">` + `<passkey-submit>`; styled with Tailwind utilities on the button.
- Identity **`<form method="post">`** and hidden inputs for static SSR posts.

Do **not** add a custom reconnect modal; the platform handles circuit reconnect without it.

## UI patterns

Prefer `Bb*` styled components; layout `<div>`s use Tailwind utilities like the BlazorBlueprint template.

| Instead of | Use |
|------------|-----|
| `<h1>` / `<h2>` page titles | `PotsurePageHeader` or `BbSectionHeader` / `BbTypographyH*` |
| Muted / body copy | `BbTypographyMuted`, `BbTypographyP` |
| Plain `<a href>` | `BbLink` or `BbButton` `Variant="Link"` |
| Raw `<table>` | `BbDataTable` |
| Lists of lines | `BbItemGroup` / `BbItem` or `BbEmpty` |
| QR (authenticator) | `BbQrCode` |

Shared wrappers: `src/Potsure.Web/Components/Shared/` (`PotsurePageHeader`, `PotsureLoadingState`, `PotsureRelatedNav`).

Also prefer `BbCard`, `BbButton`, `BbFormField*`, `BbAlert`, `BbFileUpload`, chart components, and Lucide icons.

## Spec alignment

`specs/001-potsure-v1/plan.md` and `research.md`: InteractiveServer, Lucide only, BlazorBlueprint charts, no duplicate chart package.
