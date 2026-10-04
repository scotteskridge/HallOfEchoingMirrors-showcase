# Writing plan: game text (kept apart from `docs/plans/`, which is for code)

Last updated 2026-10-01 (ring buckets). A writing session reads this, `README.md` and `ProseVoiceGuide.md`, and nothing else unless the task needs it.

## How a writing session works
- Text only. No tests, no Unity refresh, no git unless the user asks (CLAUDE.md, Session hygiene).
- Draft in chat, save only after the user approves. One batch per session, then `/clear`.
- Every milestone passage ends by saying what changed and what is now possible, using the real room and button names from `docs/BUILD-STATE.md` (Smoky Mirror, A Dark Hall, Left/Right Corridor, Dark Corridor with Hanging Mirrors, Mirror's Laboratory; *Chase Roland*, *Search*, *Gather a wisp*, *Travel*).
- Present tense. "Twenty-one years". British spelling. Ambient lines 20 words or fewer. Check with: `awk '!/^#/ && NF>20' Assets/Story/Blurbs/*.txt`.

## Where text lives
- Passages: `Assets/Story/NN_name.txt` (title on line 1). Live on save.
- Blurbs: `Assets/Story/Blurbs/*.txt`. After editing, the user runs *Hall of Echoing Mirrors → Story → Import Blurbs*.
- Action hover text: the `description:` line of each task asset in `Assets/Data/Tasks/` (plain YAML text; double any apostrophe only inside a single-quoted string).
- UI strings and tooltips: `Assets/Text/game_text.txt` (large: search a key, read a slice, never the whole file).
- Do not read `Writing Guides/_originals/` (thousands of tokens; distilled already).

## Done (2026-09-29)
- Writing guides condensed to `.md`; Clara's canon fixed (41, 21 years on).
- Passages 01-08, 10, 11, 14, 15, 16 written.
- Task hover text saved for the 11 formerly empty tasks (Common: Explore, PickUp, PutDown, Travel; Hall: FillABottledWell, GatherAWisp, LightACandleInTheCorridor, LightTheCandles, SearchLeftCorridor, SearchRightCorridor; Tutorial: ChaseRoland), plus the 5 mirror-action tasks earlier.
- All three blurb files rewritten (lines 20 words or fewer); `ring_present` left as it was.
- Blurb buckets grown (need Import Blurbs to go live): hall_corridor 29, hall_dark 32, searching 31, start_travel 14, start_explore 15, start_search 11, low_vitality 22, instantiating 13, hall_shifted 9, gathering_wisps 10, candlelight 11. `reaching` stays short by design.

## Done (2026-10-01)
- Passages 17-21 (first entry to each room), 13 and 22-26 (the lab and the walk home) written; 10 and 11's action lists fixed (Clear the bench, Take the tome and Cut the stone were dropped from the lab).
- Game date settled: 1928.
- Lab task and item hover text written (10 tasks, 8 items); Feed your hours' description back to "I". Voice split settled: flavour descriptions "I", system lines "she" (decisions 2026-10-01).
- Blurb buckets grown (need Import Blurbs): start_chase 13, start_gather_wisp 13, start_instantiate 13, satchel 12, lab_sealed 13.
- Ring buckets grown (need Import Blurbs): ring_present 14, ring_carried 15, ring_refused 12. The over-long "He took it off…" line trimmed to 20 words (the user's OK). Guess to confirm: Roland mislaid umbrellas (`ring_present`).

## Next, in order
1. **Passage 13's last line** stays `[PLACEHOLDER]` until Draw on the mana stone is redesigned (backlog Next 9). **Passage 12** can never fire (Cut the stone was dropped); retiring it is a data job.
2. **Top up the other buckets** toward 30-40 if playtest shows they fire often. Any new bucket needs a code hook (backlog).
3. **Playtest the guesses** flagged in the last blurb batch: the table laid for four (`hall_dark`), the wooden toy (`searching`), "after each confinement" (`low_vitality`), "the seventh gives slightly" (`searching`). They imply a family of four, several births, and a link to the seven hues; confirm or cut.
4. **`game_text.txt` tooltips and UI strings**, in its own session, sliced by `##` section. The tooltip layout redesign (backlog Next 4) will change what fits, so do this after it or keep lines very short.
5. **Collapse text by location** (`DesignNotes/Writing Guides/ProseVoiceGuide.md`, job 5): needs a design decision on which locations, then a small code hook.

## Open decisions for the user
- **The ring: settled 2026-09-29.** Passage 09 and `ring_present`/`ring_carried` now use the mirror version. Answered 2026-10-01: what is pushed out of her pockets is whatever she has most of, usually restorative items (wisps, phials); the ring text keeps saying "something". "I left it where it lay" stays: it fits both a ring left in its mirror and one she has put down.
- **Guesses in the 2026-10-01 passages** (confirm or cut): the sconces and "ten" (18); Clara holding the ring as she enters the lab (21); "a term's salary" (23); the clock at twenty past ten, the bread, school at four (26). Passage 26 says nothing of her stats waking (the *stats arrive* moment is still `[PROPOSED]`).
- **"Hold on to it"** in passage 02 and **"the young man is Roland"** in passage 04 are my readings; confirm or change.

## Code-side follow-ups (not for a writing session)
- Backlog Now: blurb triggers and frequency.
- Retire passage 12 (*The Stone Is Cut*) and its switch: its task no longer exists.
- `VISION.md` still says 1907 (the user's file).

## Tool notes (save tokens)
- Never glob `*.md` or grep recursively from the project root: `Library/` holds thousands of files. Use `path=` on `Assets/`, `docs/` or `DesignNotes/`.
- No Bash heredocs with backslashes; use the Write and Edit tools.
- RTF files cost about three times the tokens of plain text; convert to `.md` first.
