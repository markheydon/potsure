# Pull request policy

Canonical taxonomy: [`solo-dev-board/plan/LABEL_STRATEGY.md`](https://github.com/markheydon/solo-dev-board/blob/main/plan/LABEL_STRATEGY.md). This repo uses the same `type/*`, `status/*`, and `priority/*` labels documented in [LABEL_STRATEGY.md](LABEL_STRATEGY.md).

---

## Title

```text
[<Type>] <Imperative summary> (#<issue>)
```

- `<Type>`: `Story`, `Chore`, `Bug`, `Test`, `Documentation`, `Enabler`, or `Feature`.
- UK English, imperative verb (`Add`, `Fix`, `Document`).
- Append `(#N)` when a tracking issue exists.

Examples:

```text
[Chore] Scaffold DnsCheck.Client repository (#1)
[Story] Implement DNS Check v1 monitoring API (#2)
```

Do not use Conventional Commits prefixes (`feat:`, `fix:`) or raw label names in titles.

---

## Body

Complete [`.github/pull_request_template.md`](../.github/PULL_REQUEST_TEMPLATE.md). Use `Closes #N` when merging should close the issue.

---

## Labels (pull requests)

| Group | Required | Rule |
|-------|----------|------|
| `type/*` | Yes | Match title / linked issue |
| `priority/*` | Yes | Copy from issue; default `priority/medium` |
| `status/*` | Yes | `status/in-review` while open |

`size/*` is optional.

Dependabot PRs: `type/chore`, `priority/medium`, `status/todo` (see [dependabot.yml](../.github/dependabot.yml)).

---

## Metadata

| Field | Policy |
|-------|--------|
| Base branch | `main` |
| Draft | Ready for review when CI gates pass |
| Assignee | `markheydon` |
| Secrets | Never commit DNS Check API keys |

---

## Agent checklist

1. Branch is not `main`.
2. Title matches policy.
3. Template completed.
4. Labels applied (`type/*`, `priority/*`, `status/in-review`).
5. `dotnet format` / build / test pass for `Repo.slnx` (Release).
