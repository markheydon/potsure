# Contributing

Read [GOALS.md](GOALS.md), [SCOPE.md](SCOPE.md), [CONVENTIONS.md](CONVENTIONS.md), and [VERSIONING.md](VERSIONING.md).

## Development

```bash
dotnet format Repo.slnx --verify-no-changes
dotnet build Repo.slnx -c Release -warnaserror
dotnet test Repo.slnx -c Release --no-build
```

## Pull requests

Follow [plan/PULL_REQUEST_POLICY.md](plan/PULL_REQUEST_POLICY.md) and [plan/LABEL_STRATEGY.md](plan/LABEL_STRATEGY.md).

By participating, you agree to [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).
