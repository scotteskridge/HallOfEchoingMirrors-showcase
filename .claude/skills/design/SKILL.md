---
name: design
description: "Design discussion in Claude Code: read only the GDD section at hand, settle a question, log the decision. Use for game-design talk (rules, pacing, story, balance ideas), not for coding."
model: opus
disable-model-invocation: true
---
A design session. Keep it cheap: the GDD and decisions log are large.

0. **Lane.** Follow `docs/parallel-lanes.md` → *Starting a task* (sync with the other lanes; check this is the right folder).
1. **Find, don't read.** Read `DesignNotes/VISION.md` (one page: the pillars). Then grep `DesignNotes/GDD.md` for the topic's headings (`grep -n "^#"`) and read only that § (use offset and limit). Then grep `DesignNotes/decisions-log.md` for the topic; if nothing, grep `decisions-log-older.md`. Never read either file whole. Read `future-sketches.md` only for Act II+ topics.
2. **Check the build when it matters.** If the question depends on what's built, use a subagent to look at the code or plans and report in a few lines; don't read code here.
3. **Discuss.** Give a recommendation with the trade-off, not a survey, and say which pillar it serves; flag one it fights. Use the GDD's labels and terms. Flag conflicts between code, GDD and decisions-log; never quietly settle an open question.
4. **Record.** When the user settles something: show the exact edit to that GDD § (and its label, e.g. [OPEN] → [DIRECTION]) and apply it on the user's OK, so the GDD stays the current design; commit messages for it start `GDD:`. Then add a short `decisions-log.md` entry at the top (date, the choice, why, the GDD § changed): the log records *why*, the GDD holds *what*. Never edit `VISION.md` unless the user asks. Send buildable outcomes to `docs/BACKLOG.md` as one line each (UI and look outcomes to `docs/UI-BACKLOG.md`).
5. **Share.** If the session changed files, propose a commit message and commit on my yes, then follow `docs/parallel-lanes.md` → *Finishing a task* so the other lanes get it. No `/wrap-up` needed.
6. **One topic per session.** Suggest `/clear` when it's settled.
