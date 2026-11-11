# Potsure.Web

Blazor Server host for Potsure. Folder layout follows [ADR-0002](../../adr/adr-0002-feature-folder-source-layout.md).

| You are adding… | Location |
|-----------------|----------|
| A product page (`@page`) | `Features/{FeatureName}/Pages/` |
| UI used only within one feature | `Features/{FeatureName}/Components/` |
| Shell layout, nav, home/error routes | `Components/Layout/`, `Components/Pages/` |
| Cross-feature chrome (headers, loading) | `Components/Shared/` |
| Identity / Account template | `Components/Account/` |
| Host config, reauth, OTel, security helpers | `Configuration/`, `Services/`, `Security/`, `Telemetry/`, `Tenancy/` |
| Privacy export/delete bridge types | `Features/Privacy/` (e.g. reauthentication adapter for export) |

Razor `@using` directives: project root **`_Imports.razor`**.

Styling: Sunset theme in `wwwroot/styles/themes/default.css` and **`bb:`** layout utilities (no app Tailwind build). Global wiring: `App.razor`, `Program.cs`, `Components/Layout/`.
