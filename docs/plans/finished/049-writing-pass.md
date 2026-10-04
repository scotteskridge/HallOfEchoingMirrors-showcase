# 049 — Writing pass: the three planning placeholders

**Status:** Done (2026-10-01): wording in the Design lane, the shut message's move to a text key in the Features lane; the user's in-game check was fine.
**Design:** GDD §2 item 5 (Prose), §14.8 (prose is a feature), §14.17 (story is a parallel pleasure, not a fun source: the greybox must be fun with placeholder text, so this pass must not hold up the loop), §14.12 (the player must be able to diagnose a failed run); decisions-log 2026-09-30 "Planning is earned by Feed your hours…" (plan 032a).
**Pillar:** 6, *Prose is a feature, not the engine* (Clara's voice, first person, present tense; the loop must work with placeholder text first). Also serves pillar 3 (a failed run can be diagnosed) because two of the lines are refusal reasons (principle 17 also applies: this pass is polish, and the greybox test doesn't wait for it).

## Goal
`/writing` has everything it needs to reword three placeholder lines without losing the rule or hint each one carries. This plan only scopes the pass: **no new wording is written here**, and nothing is changed in the game until `/writing` does it.

## Out of scope
- Writing the lines (done by `/writing`, with your approval; your prose is never overwritten, `.claude/rules/content.md`).
- Plan 042 (since last run) and ui-051 (Summary charges): already planned.
- Any change to *when* the lines show, or to the planning rule itself.
- The neighbouring placeholders listed under *Next to these three*: your call, below.

## The three lines

### 1. `feed.planning_unlocked` (`Assets/Text/game_text.txt` line 95, section `## feed`)
- **Current text:** `The hours burn clear: you can plan the next run before you begin it.` (about 70 characters)
- **Where and when:** one line in the Story feed, in the milestone colour (`UiStyle.Milestone`), shown **once**, the first time *Feed your hours to the flames* is finished and gives her Quickened Hours (`unlocksPlanning`). Code: `FeedNotes.OnPlanningUnlocked`, `Assets/Scripts/UI/FeedNotes.cs:157`. It appears mid-run, in the run's flow among the other feed lines, so it reads next to whatever she is doing.
- **What it must still tell the player:** that she can now **plan the next run before it begins** (the Plan button appears on the Summary; Continue stops starting the next run straight away). Per decisions-log 2026-09-30 (the "Summary gets one line" entry) it is **also meant to say rooms she knows by heart will now run faster** (room speed is unlocked by the same item). **The current text does not say that**, so the rewrite would be the first to carry it. Whether it should is Question 1.
- **Placeholders/variables:** none.
- **Length limit:** none in code; it is one feed line (it wraps). Keep it near the current length so it doesn't dominate the feed.
- **Related text for context (not part of the pass):** the item's description on `QuickenedHours.asset` ("The candles burn to her rhythm now. Rooms she knows by heart pass…") and the task description on `FeedYourHoursToTheFlames.asset` ("Twenty-five candles, every one of them leaning towards her…") set the voice and imagery the new line sits beside.

