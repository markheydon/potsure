# Local-only mode

Local-only mode runs Potsure as a **single-user** instance without ASP.NET Core Identity accounts. Financial data is scoped to a fixed owner key (`local`, see `LocalOnlyOwnerAccessor.LocalOwnerKey`) enforced by EF Core global query filters.

## Configuration

```json
{
  "Deployment": {
    "Mode": "LocalOnly",
    "BindPublicEndpoints": false
  }
}
```

## Behaviour

- No passkey or sign-in pages are required to reach plan, import, and report pages.
- `ICurrentOwnerAccessor` resolves to `LocalOnlyOwnerAccessor` with a stable profile key.
- Financial API and page protection for hosted mode does not apply; physical access to the machine remains the primary control.
- Data Protection keys persist to the same SQL database as the application.

## Network exposure (FR-M024)

With `BindPublicEndpoints` set to `false`, `Program.cs` clears default URLs and binds Kestrel to `http://127.0.0.1:0` (ephemeral port on loopback). This prevents accidental exposure on LAN interfaces by default.

`BindPublicEndpoints` must remain `false` for local-only mode. The application refuses to start if both local-only mode and public binding are configured, because that would expose unauthenticated financial data on the network.

## When to use hosted mode instead

Choose hosted mode when multiple users need isolated plans, passkey authentication, and HTTPS ingress managed by your platform (see `docs/self-host.md` and `docs/encryption.md`).
