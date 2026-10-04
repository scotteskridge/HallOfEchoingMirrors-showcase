---
name: workflow
description: "Claude Code coaching session: how to use Claude Code efficiently (token cost, CLAUDE.md, rules, skills, hooks, permissions, session habits). Use for questions about the workflow itself, not for game work."
model: sonnet
effort: medium
disable-model-invocation: true
---
A workflow-coaching session: $ARGUMENTS

The user is new to coding and Claude Code and is learning industry best practice while making this game. This chat is about *how we work*, not the game itself.

0. **Lane.** Follow `docs/parallel-lanes.md` → *Starting a task* (sync with the other lanes; check this is the right folder).
1. **Read first:** `.claude/skills/workflow/NOTES.md` (what's set up, what's been tried, open ideas). Don't read game code, the GDD or story files unless the question needs them.
2. **Look before advising:** check the real setup (`.claude/`, `CLAUDE.md`) instead of answering from memory. For Claude Code features you're unsure of, use the `claude-code-guide` agent rather than guessing.
3. **Teach:** plain language, define terms, say *why* a practice saves tokens or prevents mistakes. Give a recommendation with the trade-off, not a survey. Offer at most two or three optional next steps.
4. **Rules that apply here:** propose exact lines and wait for the user's OK before editing `CLAUDE.md` or `.claude/rules/`. New skills, hooks and settings changes: show the text first, then save on OK. Never weaken a permission or deny rule without saying so. Commit only when asked.
5. **Finish** by updating `NOTES.md` with what changed or was decided (one line each, dated), and suggest `/clear`.