### 2. `reasons.not_by_heart_plan` (`game_text.txt` line 444, section `## reasons`)
- **Current text:** `she doesn't know {room} by heart yet ({runs} of {needed} runs); do it during a run` (about 80 characters, about 100 with a room name)
- **Where and when:** only **between runs, once planning is unlocked**. When she tries to put an action, or a trip, on the plan and any room on the walk (where she'd start, each room she passes, the room she ends in) isn't known by heart. Code: `Simulation.WhyNotPlannable`, `Simulation.ByHeart.cs:46`. It is the *reason* half of several messages, so it shows in more than one place:
  - the red-edged toast, `Can't {task}: {reason}` or `Can't travel to {room}: {reason}` (`notices.cant`, `notices.cant_travel`; `NoticeToast`, visible 3 s then a 0.6 s fade);
  - the greyed reason on a room popover action row and its tooltip (`RoomPopoverText`);
  - the queue stop card, in place of the details line, for a trip she won't make.
- **What it must still tell the player:** (a) the problem is **one particular room** she doesn't know by heart; (b) **progress**: `{runs}` of `{needed}` runs worked there; (c) the **way out**: it can't be planned yet, so she has to work in that room **during a run** first. Without (c) the player has no route to fix it (principle 12).
- **Placeholders:** `{room}` (arrives already in title case with a leading The/A/An lowercased, e.g. "the Dark Corridor", via `GameText.TitleInSentence`, so it reads mid-sentence), `{runs}` (number), `{needed}` (number; today 4, from `byHeartRuns`, so never hard-code it).
- **Grammar constraints from where it is used:** it is a **fragment** that follows a colon, starting lowercase with **no full stop** (other `reasons.*` lines are the same: `she's already there`, `the way is shut`). The feed's `skipped: {task} skipped: {reason}.` adds the full stop itself.
- **Length limit:** none in code. The toast is one small box and is up for 3 seconds, so shorter reads better; keep it near the current length.
- **Tests:** `ScheduleTargetTests` build the expected string from the same key, so rewording does not break them. Keep the three placeholder names, or those tests need editing.

### 3. The Dark Corridor's shut message (`Assets/Data/Places/LeftCorridor.asset`, the way to the Dark Corridor, field `shutMessage`)
- **Current text:** `too dark to go in: the corridor's candles might show the way` (about 60 characters)
- **Where and when:** the Dark Corridor appears on the map (dim line) once the Left Corridor is 100% searched, but is shut until the corridor's candles are lit. If she tries to **travel there** (a map click, or Travel in a popover) it is the reason given. Code: `Simulation.Places.cs:164` and `ShutWayMessageInto` (`:238`, used when planning a walk that would pass through the shut way). Shown the same three places as line 2: the toast `Can't travel to the Dark Corridor: {reason}`, the popover, the stop card. Only shown **after** she has found the way; before that the generic `reasons.way_not_found` is used so the message can't give the way away.
- **What it must still tell the player:** (a) she can't go in **because of the dark**; (b) the **hint at the fix**, that the corridor's **candles** are what light the way. This is the "way to think, not grind" hint (GDD §14 principle 7), and the only on-screen pointer to the candle task, so it can be made more poetic but not vaguer than "candles".
- **Placeholders/variables:** none. (It is a literal string on the asset, not a text key.)
- **Grammar constraints:** same fragment shape as line 2 (after `Can't travel to {room}: `, lowercase start, no full stop). The Balance Sheet's Places tab shows `shutMessage` as a 200-character field, which is a display width, not a rule.
- **How the edit gets made:** in the Inspector (LeftCorridor → Ways → the way to the Dark Corridor → Shut Message) or the Balance Sheet, never by hand-editing the `.asset` YAML (CLAUDE.md). Anything with an apostrophe is saved as `''` in the YAML; the Inspector handles that.

## Voice rules for all three (`content.md`, GDD)
- Story passages and placeholders are in **Clara's voice: first person, present tense**. Existing system lines are not consistent: `reasons.*` are third person ("she doesn't know…"), the feed's `ended.walked_out` is third person ("She walks out…"), and `planning_unlocked` addresses the player ("you can plan"). Which voice these three should speak in is Question 2.
- British spelling for players; room names in title case.
- Prose is a feature but the loop must work with the placeholder text first (pillar 6): clarity of the rule wins over style if they collide.

## Next to these three (tell, not added)
Nothing below is part of this plan unless you say so.
- **Same feature, same placeholder status:** `reasons.way_shut` ("the way is shut", generic fallback for a shut way with no message of its own) sits right beside line 2; `feed.known_by_heart` ("{room}: she knows it by heart now. What she does here is queued for next time…") is the feed line that *precedes* the refusal in the player's experience and isn't marked placeholder, but its tone has to match line 2's.
- **Listed as placeholder wording in `PROJECT_NOTES.md`:** `map.warning_tip`, `map.not_carried_mark` ("won't carry over"), `queue.warning_tip_folded` (plan ui-033); the room-speed line ("×{now} here · ×{next} after 1 more run", `room_speed_tip`). All sit in the same planning/by-heart family. Several are UI labels with tight space, which is a different job from prose.
- **Marked `PLACEHOLDER` in `game_text.txt` but a different plan:** `reasons.count_at_max` ("all {max} {item} are already done this run", plan 013).
- **Story passages (`Assets/Story/`), many marked `[PLACEHOLDER]`:** 12, 13, 17–26. That is the real story writing and far larger than a wording pass; it should stay its own `/writing` task.
- **ui-051 / plan 042** cover the Summary's new lines, as you said.

## Questions for you (nothing below is decided)
1. **Room speed in the unlock line.** decisions-log says Feed's note should also say rooms known by heart now run faster; the current line doesn't. Should the rewrite carry both (planning *and* room speed), or planning only?
2. **Voice.** For each line: Clara's first person ("I can plan the next run…"), the game's third-person narrator ("she"), or direct address ("you")? One voice for all three, or the feed in Clara's voice and the refusal reasons plain third-person for clarity?
3. **How much hint in the shut message?** Keep an explicit pointer to the candles, or something subtler that still points at them (the §14 principle 7 line is that the player must always have a "way to think")?
4. **Refusal line: how plain?** It must give the room, the runs progress and the "do it during a run" way out. May `/writing` drop the progress count `({runs} of {needed})` if the new wording makes the way out clear another way, or must the count stay?
5. **Shut message home.** It lives as a string on the asset (not a `game_text.txt` key), unlike nearly all player text. Leave that for this pass, or add a backlog line to move it to a key later? (Not done here; moving it is a code change.)
6. **Scope.** Add `reasons.way_shut` and/or `feed.known_by_heart` to this pass, since they sit right beside the three? Any of the others above?

## Answers (the user, 2026-10-01, `/writing`)
1. **Room speed:** both. The unlock line carries planning and room speed.
2. **Voice:** third person ("she") for all lines, matching the other `reasons.*`, the feed's system notes and the Quickened Hours description. Clara's first person stays in blurbs and passages.
3. **Hint:** name the candles (the Left Corridor's, so it's clear which corridor).
4. **Count:** `({runs} of {needed} runs)` stays.
5. **Shut message home:** move it to a `game_text.txt` key now, not a backlog line. The key `reasons.shut_dark_corridor` is saved; the code change (the way's field holds a text key, renamed with `[FormerlySerializedAs]`, read through `GameText.Get` in `Simulation.Places.cs`; Balance Sheet label; `PlacesTests`, `PlanWarningTests`, `TutorialContentTests` updated, plus a check that the asset's key exists) and setting the key on `LeftCorridor.asset` in Unity are done in the Features lane, which has an editor. Until then the game still shows the old string from the asset.
6. **Scope:** `reasons.way_shut` and `feed.known_by_heart` added.

**Wording saved** (`game_text.txt`, placeholder comments replaced):
- `feed.planning_unlocked`: The candles burn to her rhythm now. She can plan each run before it begins, and rooms she knows by heart pass faster.
- `reasons.not_by_heart_plan`: she doesn't know {room} by heart yet ({runs} of {needed} runs), so it can only be done during a run
- `reasons.shut_dark_corridor` (new): too dark to go in; lighting the Left Corridor's candles might show the way
- `reasons.way_shut`: the way is shut for now
- `feed.known_by_heart`: {room}: she knows it by heart now. What she does here stays queued for next time, if every room before it is known too.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Assets/Text/game_text.txt` | Edit (by `/writing`) | reword `feed.planning_unlocked`, `reasons.not_by_heart_plan`; update their `# PLACEHOLDER wording (plan 032a)` comments when you approve the final wording |
| `Assets/Data/Places/LeftCorridor.asset` | Edit (Inspector, by `/writing`'s hand-over steps) | `shutMessage` on the way to the Dark Corridor |
| `PROJECT_NOTES.md` | Edit (propose, wait for OK) | remove the three from "Placeholder rules to revisit" once approved |

Save format change? No.

## Steps
1. You answer the questions above; I add the answers to this plan and mark it Approved.
2. In a `/writing` session (Design lane): propose options for each line, you choose or write your own; apply to `game_text.txt`; give exact Inspector steps for the asset line, or apply it for you if you ask.
3. Run `GameTextTests` and `ScheduleTargetTests` in a Unity lane (the Design lane has no editor) and look at each line on screen: trigger *Feed your hours* (the dev panel's jump stages help), try a trip to the Dark Corridor before the candles, and try planning with a room not known by heart.

## Tests
| Test | Proves |
| --- | --- |
| `GameTextTests` | every key still exists and is used |
| `ScheduleTargetTests` (`not_by_heart_plan` cases) | the reason still reaches the target, with `room`, `runs`, `needed` |
| `PlacesTests` | unchanged (they use their own shut message string) |

## Done when
- [x] The three lines reworded with your approval; the placeholder comments updated
- [x] Tests above pass; Console clean
- [x] In the game: each line seen in place and fits its box (toast, stop card, feed)
- [x] `PROJECT_NOTES.md` placeholder list edited (with your OK); backlog line "Writing pass (plan 032a)" removed; changelog line only if you want one (wording changes aren't a feature)

## Notes after implementation
- `Way.shutMessage` is now `Way.shutMessageKey` (`[FormerlySerializedAs("shutMessage")]`), a `game_text.txt` key read through `GameText.Get` by one helper, `Simulation.ShutMessageOf`, used for both the trip refusal and `ShutWayMessageInto`. Empty still falls back to `reasons.way_shut`. The Balance Sheet's Places column is now "Shut message key".
- `LeftCorridor.asset`: the way to the Dark Corridor with Hanging Mirrors holds `reasons.shut_dark_corridor`, set through `SerializedObject` (as the Inspector would). Unity also wrote out two default `afterSwitch: {fileID: 0}` lines on save.
- Tests: `PlacesTests` and `PlanWarningTests` set a key and expect `GameText.Get` of it; `TutorialContentTests` now checks the real asset's key exists in `game_text.txt`. All 860 EditMode tests pass.
