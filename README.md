# Hall of Echoing Mirrors

A story-driven incremental loop game for PC (in the line of *Idle Loops* and *Increlution*), made to be played actively, not left idle. It's in development in Unity 6, aimed at a commercial release on Steam.

In 1928, Clara, a practised eromancer (a magician who works with emotion), follows her reflection into the Hall of Echoing Mirrors, where her husband Roland is trapped. Every run ends when her vitality runs out. What she learns, unlocks and masters stays with her, so each attempt gets further.

![The planning map](docs/mockups/planning/2026-10-01-planning-map-warm-palette.png)

**Status:** stage 1 of 4, a greybox Act I that can be played from start to finish. See [`docs/BUILD-STATE.md`](docs/BUILD-STATE.md) for exactly what the build does today.

## What's worth a look

| Area | Where | What it shows |
|---|---|---|
| Simulation core | [`Assets/Scripts/Core/`](Assets/Scripts/Core) | All game rules are plain C# with no Unity dependency. A deterministic tick engine means pacing comes from data, never from frame time. Per-run state (`LoopState`) and kept state (`PersistentState`) are separate. |
| Tests | [`Assets/Tests/`](Assets/Tests) | About 900 EditMode tests over the core, plus a headless balance probe that plays whole runs to tune the numbers ([`docs/balance-tools.md`](docs/balance-tools.md)). |
| Data-driven content | [`Assets/Data/`](Assets/Data) | Rooms, tasks, items, skills and switches are ScriptableObjects. Balance numbers live in the assets and are edited through an in-editor Balance Sheet. |
| Saves | [`Assets/Scripts/Core/Saving/`](Assets/Scripts/Core/Saving) | Versioned save format with an upgrade step for each version. Saves store content IDs, never names or paths. |
| UI | [`Assets/Scripts/UI/`](Assets/Scripts/UI) | uGUI and TextMeshPro: a route-planning map, a drag-to-reorder action queue, tooltips and a run summary. All player-facing text comes from one keyed text file. |
| Design | [`DesignNotes/`](DesignNotes) | The living GDD, design pillars, balancing formulas and a dated decisions log that records why each call was made. |
| Process | [`docs/plans/`](docs/plans), [`docs/CHANGELOG.md`](docs/CHANGELOG.md), [`docs/CODE-STANDARDS.md`](docs/CODE-STANDARDS.md) | Every feature starts as a written plan with design assumptions, steps, tests and a done-when checklist. |

## How it's made

I'm the sole developer and designer. I build the game by directing [Claude Code](https://claude.com/claude-code), an AI coding agent, inside a workflow I set up for it. The project rules are in [`CLAUDE.md`](CLAUDE.md) and [`.claude/rules/`](.claude/rules). Role-based skills are in [`.claude/skills/`](.claude/skills), hooks enforce the code rules automatically, and parallel lanes in separate git worktrees are described in [`docs/parallel-lanes.md`](docs/parallel-lanes.md). The art, fonts and AI use are logged with their licences in [`ASSET-LOG.md`](ASSET-LOG.md).

This repository is a public snapshot. Day-to-day work happens in a private repo, and the story background from my unpublished novel is left out here.

## Opening the project

1. Install **Unity 6000.2.15f1** (URP, 2D) through Unity Hub.
2. Open the project folder in Unity Hub (*Add → Add project from disk*).
3. Open `Assets/Scenes/SampleScene.unity` and press Play.
4. To run the tests, use *Window → General → Test Runner → EditMode → Run All*.

## Rights

© Scott Eskridge. All rights reserved. The source is shared for portfolio viewing only. No licence is granted to copy, modify or redistribute it.
