# Contributing

Read [GOALS.md](GOALS.md), [SCOPE.md](SCOPE.md), [CONVENTIONS.md](CONVENTIONS.md), and [VERSIONING.md](VERSIONING.md).

## Development

The recommended environment is the [Dev Container](docs/dev-container.md) (Aspire, Docker, Spec Kit, Playwright browsers, and HTTPS dev certificates configured on create).

```bash
dotnet format Potsure.slnx --verify-no-changes
dotnet build Potsure.slnx -c Release -warnaserror
dotnet test Potsure.slnx -c Release --no-build
```

## Pull requests

Follow [plan/PULL_REQUEST_POLICY.md](plan/PULL_REQUEST_POLICY.md) and [plan/LABEL_STRATEGY.md](plan/LABEL_STRATEGY.md).

By participating, you agree to [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).
