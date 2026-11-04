# Potsure BlazorBlueprint integration

Ground truth in the repo; prefer reading these files over memory.

## Packages (central versions)

`Directory.Packages.props`:

- `BlazorBlueprint.Components` (styled; pulls in Primitives transitively)
- `BlazorBlueprint.Primitives` (optional explicit pin on the same v4 line as Components)
- `BlazorBlueprint.Icons.Lucide`

Do not add icon packs or chart libraries unless the user explicitly requests package changes.

## Service registration

`src/Potsure.Web/Program.cs`:

- `builder.Services.AddBlazorBlueprintComponents(configureTheme: ...)` once (Sunset radius **0.65**, dark mode persistence).
- Blazor Web App uses Interactive Server (`AddInteractiveServerComponents` / `AddInteractiveServerRenderMode`).

## Static assets (`App.razor`)

Order matters for theming:

1. `wwwroot/styles/theme.css` (**Sunset** shadcn-compatible tokens; no `themes.css` — avoids overriding custom palette)
2. `_content/BlazorBlueprint.Components/blazorblueprint.css`
3. `wwwroot/css/app.css` (Tailwind v4 **app shell** utilities; rebuild from `app-input.css`)
4. Scoped bundle CSS
5. `_content/BlazorBlueprint.Components/js/theme-init.js` in `<head>` (blocking)

Regenerate app CSS after changing layout markup or `app-input.css`:

```bash
cd src/Potsure.Web && npm install && npx @tailwindcss/cli -i wwwroot/css/app-input.css -o wwwroot/css/app.css
```

## Imports

`src/Potsure.Web/Components/_Imports.razor` includes:

- `BlazorBlueprint.Components`
- `BlazorBlueprint.Primitives`
- `BlazorBlueprint.Primitives.Services`
- `BlazorBlueprint.Icons.Lucide.Components`

## Portal host and providers

Interactive overlays require `BbPortalHost` (with `@rendermode InteractiveServer` on mixed static/interactive apps):

- `src/Potsure.Web/Components/Layout/MainLayout.razor` — also `BbToastProvider`, `BbDialogProvider`, `BbDarkModeToggle`

## Interactive pages

Feature `.razor` files use `@rendermode InteractiveServer`. Identity account pages use `[ExcludeFromInteractiveRouting]` and `@layout AuthLayout`.

## Platform UI (not BlazorBlueprint)

- **`ReconnectModal`** — ASP.NET Core Interactive Server circuit reconnect UI; keep `id="components-reconnect-modal"`.

## UI patterns in repo

Prefer `BbCard`, `BbButton`, `BbInput`, `BbSelect`, `BbDataTable`, `BbAlert`, `BbFileUpload`, `BbDatePicker`, chart components, and Lucide icons.

## Spec alignment

`specs/001-potsure-v1/plan.md` and `research.md`: InteractiveServer, Lucide only, BlazorBlueprint charts, no duplicate chart package.
