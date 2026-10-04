# Working with Claude Code on the Hall of Echoing Mirrors

*A guideline for keeping AI-written code maintainable as the project grows. Researched 27 Sept 2026.*

This is the **human** half of the kit. `CLAUDE.md` is the half Claude Code reads every session; this file explains why those rules exist and how to run the workflow around them. You don't need to re-read it every day — sections 3 and 5 are the ones to keep handy.

---

## 1. The two worries, and what the research says

### "The code is only as good as the prompt"

True, but incomplete. What Claude Code produces depends on three things, and the prompt is only one:

1. **The prompt** — what you ask for in the moment.
2. **The standing context** — what Claude knows about the project before you ask anything (`CLAUDE.md`, the docs it points to).
3. **The feedback loop** — whether Claude can *check* its own work (tests, compile errors) and fix it before handing it back.

Anthropic's own guidance puts it bluntly: the setup around the model — which they call the harness — "determines how Claude Code performs more than the model alone." And the single highest-leverage item is the third one: *"Give Claude a check it can run… Without a check it can run, 'looks done' is the only signal available, and you become the verification loop."*

So the fix for "only as good as the prompt" is not writing heroic prompts. It is making sure a mediocre prompt still lands in a project that tells Claude the rules and a test suite that tells it when it's wrong.

### "The codebase will outgrow the context window"

Also true, and the problem starts earlier than people expect. Two separate effects:

- **Claude never holds the whole codebase anyway.** It navigates like an engineer would: searching, opening the files it needs, following references. That scales fine *if it knows where to look* — which is what CLAUDE.md, the rules files and your consistent folder structure are for.
- **Context rot.** Model accuracy drops as the conversation fills up, long before the window is full. Chroma's 2025 study tested 18 frontier models and every one got worse as input length grew; irrelevant-but-similar material ("distractors") made it worse still. A coding session accumulates exactly that: every file read, every failed attempt, every error log stays in the window. Anthropic's docs: *"performance degrades as it fills… Claude may start 'forgetting' earlier instructions or making more mistakes."*

The practical conclusion: **the risk isn't the codebase being too big for the window; it's individual sessions being too long and too cluttered.** That is controllable.

### What "poor code" actually looks like when AI writes it

The large studies agree on the pattern, and it is specific:

| Symptom | Evidence |
| --- | --- |
| **Duplication instead of reuse** — a new helper written because the existing one wasn't noticed | GitClear (211M lines, 2020–24): duplicated blocks up roughly eightfold; refactoring fell from ~25% of changes to under 10%. A 2026 arXiv study found AI agents produce more semantically redundant code than humans — and reviewers *didn't notice*. |
| **Errors hidden rather than fixed** | GitClear 2026: code that catches errors without handling their cause up 47%. |
| **Code churn** — written, then rewritten within two weeks | GitClear: churn rose from 5.5% to 7.9%. |
| **Plausible code that misses edge cases** | Anthropic lists "the trust-then-verify gap" as a top failure pattern. |

Every one of these is addressed by a specific rule in `CLAUDE.md`: *search before you create*, *never make a check pass by hiding the error*, *write the plan first*, *tests required*. The reviewer subagent checks for exactly this list.

**The honest caveat:** no setup makes this free. The research is also clear that the developers who do well with AI tools keep review standards high. You are learning, so you can't review like a senior engineer yet — the workflow compensates with tests, a second-opinion reviewer and plain-language explanations, but *you reading the summary and asking "why?"* is still part of the loop.

---

## 2. What the kit adds and which problem each piece solves

Installation is in `START-HERE.md` (in the zip, not the project). Once installed:

