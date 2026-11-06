# Potsure BlazorBlueprint integration

Ground truth in the repo; prefer reading these files over memory.

**Baseline:** BlazorBlueprint **Scenario B** (no app Tailwind build): Sunset theme CSS, package `blazorblueprint.css`, and `theme-init.js`. Do **not** load `themes.css` (that file is for `BbThemeSwitcher` base/primary presets and overrides `default.css`). App shell layout uses **`Bb*`** plus **`bb:`** utilities from `blazorblueprint.css`.

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

Order:

1. `wwwroot/favicon.svg`
2. `wwwroot/styles/themes/default.css` (official [Sunset preset](https://blazorblueprintui.com/docs/theming) OKLCH tokens on `:root` + `.dark`, including `--sidebar` and alert variables)
3. `_content/BlazorBlueprint.Components/blazorblueprint.css`
4. Blocking `_content/BlazorBlueprint.Components/js/theme-init.js` in `<head>` after the stylesheets

Do **not** link `_content/BlazorBlueprint.Components/css/themes.css` unless you add `BbThemeSwitcher`. `ThemeService` still sets `data-base-color` (default zinc) for dark mode; with `themes.css` present that attribute replaces Sunset `--primary` with a grey scale primary (black-looking buttons).

Root `<Routes @rendermode="InteractiveServer" />` matches the template interactive host.

## Layout styling (no consumer Tailwind)

- Use **`Bb*`** for UI; use **`bb:`** utilities in `class` / component `Class` for layout (for example `bb:flex bb:gap-4`, `bb:mb-4`).
- Do **not** add a Tailwind build or `@source` the BlazorBlueprint package.
- **`PasskeySubmit`** uses native `<button>` with `bb:` utility strings (Identity passkey interop).

## Imports

Project-wide Razor usings: `src/Potsure.Web/_Imports.razor` (includes BlazorBlueprint and feature page namespaces). `Components/_Imports.razor` defers to that file.

`src/Potsure.Web/_Imports.razor` includes:

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

- **`AuthLayout`** — centred `max-w-sm` column on the same background as the rest of the app (`Components/Layout/AuthLayout.razor`).
- **`AuthFormCard`** — branded card header (`text-2xl` title, optional piggy-bank icon), body slot, optional footer link (`FooterPrompt` / `FooterLinkText` / `FooterHref`).
- **Register (`signup-01`)** — card title “Create an account”, subtitle “Enter your information below…”, field helper text under email/password/confirm, full-width “Create Account”, footer “Already have an account? Log in”. No social signup row (no external providers).
- **Login (`login-01`)** — `AuthOrContinueDivider` + outline **`PasskeySubmit`** instead of social login. Do not surface a resend-confirmation link on the login card (the `/Account/ResendEmailConfirmation` route may remain for bookmarks).
- **`AuthOrContinueDivider`** — “Or continue with” separator on the login card.
- **`PasskeySubmit`** — `Outline="true"` on login; primary styling on manage “Add passkey”.

Manage-account pages use **`ManageLayout`** (sidebar nav + `max-w-2xl` cards, no brand icon). The **Profile** page (`Account/Manage`) follows [form-03 profile edit](https://blazorblueprintui.com/blueprints/forms/form-03): card header, separator, **`BbAvatar`** initials, read-only username/email with helper copy, editable phone, footer **Cancel** / **Save changes**. Prefer `AuthFormCard` with `ShowBrand="false"` for other narrow manage forms.

Do not use `PotsurePageHeader` on `AuthLayout` pages; the card title is the page heading.

## Potsure-only UI exceptions

- **`PasskeySubmit`** — native `<button name="__passkeySubmit">` + `<passkey-submit>`; styled with **`bb:`** utilities on the button.
- Identity **`<form method="post">`** and hidden inputs for static SSR posts.

Do **not** add a custom reconnect modal; the platform handles circuit reconnect without it.

## UI patterns

Prefer `Bb*` styled components; layout `<div>`s use **`bb:`** utilities from `blazorblueprint.css`.

| Instead of | Use |
|------------|-----|
| Raw enum / Guid in closed `BbSelect` | `BbSelectItem` `Text="..."` **and** `BbSelectValue` child content with the formatted label (`ChildContent Context="ignored"` inside `EditForm` to avoid Razor `context` clash). Closed trigger does not always resolve `Text` from items alone. |
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
