---
name: pr-address-review
description: Review all open comment threads on a pull request, fix the issues raised, reply to each thread explaining what was done, and resolve the conversation. Use when a PR has received code review with open threads that need addressing before merge.
---

# Address PR Review

## When to Use

- A PR has received a code review with open comment threads.
- Before merging - ensures no review feedback is silently ignored.

## Step 1 - Identify the PR

If a PR number was provided, use it. If not, ask which PR number (and repo if not the current one).

Confirm the PR number and repo before proceeding.

## Step 2 - Read All Open Review Threads

Fetch all open (unresolved) review comment threads on the PR.

For each thread, extract and list:

- Thread ID
- File and line number
- Reviewer comment (full text)
- Category: `fix-required`, `question`, `suggestion`, `nitpick`, or `praise`

Present this list as a table and confirm before making changes. If there are no open threads, report that and stop.

## Step 2.5 - Thread Reply Transport Rules

Before posting any reply, confirm you can post **into the existing thread itself**.

Allowed:

- Reply directly to the existing review comment/thread.
- Add a review comment attached to the same file/line thread in a pending review, then submit.

Not allowed:

- Posting a normal PR/issue comment as a substitute for a thread reply.
- Posting top-level PR comments that reference thread IDs instead of replying in-thread.

If thread-level reply tooling is unavailable:

1. Stop before resolving any threads.
2. Report the limitation clearly.
3. Provide the exact reply text per thread for manual paste.
4. Do **not** resolve threads in this fallback path.

## Step 3 - Address Each Thread

Work through threads one at a time.

### fix-required or suggestion (actionable)

1. Make the code change.
2. Post a thread reply: `Fixed. [One sentence describing what was changed and where.]`
3. Resolve the conversation.

### question

1. Post a reply answering the question.
2. If the question implies a code change, make it and note in the reply.
3. Resolve the conversation.

### nitpick

1. Apply if trivially safe; otherwise explain why not.
2. Post a reply and resolve.

### praise

1. Post: `Thanks for the kind words!`
2. Resolve the conversation.

## Step 4 - Summary

Output a summary table of all threads, actions taken, and resolution status.

State: "All [n] threads resolved. PR is ready for final review before merge."

## Rules

- Never resolve a thread without posting a reply first.
- Never silently skip a thread.
- Never use top-level PR/issue comments as a proxy for thread replies.
- Do not make unrequested changes to files not referenced in a review thread.
- One reply per thread.
- Use UK English in all replies.
