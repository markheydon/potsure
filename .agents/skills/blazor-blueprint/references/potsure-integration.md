# Potsure BlazorBlueprint integration

Ground truth in the repo; prefer reading these files over memory.

## Packages (central versions)

`Directory.Packages.props`:

- `BlazorBlueprint.Components` (styled + primitives dependency)
- `BlazorBlueprint.Primitives`
- `BlazorBlueprint.Icons.Lucide`

Do not add icon packs or chart libraries unless the user explicitly requests package changes.

## Service registration

`src/Potsure.Web/Program.cs`:

- `builder.Services.AddBlazorBlueprintComponents();` once (not both Components and Primitives registration).
- Blazor Web App uses Interactive Server (`AddInteractiveServerComponents` / `AddInteractiveServerRenderMode`).

## Static assets (`App.razor`)

Order matters for theming:

1. `wwwroot/styles/theme.css` (Potsure shell tokens)
2. `_content/BlazorBlueprint.Components/css/themes.css`
3. `_content/BlazorBlueprint.Components/blazorblueprint.css`
4. App bundle CSS
5. `_content/BlazorBlueprint.Components/js/theme-init.js` in `<head>`

`<html>` uses `data-base-color="zinc"` (palette from Blueprint themes).

## Imports

`src/Potsure.Web/Components/_Imports.razor` includes:

- `BlazorBlueprint.Components`
- `BlazorBlueprint.Primitives`
- `BlazorBlueprint.Primitives.Services`
- `BlazorBlueprint.Icons.Lucide.Components`

## Portal host

Interactive overlays require `BbPortalHost`:

- `src/Potsure.Web/Components/Layout/MainLayout.razor`
- `src/Potsure.Web/Components/Layout/AuthLayout.razor`

## Interactive pages

Blueprint-heavy `.razor` files use `@rendermode InteractiveServer` at page or component level (auth, import wizard, plan, reports, recurrence, privacy panels).

## UI patterns in repo

Inspect neighbours before inventing markup:

- Forms and alerts: `Components/Account/PasskeySignIn.razor`
- Auth shell layout: `Components/Layout/AuthLayout.razor`
- Feature UI: `Components/Import/`, `Components/Plan/`, `Components/Reports/`, etc.

Prefer composing existing `BbCard`, `BbButton`, `BbInput`, `BbAlert`, tables, and charts consistent with nearby files.

## Spec alignment

`specs/001-potsure-v1/plan.md` and `research.md` record v4 choices: InteractiveServer, Lucide only, BlazorBlueprint charts, no duplicate chart package.
