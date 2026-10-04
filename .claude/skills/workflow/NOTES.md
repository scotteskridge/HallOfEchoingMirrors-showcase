# Workflow notes (for `/workflow` sessions)

Goal: keep token use low by giving each chat one role and loading only the files it needs, while any chat can still reach the full codebase when a task needs it.

## Current setup (2026-09-29)
- `CLAUDE.md` loads every session (under 1,100 words by rule; each rules file under 200 lines); `MEMORY.md` index also loads.
- Path-scoped rules in `.claude/rules/` (`ui.md`, `content.md`, `simulation.md`, `editor-tools.md`, `design-docs.md`) load only when matching files open.
- Enforcement: `.claude/settings.json` denies edits to `.meta` files, asks before editing `.unity`/`.prefab`/`.asset`/`Resources`, and a PostToolUse hook (`.claude/hooks/check-code-rules.py`) flags legacy `Input` and `Time.*` in Core.
- Role skills, one per chat type, each naming the files it reads: `/design` (Opus), `/writing` (Opus), `/ui-work` (Sonnet), `/workflow` (Sonnet, this chat).
- Workflow skills: `/plan-feature`, `/implement`, `/wrap-up`, `/code-health`, `/refactor`. Agent: `reviewer`.
- `SessionStart` hook in `.claude/settings.json` shows a "type /design, /writing or /ui-work" reminder on startup and after `/clear`.
- Deny rules block reading `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`, `Build*/`.

## Key facts learned
- There is no per-chat CLAUDE.md. Anything that must survive `/clear` lives in `CLAUDE.md`, rules, skills, memory or hooks; each chat starts by typing its role skill.
- Skills load only their one-line description until invoked; rules load on matching paths; a nested `CLAUDE.md` in a subfolder loads when a file there is read; subagents keep file reading out of the main chat.
- Deny rules are project-wide, so per-role limits go in the skill text ("don't read X").

## Open ideas (not done)
- Move folder-specific rules from `CLAUDE.md` into nested `CLAUDE.md` files or path rules to shrink the always-loaded file.
- Check whether the `SessionStart` hook message actually displays (untested as of 2026-09-29).
- Try a cheaper model on mechanical tasks; review `/explain-usage` output to see where tokens go.

## Log
- 2026-10-01: planning, design and writing sessions end themselves: `/plan-feature` step 7 (Approved status, Open plans list, commit on yes, share), and a Share step in `/design` and `/writing`. Building sessions still end with `/wrap-up`.
- 2026-10-01: `/implement` skill (Sonnet, medium effort) for building an approved plan: lane step 0, right folder for `ui-` vs other plans, Approved-status check, stop on unsettled design, tested steps, hands over to `/wrap-up`. Like `/sync-state`, it has no `disable-model-invocation`, so typing "implement docs/plans/…" starts it too (user's choice). Not listed in `CLAUDE.md` (word budget full).
- 2026-10-01: `SessionStart` hook `lane-context.py` (all sources: startup, clear, resume, compact) reads the lane table in `docs/parallel-lanes.md` and tells each session its lane and Unity editor name, so sessions without a role skill still pick the right editor. Tested by hand in all three folders; untested yet in a live session (does `additionalContext` reach Claude in the desktop app?).
- 2026-10-01: parallel lanes set up (`docs/parallel-lanes.md`): worktrees `HoEM-worktrees/ui` (`lane/ui`) and `HoEM-worktrees/design` (`lane/design`); main folder becomes `lane/features` once its open work is committed; `backlog-tasks` is the shared branch, moved forward only by fast-forward at `/wrap-up`. Role and workflow skills got a step 0 (sync, right folder); `CLAUDE.md` branch line points to the doc.
- 2026-10-01 (plan 040): design docs restructured, one question per place: `DesignNotes/VISION.md` (pillars), `GDD.md` kept current when a point is settled (shown first, applied on OK), `decisions-log.md` the why, `docs/CHANGELOG.md` finished features in player language, `docs/BUILD-STATE.md` revived with a project `/sync-state` that `/wrap-up` runs. `/design`, `/plan-feature`, `/wrap-up`, the plan template and `design-docs.md` updated; the opening brief merged into GDD §12a; log entries from 2026-09-28 and earlier archived. The claude.ai-synced `sync-state` was removed by the user, 2026-10-01. `Remove Me/` was deleted instead of denied (its files are all in git history).
- 2026-09-29 (retest from project root, all passed): `.meta` write denied (Write and Bash), `DesignNotes/Archive` denied (Read and Bash `ls`), PostToolUse hook flagged `Time.deltaTime` in a Core file (exit 2) and the temp file was removed. `design-docs.md` rule loaded only after the GDD was opened. `CLAUDE.md` is 1,091 words (9 spare). Transcripts (56 sessions): 98.6% cache hits; one session (Sep 23-27, 4,697 turns, ~500k average context, 8 compactions) used 69% of all tokens; recent sessions average 200-270k context. ~870 of 2,092 Bash calls began with `cd "D:/..."`. Baseline context 78k at start: system tools 35.7k, MCP tools 16.4k, skills 6.0k, system prompt 4.1k, MCP instructions 3.0k.
- 2026-09-29 (verification pass): deny rules and the PostToolUse hook did NOT fire in a session whose cwd was `Assets/Story` (a `.meta` write and an `Archive` read both succeeded; also silent from a project-root cwd, cause unconfirmed). The hook script itself works (exit 2 on `Time.deltaTime` in Core). `./` deny patterns anchor to the cwd; `/path` anchors to the project root. Retest from a fresh session started at the project root. Transcript tally: ~2,100 Bash calls began `cd "D:/..." &&` (a prompt driver when the cwd is a subfolder); 365 `sed -n`, 351 `grep -n`, 322 `python -` heredocs, 207 `execute_code`.
- 2026-09-29: restructured `CLAUDE.md` (moved design-doc rules to `.claude/rules/design-docs.md`), added the PostToolUse rule hook and deny/ask rules, `effort: medium` on `/ui-work`, `/wrap-up`, `/workflow`, denied `DesignNotes/Archive/**`. The `SessionStart` reminder doesn't show in the desktop app (likely CLI-only); the role list in `CLAUDE.md` covers it.
- 2026-09-29: added `/writing`, `/ui-work`, `/workflow`, the `SessionStart` hook, and the Role sessions bullet in `CLAUDE.md`; `/workflow` added to that bullet, the Skills line and the hook message.
