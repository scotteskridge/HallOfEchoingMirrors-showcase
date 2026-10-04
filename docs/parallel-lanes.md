# Parallel lanes: several agents at once

Up to three Claude Code sessions work at the same time, each in its own folder (a **git worktree**: a second checkout of the same repository, with its own branch) so they never edit the same files on disk. Unity locks a project folder while it's open, so each Unity lane needs its own folder anyway.

| Lane | Folder | Branch | Unity editor (MCP name) | Sessions that belong here |
|---|---|---|---|---|
| Features | `D:\3 Unity\HallOfEchoingMirrors` | `lane/features` | `HallOfEchoingMirrors` | `/implement` (non-`ui-` plans), backlog tasks, `/code-health`, `/refactor`, `/balance` |
| UI | `D:\3 Unity\HoEM-worktrees\ui` | `lane/ui` | `ui` | `/ui-work`, `/implement` (`ui-` plans) |
| Design | `D:\3 Unity\HoEM-worktrees\design` | `lane/design` | none: **don't use Unity tools** | `/design`, `/writing`, `/plan-feature`, `/workflow` |

**`backlog-tasks` is the shared branch.** Finished work from every lane collects there. No folder keeps it checked out, so any lane can move it forward without switching branches. Never commit to `main`.

**One active session per folder.** Sessions in the same folder share its files, branch and Unity editor, so their edits mix and their compiles collide. To change model (Opus for design or a hard bug, Sonnet for building), switch the model in the lane's session; UI planning belongs in the Design lane. If files change that this session didn't touch, another session is probably working here: stop and tell the user.

A SessionStart hook (`.claude/hooks/lane-context.py`) reads the table above and tells every session its lane and Unity editor, so keep the table's columns as they are. To check by hand: `git branch --show-current`. If you're on a branch not in the table (or on `backlog-tasks` itself), the old one-folder workflow applies: work on the current branch and skip these steps.

## Starting a task
1. `git status`. If there are uncommitted changes (from an earlier task, or another session working in this folder), stop and ask the user what to do with them.
2. `git merge backlog-tasks` to bring in what the other lanes have finished (usually a fast-forward, no conflicts). If it would change a scene, close the scenes first: see *Git commands that change a scene* below.
3. **Right lane?** If the session's skill belongs to another lane (table above), tell the user which folder to use; carry on only if they say so.
4. **Unity lanes:** read `mcpforunity://instances` and `set_active_instance` to this lane's editor, the one with this lane's name in the table (an editor is named after its folder; the list shows no paths), so compiles and tests never run in another lane's editor. If that editor isn't listed, ask the user to open it (setup step 3). Then `refresh_unity` so it picks up merged changes. **Design lane:** don't use Unity tools.

## Finishing a task (after the user says yes to the commit)
Building sessions get here through `/wrap-up` step 7; `/plan-feature`, `/design` and `/writing` end with it themselves, without `/wrap-up`.
1. Commit on the lane branch.
2. `git merge backlog-tasks` again: another lane may have finished meanwhile (scenes: same check as *Starting*, step 2).
   - Conflicts in C# or text: resolve them, and tell the user what you chose.
   - `CHANGELOG.md`, `BACKLOG.md`, `UI-BACKLOG.md`, `docs/plans/README.md`: keep both sides' lines.
   - `BUILD-STATE.md`: take either side, finish the merge, then re-run `/sync-state` and commit.
   - `.unity`, `.prefab`, `.asset` (scenes, prefabs, data): **stop and ask** the user; don't hand-merge them.
3. If the merge brought anything in, re-run the checks (CLAUDE.md → "Checking your work") in a Unity lane. The design lane skips this; it only changes docs.
4. `git push . HEAD:backlog-tasks`. This is a **local** move of the shared branch, not a push to GitHub. Git only allows it as a fast-forward, so finished work can't be overwritten. If it's refused as non-fast-forward, another lane got there first: repeat steps 2–4.
5. Say which commits reached `backlog-tasks`. Pushing to GitHub stays "only when asked": then `git push origin backlog-tasks` (and the lane branch if wanted).

