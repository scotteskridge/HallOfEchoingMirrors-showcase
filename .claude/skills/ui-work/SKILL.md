---
name: ui-work
description: "UI session: build or adjust player-facing screens, layout, tooltips, fonts and colours. Use for UI and visual work; not for game rules or story text."
model: sonnet
effort: medium
disable-model-invocation: true
---
A UI session: $ARGUMENTS

0. **Lane.** Follow `docs/parallel-lanes.md` → *Starting a task* (sync with the other lanes; check this is the right folder).
1. **Start from the picture.** Read `docs/mockups/README.md` and the matching item in `docs/UI-BACKLOG.md`, then the notes file of only the folder that matches the task. A mock-up is a proposal, not a decision.
2. **Read only the UI files at hand** (`Assets/Scripts/UI/`, `Assets/Prefabs/UI/`, `Assets/Data/UI/`). Grep for the helper before writing one; `.claude/rules/ui.md` lists them and loads on its own.
3. **Don't read** the GDD, `future-sketches.md`, `Writing Guides/` or Core simulation code unless the task needs a fact from them; then grep one § or use a subagent. UI shows the simulation and never holds rules.
3b. **Token budget:** stay inside the UI folders and `docs/mockups/`. No repo-wide globs or greps, no Explore agents. Skip `PROJECT_NOTES.md` unless the task needs a preference from it. If you need a file outside scope, name it and ask first.
4. **Check in Unity:** for layout, colour or text-only changes, refresh and check the console, then look at the screen. Run EditMode tests only when a C# file changed. Give numbered editor steps and list every layout change.
5. **Player-facing wording:** use your best judgment on UI wording, including flavour text, as `game_text.txt` keys. The user will sometimes run a `/writing` session to check it against the voice guide, so keep a list of the keys you added or changed.
