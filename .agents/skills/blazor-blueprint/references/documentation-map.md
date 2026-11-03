# BlazorBlueprint upstream documentation map

Official docs base: `https://blazorblueprintui.com/llms/`

## Default fetch strategy (prefer this)

| Step | Action |
|------|--------|
| 1 | Open `index.txt` and search for the `Bb*` component name or topic |
| 2 | Fetch the listed path (`components/…`, `primitives/…`, `blueprints/…`) |
| 3 | If the task involves wiring (first use of dialog, toast, theme), also fetch `setup.txt` or `services.txt` |
| 4 | If binding or state behaves oddly, fetch `patterns.txt` |

Use WebFetch or shell `curl -sL` on the HTTPS URL. Respect doc **Library Version** in METADATA; Potsure targets v4.x per `Directory.Packages.props`.

## When to fetch which file

| Task | Files |
|------|--------|
| New package or global config | `setup.txt`, optionally `migration-v3-to-v4.txt` |
| Dialog, sheet, drawer, toast, theme service | `services.txt` + specific component doc |
| Localization strings | `localization.txt` |
| Icons (`LucideIcon`, etc.) | `icons.txt` + Potsure uses Lucide package docs in index |
| Copy-paste page from catalog | `blueprints.txt` then category under `blueprints/` |
| Unknown component API | `index.txt` → single `components/{slug}.txt` |
| Headless-only primitive | `primitives/{slug}.txt` (may need `setup.txt` for `primitives.css` if not using Components package) |

## Avoid by default

| File | Why |
|------|-----|
| `llms-full.txt` | ~2.8 MB; duplicates all topics; use only for rare cross-cutting audits |
| Bulk download of all `components/*.txt` | Fetch only slugs you will edit in this task |

## Resolving slugs

From `index.txt` section headers, e.g. `BbBarcode - components/barcode.txt` →  
`https://blazorblueprintui.com/llms/components/barcode.txt`

Multi-part components (Gantt, pivot grid, map chart) document supporting types in the same file as the parent.

## After fetching

- Use parameter names and child content structure from the doc verbatim in new Razor.
- Check METADATA **Library Version** against installed NuGet version before assuming 4.1-only APIs.
- If doc and compiler disagree, trust the compiler and `dotnet-inspect` on the referenced package version.