## Git commands that change a scene
If git rewrites a `.unity` file that a Unity editor has open, that editor shows a *"modified externally"* dialog and **freezes until someone clicks it**; every Unity MCP call hangs meanwhile (seen 2026-10-01). So in a Unity lane, before any git command that may change a scene (`merge`, `stash`, `checkout`/`restore` of a scene, `reset`):
1. Check: `git diff --name-only HEAD...backlog-tasks -- "*.unity"` for a merge; for the others you know which files they touch. Nothing listed: carry on as normal.
2. Close the scenes: `execute_code` → `HallOfEchoingMirrors.EditorTools.SceneGitGuard.CloseForGit(out var m); return m;` (or *Hall of Echoing Mirrors → Tools → Close Scenes Before Git*). It refuses while a scene has unsaved changes: ask the user to save or discard them, never discard them yourself.
3. Run the git command.
4. Reopen: `SceneGitGuard.ReopenAfterGit(out var m); return m;` (or *Tools → Reopen Scenes After Git*). It picks up the new files first.

If Unity stops answering right after a git command anyway, ask the user to look at that editor for the dialog and click **Reload** (and **Don't Save** if asked to save).

**Scene changes nobody made:** since 2026-10-01 (commit 635888d), saving `SampleScene.unity` without edits should change nothing. If a save changes only `m_AnchorMin`, `m_AnchorMax`, `m_AnchoredPosition` and `m_SizeDelta` (or prefab overrides of those), don't commit it and don't discard it. Run the EditMode tests first.
- If `LayoutDriverTests` fails, an object has two layout drivers: fix the object it names, then save again.
- If the changed values are mostly 0s, Unity is writing its normal form for layout-driven values. Discarding brings back stale numbers that the next save changes again. Tell the user and commit it once, on its own.
- Anything else: tell the user and ask.

**Keep lanes apart:** don't start two tasks at once that edit the same scene, prefab or data asset. `SampleScene.unity` in particular merges badly. If both UI and features need it, do one after the other.

## One-time setup
1. Worktrees (done 2026-10-01): `git worktree add -b lane/ui "D:/3 Unity/HoEM-worktrees/ui" backlog-tasks`, and the same for `design`. `git worktree list` shows them.
2. Main folder (done 2026-10-01): `git switch -c lane/features`. While any folder has `backlog-tasks` checked out, step 4 of *Finishing* is refused.
3. UI editor: Unity Hub → *Projects* → *Add* → *Add project from disk* → go *into* `D:\3 Unity\HoEM-worktrees\ui` before pressing *Select Folder* (done 2026-10-01; the Hub entry is named `ui`). Picking the parent `HoEM-worktrees` makes Unity create an empty project there, with no MCP menu. A second launch while the first is still importing says "another Unity instance is running with this project open": quit that one and wait. Open it with 6000.2.15f1. The first open rebuilds its `Library` cache (several minutes). Then *Window → MCP for Unity*: auto-start server and auto-register **off**, URL `http://127.0.0.1:8080`, press *Connect*. Both editors share the one standalone server (`docs/unity-mcp-connection.md`).
3b. Unity rewrites `.vscode/settings.json` in a worktree (the solution is named after the folder, e.g. `ui.slnx`), so it would always show as changed. In that worktree only: `git update-index --skip-worktree .vscode/settings.json` (done for `ui` 2026-10-01). Undo with `--no-skip-worktree` if git ever refuses a merge over that file.
4. Copy `.claude/settings.local.json` (not in git) from the main folder into each worktree's `.claude/`, so the same permissions apply there.
5. Claude Code: start each session in its lane's folder (desktop app: pick the folder when starting a session).
6. Shared memories (done 2026-10-01 for `ui` and `design`): Claude Code keeps auto-memory per starting folder, so a worktree's sessions start with none. Each worktree's `memory` folder under `%USERPROFILE%\.claude\projects\` (e.g. `D--3-Unity-HoEM-worktrees-ui\memory`) is a junction to the main folder's (`D--3-Unity-HallOfEchoingMirrors\memory`), so all lanes read and write one set. For a new lane, start one session there first (it creates the project folder), then in PowerShell: `New-Item -ItemType Junction -Path <worktree's memory folder> -Target <main memory folder>`. Deleting the junction removes only the link.

## RAM (16 GB machine)
One editor uses about 3–4 GB. Two editors fit only with Chrome and other heavy apps closed; check free memory in Task Manager → *Performance* → *Memory* before opening the second one (aim for 4 GB+ free). If the PC starts to crawl, close the UI editor between UI tasks. A fallback, untested here: run a lane's tests without an open editor, Unity in batch mode (`Unity.exe -batchmode -projectPath <folder> -runTests -testPlatform EditMode -testResults <file>`), which only uses memory while the tests run.

## Removing a lane
`git worktree remove "D:/3 Unity/HoEM-worktrees/ui"` (refuses if there are uncommitted changes), then `git branch -d lane/ui` once it's merged into `backlog-tasks`.