```
CLAUDE.md                         ← every session: the game in brief, hard rules, working style
.claude/rules/simulation.md       ← only when Claude opens simulation, save or test code
.claude/rules/ui.md               ← only for UI code
.claude/rules/editor-tools.md     ← only for editor scripts
.claude/rules/content.md          ← only for text, story and data files
.claude/agents/reviewer.md        ← fresh-context code reviewer
.claude/skills/plan-feature/      ← /plan-feature: interview → written plan
.claude/skills/wrap-up/           ← /wrap-up: verify → review → log → teach → commit message
.claude/skills/design/            ← /design: design talk that reads one GDD section, logs the decision
docs/plans/                       ← one plan file per feature
DesignNotes/                      ← (yours, tracked in git) GDD.md, decisions-log.md and the other design docs
```

| Problem | Countermeasure | Where |
| --- | --- | --- |
| Vague prompts | Claude interviews you and writes a plan; the plan becomes the prompt | `/plan-feature` |
| Context rot | One task per session; plan in one session, build in a fresh one; subagents for searching | CLAUDE.md, §6 |
| Rules lost in a long CLAUDE.md | Short root file; area rules load only when relevant | `.claude/rules/` |
| Duplication | "Search before you create"; the rules files name the helpers to reuse; the reviewer checks it first | CLAUDE.md, rules, reviewer |
| "Looks done" isn't done | Claude runs the tests and reads the Console itself (Unity MCP); evidence, not claims | CLAUDE.md → Checking your work |
| Claude grading its own homework | Reviewer subagent that only sees the diff and the rules | `/wrap-up` |
| CLAUDE.md growing unchecked | Claude proposes rule changes; you approve them | CLAUDE.md → Session hygiene |
| Design drift | "Never quietly settle a design question"; settle them with `/design` | CLAUDE.md → Design docs |

---

## 3. The daily loop

### Size the task first

| Size | Example | Process |
| --- | --- | --- |
| **Tiny** (one sentence) | Rename a label key; fix a typo in `game_text.txt` | Just ask. |
| **Small** (one or two files, approach obvious) | Add a tooltip to the pockets counter; a new field on `ResourceDefinition` with its item section and Balance Sheet column | Ask directly, including how to check it. `/wrap-up` at the end. |
| **Feature** (several files, or a new system) | Traversal vitality charge (GDD §12); standing orders (§13a); the run report | Full cycle below. |
| **Too big** (more than ~8 steps) | "Build realm mastery" | Ask Claude to propose a split, then plan the first piece only. |

Anthropic: *"If you could describe the diff in one sentence, skip the plan."*

### The feature cycle

**Session A: plan.** New conversation:
```
/plan-feature traversal vitality charge, GDD §12 "Traversal economy"
```
Claude reads that GDD section and the decisions log, has a subagent find the code it touches (probably `Simulation.Places.cs`, `NodeDefinition`, LoopSettings), asks you questions, and writes `docs/plans/003-traversal-charge.md`. Read it and edit it if needed; it's just a text file. This is the cheapest moment to change your mind. Commit it.

**Session B: build.** `/clear`, then:
```
implement docs/plans/003-traversal-charge.md
```
A fresh session starts with only CLAUDE.md and the plan: no leftover exploration cluttering it. It writes failing tests (using `SimulationTestBase`), then the code, then runs the tests through Unity MCP until they pass.

**Same session: finish.**
```
/wrap-up
```
It re-checks, sends the diff to the reviewer, fixes what matters, logs decisions, explains what changed, gives you Play-mode steps, and proposes a commit message.

**You:** try it in Unity. Happy? Commit. `/clear`.

### Why plan and build are separate sessions
Planning reads lots of files and tries ideas out, which is exactly the material that causes context rot. Writing the plan to a file keeps the *conclusions* and throws away the *noise*. Anthropic recommends this pattern directly: *"Once the spec is complete, start a fresh session to execute it."*

