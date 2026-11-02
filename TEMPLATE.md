# Template setup checklist

Complete after **Use this template** (or cloning).

## 1. Rename placeholders

Search the repository for **`Acme.Project`** and replace with your project or package name (for example `MyCompany.Widgets` or `MyApp`).

| Item | Action |
|------|--------|
| `src/Acme.Project/` | Rename folder and `.csproj` |
| `tests/Acme.Project.Tests/` | Rename folder, `.csproj`, and project reference |
| `Repo.slnx` | Update project paths |
| `RootNamespace` | Set in `.csproj` if different from assembly name |
| `README.md` | Product title, badges, links |
| `GOALS.md`, `SCOPE.md` | Real goals and boundaries |
| `.github/workflows/*.yml` | Solution name stays `Repo.slnx` unless you rename the file |

Optional: rename `Repo.slnx` to `YourProduct.slnx` and update workflow paths.

## 2. GitHub repository settings

- **About:** description, website (Pages or product URL), topics (`csharp`, `dotnet`, `open-source`, …).
- **Branch protection:** require CI on `main` if desired.
- **Secrets:** only when you add integrations (NuGet, live smoke, etc.).

## 3. NuGet and consumer docs

This template does **not** publish to NuGet by default (`IsPackable` is false).

- **HTTP API client package:** generate from [dotnet-api-client-template](https://github.com/markheydon/dotnet-api-client-template).
- **Other packable libraries:** set `IsPackable`, add `PackageId`, pack readme beside the `.csproj`, and copy the Release workflow NuGet steps from that template.

## 4. GitHub Pages (optional)

Add `docs/_config.yml` and consumer markdown under `docs/` when you need a published site. See the API client template for a working Jekyll config.

## 5. First release

1. Set `<Version>` or tag-only versioning policy in [VERSIONING.md](VERSIONING.md).
2. Merge to `main`.
3. `git tag v0.1.0 && git push origin v0.1.0` — creates a GitHub Release via [plan/RELEASE.md](plan/RELEASE.md).

## 6. Cursor

Keep `AGENTS.md` and `.cursor/rules/` aligned with how you actually work. Add project skills under `.agents/skills/` when you repeat a workflow.
