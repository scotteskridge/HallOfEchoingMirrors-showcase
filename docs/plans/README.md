# Plans

One file per plan, numbered in one shared sequence. UI and look plans are named `ui-NNN-short-name.md` (a convention from plan 014 on); earlier UI plans keep their old names because the decisions log and other docs link to them.

| Area | Plans |
| --- | --- |
| UI and look | 002 layout shell, 003 room popovers, 004 ribbon and queue drawer, 005 drag to reorder, 006 route on the map, 007 floor on the map, 008 pockets and stats, 009 finish Part 1, 010 trait icons and tips, 011 book fonts, 012 UI colour roles, `ui-014` main screen look, `ui-015` schedule anywhere, `ui-019` queue column, `ui-024a` planning screen design, `ui-024b` repeat and plan build, `ui-024c` planning screen, `ui-024d` plan warnings, `ui-026` look pass, `ui-033` planning screen finish, `ui-034` code health and queue fixes, `ui-035` button and chip prefabs, `ui-036a` tooltip shape, `ui-051` Summary charges display; roadmaps: `backlog-roadmap.md` (clearing the game-rules backlog), `finished/ui-rework-roadmap.md`, `ui-backlog-roadmap.md` (clearing the UI backlog in batches) |
| Game rules and other | 001 longest run, 013 candles at maximum, 015 blurb triggers, 016–017 run report, 018 blurb verb kinds, 020 explore once, 021 room-entry benchmarks, 022 base speed (rolled back) and 022b halve game time, 023 rooms known by heart, 025 room speed, 027 lab redesign (content spec; built by 027a phase 1, 027b earrings, 027c gem and exit, 027d jump-ahead dev tool), 028 refactor follow-ups, 029 Feed unlocks room speed, 030a cost curve and 030b escalating charge, 031 stats pass, 041 stats rework, 042 since last run (Core), 032a planning and by heart, 032b saved report and dev runs, 040 design docs restructure, 040b slim the GDD, 055 Act I playtest batch |

**Finished plans** live in `finished/` (Done, rolled back or superseded, with nothing left to do). The plans in this folder are the ones still open: drafts, in progress, or built but waiting on a check. The table above lists every plan. Each open plan has a **Left to do:** line under its status.

## Open plans

| Plan | Left to do |
| --- | --- |
| ui-054 Mirror shapes | Approved 2026-10-02; to build in the UI lane (`/implement docs/plans/ui-054-mirror-shapes.md`) |
| 012 UI colour roles | Play check at 1920×1080 (the user) |
| 052 Simulation extraction | Approved 2026-10-01; Act I playtest done 2026-10-02 (plan 055), so it can go ahead; four `/refactor` sessions in the Features lane, one per extraction; **#1 `RunReportBuilder` and #2 `RoomSpeedHold` done 2026-10-01, #3 `PlanWarningsTracker` done 2026-10-02**, next `Stats` (judge first whether it's worth it) |
| Backlog roadmap | Plan each batch (043 to 051) in its own session; 042 is built; ui-043 can start now (049 is done) (`backlog-roadmap.md`) |
| UI backlog roadmap | Plan each later batch (ui-034 to ui-039) in its own session; colour (Next 6) is parked to stage 2 |

`/wrap-up` keeps this list and the **Left to do:** lines current: a finished plan leaves the list, a new plan joins it.

Backlogs: `docs/BACKLOG.md` (game rules and other work), `docs/UI-BACKLOG.md` (UI and look). Template: `_TEMPLATE.md`. The next plan is 056 (034–039 are reserved by the UI backlog roadmap, 042–051 by the backlog roadmap).