### Design work
- Design questions go to `/design`, in their own session, one topic each. It reads only the GDD section at hand, so a design debate doesn't fill a coding session.
- When Claude Code flags a conflict ("the GDD says X, the build does Y"), settle it in a `/design` session; it logs the decision and edits the GDD if you ask.
- When the GDD changes significantly, ask in a fresh session: *"`DesignNotes/GDD.md` has changed. Compare it with its previous commit (`git diff`). Which code, plans and rules-file lines are affected? Report only."*

---

## 4. Prompting: what good looks like

Four ingredients: **scope** (which file or system), **source** (GDD section, an existing class to copy, the exact error), **constraint** (what not to do), **check** (how Claude proves it worked).

| Weak | Strong |
| --- | --- |
| "Add Composure to the ring." | "Composure should reduce the ring's bleed (GDD §6, Composure job 2). Look at how `StrengthOf` feeds other effects in `Simulation.Attributes.cs` and follow that. The amount goes in LoopSettings with a Balance Sheet column. Add a test in the carrying tests: bleed at Composure 0 vs 10. Run them." |
| "The floor is broken." | "After walking out, candles left on the floor of the Dark Hall are still there next run, but the floor should clear each run. Repro in a test first (`FloorTests`), then fix the cause in `Simulation.Floor.cs`." |
| "Make the queue better." | "What would it take to add GDD §13's 'loop the last action' flag to `ActionQueue`? Don't edit anything; list the options with trade-offs." |
| "Clean up the UI code." | "Use a subagent to list places in the UI code that format numbers or colours by hand instead of using `UiText` / `UiStyle`. Report only." |

**Phrases worth keeping:**
- "Don't change anything yet; just explain / list options."
- "Use a subagent to find…"
- "Write a failing test first, then fix it."
- "Run the tests and show me the result."
- "Does this conflict with the GDD or the decisions log?"
- "Explain that like I'm new to C#."

**For bugs, give evidence, not your theory.** With MCP connected, just say "there's an error in the Console when I click Search; read it and find the cause." Without it, paste the full Console message and stack trace.

---

## 5. Context hygiene: the habits that matter most

| Habit | How |
| --- | --- |
| **One task per session** | `/clear` between tasks. The single biggest lever against context rot. |
| **Two strikes and restart** | Corrected Claude twice on the same thing and it's still wrong? `/clear`, and write a better first prompt using what you learned. Anthropic: *"A clean session with a better prompt almost always outperforms a long session with accumulated corrections."* |
| **Search in a subagent** | "Use a subagent to find where X happens." It reads the files in its own window and hands back a summary. |
| **Check the gauge** | `/context` shows what's loaded and how full the session is. Past about half and still mid-task? `/compact focus on the plan and the files changed`. |
| **Side questions off the record** | `/btw what does readonly mean?` answers without adding to the session. |
| **Stop early** | `Esc` stops Claude mid-action; `Esc Esc` or `/rewind` goes back. Redirecting in the first minute is cheaper than untangling later. |
| **Name long sessions** | `/rename floor-rework`; later `claude --resume`. |

---

## 6. Reviewing code while you're learning

You don't need to understand every line, but you do need the *shape*. After each wrap-up:

1. **Can I say in one sentence what each new file or method is for?** If not: "Why does this exist? Could it be part of something that's already there?"
2. **Did new simulation rules land in the matching `Simulation.*.cs` file?**
3. **Any number that looks like a balance value in the code?** Ask where it lives in LoopSettings.
4. **Any player-facing text in code, or American spelling in `game_text.txt`?**
5. **Did tests get added, and did any get removed or changed?** Changed tests deserve a "why?".
6. **Is there a `catch`?** Ask what happens when it triggers.
7. **Does it work when I press Play?**

Keep commits small (one feature, one commit) so any mistake is easy to find and undo.

The teaching summaries are your tutorial. When a new idea appears (partial classes, events, lambdas, ScriptableObjects, interfaces), spend two minutes asking "show me the simplest possible example of that." Over a few months that adds up, and your prompts and reviews get sharper with it.

---

## 7. Keeping the setup healthy

