---
name: writing
description: "Story and text session: rewrite player-facing text and story prose only. Use for blurbs, task text, story beats and game_text.txt; not for code."
model: opus
disable-model-invocation: true
---
A writing session: $ARGUMENTS

Edit text only. Skip tests and Unity refresh; use git only for the lane sync (step 0) and the Share step, unless asked.

0. **Lane.** Follow `docs/parallel-lanes.md` → *Starting a task* (sync with the other lanes; check this is the right folder).
1. **Read first, in this order:** `DesignNotes/Writing Guides/README.md`, then `WRITING-PLAN.md`, then `ProseVoiceGuide.md`. Read `BookVoiceSample.md` and `PlotSummary.md` only if the task needs them. Never read `_originals/`. (The voice guide and novel background files are private and not in this public copy.)
2. **Read only the text at hand:** grep `Assets/Text/game_text.txt` and `Assets/Story/` for the keys or files in the task; don't read them whole. Grep the GDD or `decisions-log.md` only for a story question, one § at a time.
3. **Don't open code, scenes or data assets.** If a text change needs a code or data change, list it at the end for another session.
4. **Rules that still apply:** never overwrite the user's story prose; mark placeholders `[PLACEHOLDER]`, in Clara's voice (first person, present tense); British spelling; room names in title case; player-facing text stays in `game_text.txt` keys.
5. **Finish** by listing which keys or files changed and any open story questions. Never quietly settle one.
6. **Share.** If the session changed files, propose a commit message and commit on my yes, then follow `docs/parallel-lanes.md` → *Finishing a task* so the other lanes get it. No `/wrap-up` needed.
