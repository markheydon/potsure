---
name: project-documentation
description: Project-aware documentation conventions that complement the documentation-writer skill. Use when writing or updating README content, docs pages, user guides, tutorials, or technical blog posts in a repository that provides project context files such as GOALS.md, SCOPE.md, CONVENTIONS.md, or AGENTS.md.
---

# Project Documentation Companion

This skill complements the upstream `documentation-writer` skill.
Use it to add project-aware documentation judgement on top of Diátaxis guidance.

## When to use this skill

Use this skill when you need to:
- Write or update project documentation that should reflect the repository's goals, scope, and conventions.
- Decide whether content belongs in `README.md`, `docs/`, or another project-owned Markdown file.
- Keep documentation concise, grounded in the codebase, and consistent with local terminology.

## Layering

Treat the writing stack as three layers:

1. `documentation-writer` - Diátaxis framework, audience clarification, and document structure.
2. This skill - project-aware placement, terminology, and review rules.
3. The active repository context files - the source of truth for goals, scope, conventions, and local standards.

## Workflow

1. Start by applying the `documentation-writer` skill for document type, audience, goal, and scope.
2. Read any available context files before drafting:
   - `GOALS.md`
   - `SCOPE.md`
   - `CONVENTIONS.md`
   - `AGENTS.md`
3. Ground all terminology and recommendations in those files. Do not invent missing context.
4. Choose the document location using [references/document-placement.md](references/document-placement.md).
5. If a template or starter structure would help, use the relevant file in `assets/`.
6. Before finishing, run through [references/review-checklist.md](references/review-checklist.md).

## Scope boundaries

- This skill is for project documentation, not ADR authoring.
- If the user wants to create or update an ADR, use the dedicated `create-architectural-decision-record` skill instead.
- Do not duplicate ADR templates or ADR process rules here.

## Writing rules

- Keep prose concise and specific.
- Prefer task-oriented guidance over feature tours.
- Reflect the repository's actual structure and workflows rather than generic boilerplate.
- When context files and existing docs disagree, flag the mismatch rather than silently picking one.

## References

- [references/document-placement.md](references/document-placement.md) - decide where content should live.
- [references/review-checklist.md](references/review-checklist.md) - final review checks.
- [assets/readme-section-template.md](assets/readme-section-template.md) - starter for short README sections.
- [assets/how-to-template.md](assets/how-to-template.md) - starter for task-focused how-to guides.
- [assets/tutorial-template.md](assets/tutorial-template.md) - starter for learning-oriented tutorials.
- [assets/technical-blog-template.md](assets/technical-blog-template.md) - starter for technical blog posts.