**Every ~5 features: a maintenance session.** Fresh conversation:
> Use a subagent to review `Assets/Scripts` for: duplicated logic, files over 300 lines, game rules inside MonoBehaviours, hard-coded balance numbers, player-facing text in code, and statements in CLAUDE.md or `.claude/rules/` that no longer match the code. Report a prioritised list; don't change anything.

Then plan fixes for the top two or three like any other feature. This is the refactoring that AI-assisted projects tend to stop doing (GitClear), so it goes on the calendar.

**Growing rules.** `/wrap-up` proposes lines when you've corrected Claude on the same thing twice. Say yes only for real repeat mistakes. When a rule gets ignored, the file is probably too long: prune before adding. Use IMPORTANT on at most one or two lines.

**Every 3–6 months, or when a new model ships:** reread CLAUDE.md and the rules files, and delete what the model no longer needs. Anthropic notes that instructions written for an older model's weaknesses can hold a newer one back.

**Big file warning.** `Simulation.cs` is one class split across many files. If one partial file passes ~400 lines, or the class as a whole gets hard to navigate, ask Claude in a maintenance session whether a system should become its own class. Plan it; don't let it happen mid-feature.

---

## 8. Warning signs

| You notice | It usually means | Do this |
| --- | --- | --- |
| Claude asks something CLAUDE.md answers | Rule ambiguous or buried | Reword it; prune the file |
| A second helper that does what `UiText` / `EditorUiFactory` / `RoomFor` already does | "Search before you create" skipped | Point to the original; add it to the rules file if it keeps happening |
| A fix breaks something else | Thin test coverage there | "Write tests that pin down the current behaviour of X before changing it" |
| Claude "fixes" with a null check or try/catch | Symptom hidden, cause untouched | "Find the root cause; don't suppress the error" |
| Sessions feel slower and dumber as they go | Context rot | `/clear` and restart with a sharper prompt |
| A MonoBehaviour is getting long | Rules leaking into the UI | Ask to move the logic into the simulation, with tests |
| You can't tell what changed | Change too big | Smaller plans, smaller commits |
| Code and GDD disagree | GDD stale or code drifted | Decide with `/design`, log it, then fix the code |

---

## Sources

- [Best practices for Claude Code](https://code.claude.com/docs/en/best-practices): context as the core constraint, verification, explore → plan → code, CLAUDE.md guidance, subagents, failure patterns
- [How Claude remembers your project](https://code.claude.com/docs/en/memory): CLAUDE.md size (under 200 lines), path-scoped rules in `.claude/rules/`
- [How Claude Code works in large codebases](https://claude.com/blog/how-claude-code-works-in-large-codebases-best-practices-and-where-to-start): the harness, layered context, LSP, periodic review
- [Code intelligence plugins](https://code.claude.com/docs/en/plugins/code-intelligence.md) and [C# LSP plugin](https://claude.com/plugins/csharp-lsp)
- [MCP for Unity (CoplayDev)](https://github.com/CoplayDev/unity-mcp): Editor bridge for tests, Console and compile status
- [Context Rot (Morph summary of Chroma's 18-model study)](https://www.morphllm.com/context-rot) and [TinyFish: Context Rot](https://www.tinyfish.ai/blog/context-rot)
- [LeadDev: Code maintainability plummets in the AI coding era](https://leaddev.com/ai/code-maintainability-plummets-in-the-ai-coding-era) and [How AI generated code compounds technical debt](https://leaddev.com/technical-direction/how-ai-generated-code-accelerates-technical-debt)
- [Tembo: AI Technical Debt](https://www.tembo.io/blog/ai-technical-debt)
- [More Code, Less Reuse (arXiv 2601.21276)](https://arxiv.org/pdf/2601.21276)
- [Unity: Run tests from the command line](https://docs.unity3d.com/Manual/test-framework/run-tests-from-command-line.html)
