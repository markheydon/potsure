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

After Razor markup changes, rebuild Tailwind: `npm run build:css` (see `.agents/skills/blazor-blueprint/references/potsure-integration.md`).
