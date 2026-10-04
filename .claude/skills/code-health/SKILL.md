---
name: code-health
description: Periodic whole-codebase (or one-area) quality audit against docs/CODE-STANDARDS.md. Produces a prioritised report and refactor candidates; changes no code. Use every ~5 features, before milestones, or when asked for a code review of the codebase.
model: opus
argument-hint: "[area or folder; empty = whole codebase]"
disable-model-invocation: true
---
Audit the codebase against `docs/CODE-STANDARDS.md` (plus `CLAUDE.md` and `.claude/rules/`). Scope: $ARGUMENTS (if empty, the whole of `Assets/Scripts` and `Assets/Tests`).

**Don't change any code in this skill.** The output is a report.

0. **Lane.** Follow `docs/parallel-lanes.md` → *Starting a task* (sync with the other lanes; check this is the right folder).
1. **Split the work.** Divide the scope into areas: simulation core, saving, UI, editor tools, tests. Give each area to its own subagent, in parallel, so the file reads stay out of this conversation. Tell each subagent to:
   - read `docs/CODE-STANDARDS.md` and the matching `.claude/rules/` file
   - use the LSP tool (find references, call hierarchy) to confirm duplication, dead code and feature envy, rather than guessing from text search; if it isn't available, say so in the findings and fall back to Grep
   - return findings only, each as: severity (🔴/🟠/🟡) · `file:line` · the standard it breaks (section number) · what's wrong in one sentence · the smallest fix · effort (S/M/L)
   - cap itself at its 15 most important findings, and skip anything the standards' "Don't over-engineer" section rules out
2. **Merge.** Remove duplicates across areas. Drop anything in an area `docs/BACKLOG.md` says is about to be redesigned (note it as "skipped: redesign planned"). Re-check every 🔴 yourself by opening the code.
3. **Write the report** to `docs/code-health/YYYY-MM-DD.md`:
   - **Summary:** 3–5 sentences on overall health and the biggest themes (e.g. "UI formatting is duplicated in 6 views").
   - **Scorecard:** one row per area: 🔴 / 🟠 / 🟡 counts and a one-word trend compared with the previous report, if there is one.
   - **Fix now (🔴):** all of them.
   - **Top refactors:** the 5 best 🟠 items by value for effort, each with why it matters for upcoming work.
   - **Everything else:** a compact table.
   - **Good patterns to keep:** 2–3 things done well, so they get copied.
4. **Propose next steps.** Suggest how to group the 🔴 and top 🟠 items into small refactor tasks (one area, one purpose, no behaviour change each), and propose adding them to `docs/BACKLOG.md` under Next. Wait for my OK before editing the backlog.
5. **Teach.** For the top 3 items, explain in plain language what the problem is, why it matters, and what the fix looks like. Name the code smell or principle, so I learn the vocabulary.
6. Commit the report (only the report) when I say so.
