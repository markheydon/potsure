# C# OSS project template

GitHub template for **Mark Heydon**-style open source .NET repositories: governance markdown, Cursor agent rules, CI, optional GitHub Releases on tags, and xUnit v3 tests. Works for class libraries, Blazor packages, tools, or apps — not only HTTP clients.

**Using this template:** click **Use this template** on GitHub, then follow [TEMPLATE.md](TEMPLATE.md).

**HTTP API client SDK?** Start from [dotnet-api-client-template](https://github.com/markheydon/dotnet-api-client-template) instead (NuGet, typed `HttpClient`, sample console, Pages docs).

## Quick start

```bash
dotnet build Repo.slnx -c Release -warnaserror
dotnet test Repo.slnx -c Release --no-build
```

## Repository layout

| Path | Purpose |
|------|---------|
| `GOALS.md`, `SCOPE.md`, `CONVENTIONS.md` | Product and coding context for humans and agents |
| `AGENTS.md` | Cursor agent operating rules |
| `.cursor/rules/` | File-scoped agent constraints |
| `.agents/skills/` | Project skills (ADR, docs, PR review) |
| `plan/` | Release and PR policy |
| `adr/` | Architectural decision records |

## Licence

MIT — see [LICENSE](LICENSE).
