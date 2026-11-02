# Security

Potsure handles **highly sensitive personal financial data** ([GOALS.md](GOALS.md) **G6**). Whether someone runs it locally or uses a hosted instance on the internet, financial information must stay protected and access must be restricted.

## Expectations

- **Local-only mode** must not expose budget data as an unintended network service.
- **Hosted mode** must require authenticated access (including passkeys where supported), with data isolated per user.
- **Secrets** must not appear in the repository, logs, or issue trackers.

## Reporting vulnerabilities

Report security issues privately to the repository maintainer. Do not open public issues for undisclosed vulnerabilities.

Do not commit secrets, API keys, or credentials.
