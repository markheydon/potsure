---
title: Multi-targeting .NET 8 and .NET 10
status: accepted
date: 2026-10-02
deciders: (your name)
---

# ADR-0001: Multi-targeting .NET 8 and .NET 10

## Context

Consumers run LTS (.NET 8) and current (.NET 10) runtimes. Libraries should support both without forking the codebase.

## Decision

- Target `net8.0` and `net10.0` on packable libraries and test projects.
- Pin SDK band in `global.json` with `rollForward: latestPatch`.
- Run CI on both target frameworks via the solution test projects.

## Consequences

- Slightly larger build matrix; aligns with modern .NET OSS practice in this org.
- Drop `net8.0` only in a major version when support policy changes (document in VERSIONING.md).
