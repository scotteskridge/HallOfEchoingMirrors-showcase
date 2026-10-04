---
name: balance
description: Run a balance pass with the probe, the what-if scenarios and the snapshot (docs/balance-tools.md): measure how many runs a fresh save needs, find where it stalls, propose a change table, apply it on OK, re-measure. Use for "balance", "tune", "too hard/easy", "how many runs", or a playtest that says pacing is off.
model: opus
argument-hint: "[what feels off, or a target, e.g. 'Act I exit in 7–12 runs']"
disable-model-invocation: true
---
Balance pass: $ARGUMENTS

Numbers change only in the assets (the Balance Sheet), never in code. A change of **rule** (what a task needs, what a switch does) is a design question: ask, or `/design`. Read `docs/balance-tools.md` first.

0. **Lane.** Follow `docs/parallel-lanes.md` → *Starting a task*. This needs the Features lane's Unity editor.
1. **Target.** Get the target from the user or the design (GDD §12a, the decisions log, `PROJECT_NOTES.md`): how many runs, which way of playing it's for (tidy, loose), and what should feel grindy or quick. If the documents disagree, say so and ask which applies.
2. **Map the route.** Read the act's route (`Assets/Tests/EditMode/Balance/<Act>Route.cs`) and the content it plays. If the content changed since the route was written (new gates, tasks, rooms), update the route first and say what you changed. The route reads gates from the assets; keep it that way.
3. **Measure.** Set `docs/balance/scenarios.txt` to just `shipped`, run the probe (*Test Runner → EditMode → category Balance → Run Selected*; through MCP: `run_tests` with `category_names: ["Balance"]`). Read `docs/balance/latest/summary.md`, then `probe_runs.csv` for the stalls: where each run died, doing what, at which second.
4. **Show the user** a short run-by-run table for the tidy and loose ways of playing, and where it stalls. Say what is the game and what is the bot (a bot doing something a human wouldn't, like retrying a task that kills it, is a bot fix, not balance).
5. **Try changes in memory.** Write scenarios (one change per line; `balance_snapshot.csv` lists every field path), run the probe again, compare. Change as few values as you can; prefer the lever that matches the user's intent (a grind comes from time against the drain and mastery, not from hard gates).
6. **Propose a table and stop:** field · asset · old → new · why · effect (runs, tidy and loose). Wait for the user's OK. Never settle a design question here.
7. **Apply on OK** with `SerializedObject` + `ApplyModifiedPropertiesWithoutUndo` + `AssetDatabase.SaveAssets()` (an editor setup step for many rows). Check `git diff` shows only those values (Unity may also write default fields or re-wrap text: say so).
8. **Check.** Full EditMode run; tell the user before touching any test that pins an old number. Re-run the probe on the shipped numbers and confirm it matches step 5's measurement. Commit `docs/balance/latest/` with the pass (`/wrap-up`), so the next pass can compare.
9. **Placeholders.** Numbers the user calls provisional go in `PROJECT_NOTES.md` (placeholder list). Give the user exact steps to play a fresh save and what run numbers to note.

**Traps (all seen 2026-10-02):**
- Edits made with Undo recording can be reverted in memory by a test run (it uses Undo), leaving the asset dirty with the old numbers. Always edit balance assets without Undo, and re-check a value after a test run.
- Explicit tests start only through a category filter (`category_names`), not by name.
- The MCP test tracker sometimes drops a long full run (reports "failed" with no failures). Run the suite through `TestRunnerApi` from `execute_code` with a callback that writes the result to a file, then read the file.
- A test run can unload the open scene for a moment; never save a scene mid-run. Tell the user before running tests while they might be playing.
- In Bash heredocs, `\n` inside a Python string becomes a real newline: use Write/Edit for code.
