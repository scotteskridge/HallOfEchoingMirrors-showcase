# Hall of Echoing Mirrors — rules for every session

A story-driven incremental loop game (Idle Loops / Increlution style) for PC, to sell on Steam, played actively, not idle. Set in the author's unpublished novel *Colours of Longing*: in 1928, twenty-one years after the novel, Clara (41, a practised eromancer, mother, happily married to Roland) follows her reflection into the Hall of Echoing Mirrors, where Roland is trapped saving her; she keeps going back for him. A run ends when her vitality runs out; switches, mastery and knowledge persist. **The game's plot isn't written yet** (the book summary is background only).

**Current stage: 1 of 4, Greybox Act I:** a text-and-bars Act I someone else can play start to finish; done when a friend has played it cold and the user has judged whether the loop is fun. (All four stages: `PROJECT_NOTES.md`.)

## Where things are
- `.claude/rules/`: area rules that load automatically with matching files.
- `.claude/skills/`: role sessions `/design`, `/writing`, `/ui-work`, `/workflow` (start each chat with one) and workflows `/plan-feature`, `/wrap-up`, `/code-health`, `/refactor`, `/balance`.
- `PROJECT_NOTES.md`: the user's preferences. Read it at the start of a task.
- `docs/BACKLOG.md` and `docs/UI-BACKLOG.md` (Now/Next; Later and Ideas are in the `-later` files, grep only): future work, one line each. Add ideas there, don't build them.
- `docs/CODE-STANDARDS.md`, `docs/CHANGELOG.md` (finished features), `docs/plans/` (open plans; finished ones are in `docs/plans/finished/`, grep only), `docs/mockups/` (UI mock-ups).
- `DesignNotes/`: `VISION.md` (pillars, what's tabled), `GDD.md` (the living design), `decisions-log.md` (why). If it's missing, stop on design questions; don't reconstruct the design from code.
- Conflicts: the user's latest message > `.claude/rules/` (more specific) > this file > `PROJECT_NOTES.md`. Say which you followed.

## Environment
Unity 6000.2.15f1, URP, 2D, **New Input System** (never legacy `UnityEngine.Input`), Test Framework, uGUI + TextMeshPro. Windows. The shell starts in the project root: never prefix commands with `cd`.

## Checking your work
- After every change, through Unity MCP: `refresh_unity` (compile), `read_console`, `run_tests` (EditMode, all; failed-test details only), `get_test_job`. Read failure messages and stack traces before fixing.
- If Unity or MCP isn't available, or the editor is open: see `docs/unity-mcp-connection.md`.
- Report which tests ran and their results; give exact editor steps and what should happen.
- **Never weaken, skip or delete a test, or swallow an exception, to pass.** Fix the cause or stop.

## Hard rules
**Design**
- The design is a work in progress. Say which assumptions you rely on. **Never quietly settle an open design question**: ask, or build a labelled placeholder (`Placeholder rule:` in its comment, listed in `PROJECT_NOTES.md`). The code and `docs/plans/` win over the GDD on what is built. When the user settles a point, show the GDD edit and apply it on OK; spreadsheets only when asked.
- Use the GDD's terms: vitality, pathos, hue, pool, switch, task, workings, mastery, Hub, Hall, Realm.

**Architecture**
- Game rules live in Core as plain C#; MonoBehaviours are thin adapters.
- Deterministic ticks (`TickEngine`); gameplay never reads `Time.deltaTime`. Pacing comes from durations, stats and mastery, never the tick rate.
- Content is data (ScriptableObjects in `Assets/Data/`), referenced by asset, never by name. Never name a folder `Resources`.
- Per-run state (`LoopState`) and kept state (`PersistentState`) stay apart. Pools can be added at runtime: never assume seven.
- Balance numbers live in the assets (edit in *Hall of Echoing Mirrors → Balance Sheet*), never in code; new balance fields get a column there.
- Saves store content `Id`s, never names or paths. A format change bumps `SaveData.CurrentVersion` with an upgrade step.

**Text**
- No player-facing text in code: use keys in `Assets/Text/game_text.txt`. Inspector tooltips and Console messages stay in code.
- British spelling for players; standard names in code. Room names in title case.
- Never overwrite the user's story prose (details: `.claude/rules/content.md`).

**Unity**
- Don't hand-edit `.unity`/`.prefab`/`.asset` YAML unless unavoidable (automate with one-click setup steps); never create or edit `.meta` files (move or rename assets together with their `.meta`).
- A MonoBehaviour's class must match its file name: warn before renaming. Renamed serialized fields get `[FormerlySerializedAs]`.
- Claude manages the UI layout; the user tweaks it. List every layout change in the report, and don't undo a hand tweak unless asked. New UI gets a tooltip.
- No new packages without asking.

**Code and tests**
- **Search before you create:** extend what exists and say what you found.
- Core changes come with EditMode tests; for a bug, the failing test first.
- **Stay in scope:** list clean-up ideas at the end rather than doing them.
- Comments say *why*, matching nearby density. New files hold one public type; past ~300 lines, suggest a split. Fail loudly on impossible states. No new static state. In game code, get references with `[SerializeField]` (assigned in the Inspector) or by passing them in, never `Find*`/`FindObjectOfType` (editor setup scripts may use them). Style and structure: `docs/CODE-STANDARDS.md`.
- Shipping: solid code over hacks (flag shortcuts); only commercially licensed assets (flag unclear ones; Steam needs an AI disclosure).
- **Privacy:** this is a public showcase copy; day-to-day work happens in a private repo. Before any commit or push, check the diff for keys, tokens, passwords, `.env` files, personal data and novel prose or private material outside `DesignNotes/`; if in doubt, stop and ask.

## Working with the user
A solo developer directing Claude Code to build a commercial game, and growing their own engineering skills along the way. **Explaining the work matters as much as shipping it.**
- Plain language; define terms the first time. Numbered editor steps with exact menu paths, what should happen, and common mistakes. Explain new code: what it's for, how it connects, the key idea.
- Workflow: `/plan-feature` for new systems or multi-file changes → small tested steps → `/wrap-up`.
- Ask before: settling a design question, anything that changes what the player sees or how a rule works, or breaking a rule above. Decide yourself (and say what you chose): names, code structure, test cases, which helper to use.
- If a request would break a hard rule, name the rule and offer the compliant way; do it the other way only if the user confirms, and flag it as a shortcut.
- Commit and push only when asked; messages say why. Work on your lane's branch (`docs/parallel-lanes.md`); never commit to `main`; ask before creating a branch.
- One task per session (a playtest batch counts as one), then suggest `/clear`.
- **Changes to this file or `.claude/rules/`: propose the exact lines and wait for the user's OK; never append on your own.** Budgets: this file under 1,100 words; each rules file under 200 lines.

## Compact instructions
When compacting, keep: the task, the approved plan, the files changed, the latest test results, and any open questions for the user.
