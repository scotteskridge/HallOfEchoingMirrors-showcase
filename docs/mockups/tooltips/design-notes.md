# Tooltip states: design chat notes (2026-09-29)

From the design chat (Claude Design), with `Tooltips — six states@2x.png`. This is a proposal, **not settled**: the open points are listed at the end. The mock-up uses EB Garamond + Inter.

## Rule underneath
**No prose where a number will do.** Every fact gets a column. The one sentence that survives is the rule the player can't infer.

## Problem with the current tooltip
It has seven facts: duration, cost, skill multiplier, XP to skill, XP to mastery, repeat rule and progress. All are ~12 px, one colour, written as sentences, in a box that covers the panel it describes.

## Shape: one layout, six states
- **Size:** fixed 340 px wide (260 for the compact form). It never overlaps the thing it describes.
- **Kicker:** small caps, letter-spaced, naming the verb class and the place ("SEARCH · THE MIRROR'S LABORATORY").
- **Title:** in the serif.
- **Fact rows:** a label column (Takes, Costs, Trains, Faster with, Gives, Bleeds, Opens), then the values.
- **Actual large, base small:** "1.1s" at ~22 px beside "from 3.9s base" at ~11 px, plus "−0.3s since last run" on the right. Mastery is only satisfying when the number visibly moves (§14.19).
- **Colour carries meaning:**
  - times: amber;
  - vitality: rose;
  - met conditions and "kept": green;
  - unmet conditions: orange.

  Add "flat · skill cannot reduce it" where it applies.
- **XP as chips, one dot per skill:** "● Wayfinding +12". The mastery sentence sits under the chips because it is a rule, not a number.
- **Footer = state, not description:** the repeat rule, "0 / 4" and a progress bar, in a darker strip.

## The six states
1. **Action, full:** as above.
2. **Common verb, compact** (260 px): the title, then "1.0s · no cost · trains nothing · 5 → floor". Most tooltips should look like this.
3. **This will end the run:** the border, header and cost turn red together, with "you have 34" and a footer saying what will happen ("Queuing it will end the run where she stands"), never "insufficient vitality".
4. **Skill from the top bar:**
   - the title with ×3.54 at the right;
   - a table with the columns Level, XP and Speed;
   - two rows: "This run" and "Mastery KEPT", with a bar;
   - a one-line rule: levels reset each run and mastery doesn't.
5. **Carried item:** "CARRIED · 1 POCKET · ONE OF A KIND", with the rows Bleeds, Trains and Opens, and **one line in Clara's voice** in italic serif. Prose goes on item tooltips only, never on action tooltips (those are read mid-decision).
6. **Not available yet (locked):** a lock icon, then every condition, ticked green when met, with the unmet one carrying the only instruction (§14.12: the player must be able to diagnose it).

## Interaction
- 8 px gap from the thing described; the tooltip flips near the screen edge.
- A 350 ms delay before the first tooltip opens, then 0 ms when moving between neighbouring items, so scanning a list isn't a slideshow.

## Answers so far (the user and the design chat, 2026-09-29)
- **Locked actions:** the user thinks an action either shows greyed out or doesn't show until its conditions are met. **Check this in the code.** Some actions are **secret**: they need exploring, or clues from reading the text. The locked form must never reveal a secret action. It is only for actions the player already knows exist.
- **Can't afford it today:** the action is refused, and a red-framed notice appears bottom left (`NoticeToast`), e.g. "Can't light a candle: she can't hold any more…". Still to check: what happens when the vitality cost is more than she has left. In the user's second screenshot, a tooltip covers that notice, which is one more case for "never covers what it describes".
- **§14.12** (main list): *the player must be able to diagnose a failed run*. "Difficulty comes from a hard problem, not a hidden one." §13a's run report serves it too.
- **§14.19** (Koster block): *make mastery's effect visible and moving*. "A chain that visibly goes 85s → 70s → 50s across runs reads as progress." (GDD v0.4 had §14.18 out of place; v0.5, which is in the repo, fixes it. §12 and §19 are unaffected.)
- **Per-action "since last run": no data for it yet.** Only milestones are timed against the last run.
  - **The design chat's proposal:** don't store a time per action. Store **one float per skill: its speed multiplier at the start of the last run**. Then `delta = base/speedNow − base/speedAtLastRunStart` works for every action that skill governs, even ones never done.
  - **What it measures:** mastery growth only, so it's often zero. Show it only when it isn't zero, never "−0.0s".
  - **Save format:** it's a new `PersistentState` field, so it bumps the save format version.
  - It pairs with **best-ever milestone times** (backlog *Later*; §13a *Benchmarks*). Both are the same "kept per thing, last time" problem, and one plan could do both.

## Settled 2026-10-01 (decisions log *Tooltips: the open points settled*)
State 6 only for actions already listed but missing something (nothing hidden becomes visible); state 3 is an estimate worded "likely", a warning only; "since last run" gets its own plan after ui-036; compact form for the common verbs; serif EB Garamond; item voice lines as `[PLACEHOLDER]` keys. The list below is kept as the questions that were asked.

## Open (answered above): need the user's decision (and a GDD check) before building
- **Locked actions shown (#6):** today a locked action doesn't appear at all. Showing it changes what the player sees.
- **Warning form (#3):** check what queuing an action she can't afford does today (the decisions log says actions are dropped or refused with a reason).
- **"−0.3s since last run" per action:** needs a per-action time from the last run. That may be new tracking and a save format change.
- **Item lines in Clara's voice:** the user's prose. Build them as `[PLACEHOLDER]` keys.
- **Serif:** EB Garamond (the mock-up) or Cormorant Garamond (plan 011).
