---
name: plan-feature
description: Turn a feature request into an approved written plan in docs/plans/ before any code is written. Use for anything that touches several files, adds a new system, or where the approach is unclear.
model: opus
argument-hint: "[feature, or a backlog item]"
disable-model-invocation: true
---
Plan this feature: $ARGUMENTS

Don't write or edit code in this skill. The output is a plan file.

0. **Lane.** Follow `docs/parallel-lanes.md` → *Starting a task* (sync with the other lanes; check this is the right folder).
1. **Find the design.** Read `DesignNotes/VISION.md` and name the pillar(s) this feature serves; if it fights one, say so before planning. Grep `DesignNotes/GDD.md` (and the Balancing Formulas doc, if relevant) for the sections this touches, and read only those. Check `DesignNotes/decisions-log.md` for anything newer. Check `docs/BACKLOG.md` and `docs/UI-BACKLOG.md`: if the feature is listed, plan that item and move it to *Now*. Quote the section numbers you're building against. If the design marks the point [OPEN], or the GDD and decisions log disagree, stop and ask me to decide.
2. **Find the code.** Use a subagent to locate the existing types, `Simulation.*.cs` files, assets, tests and helpers this feature will touch or could reuse, so the file reads stay out of this conversation. Ask it for file paths, type names and one line each, not file contents.
3. **Interview me** about anything the design and code don't settle: edge cases, what the player sees, what's out of scope. A few focused questions at a time. Skip anything you can answer yourself. Explain any term I might not know.
4. **Park future work.** If a later feature comes up while planning, add it as one line to the backlog it belongs in (UI and look work in `docs/UI-BACKLOG.md`, everything else in `docs/BACKLOG.md`; usually under *Later* or *Ideas*) and keep it out of this plan. Don't build hooks for it now.
5. **Write the plan** to `docs/plans/NNN-short-name.md` (next number in sequence; a plan for UI or look work is named `docs/plans/ui-NNN-short-name.md`, with NNN continuing the same sequence) using `docs/plans/_TEMPLATE.md`. Aim for one screen. A feature needing more than ~8 steps should be split into two plans; propose the split.
6. **Show me the plan and stop** for my review; I may edit it.
7. **On my approval:** set its Status to Approved, add it to *Open plans* in `docs/plans/README.md`, propose a commit message and commit on my yes, then follow `docs/parallel-lanes.md` → *Finishing a task* so the plan reaches the lane that builds it. No `/wrap-up` needed. Then tell me: in the Features folder (UI folder for a `ui-` plan), `/clear` (or a new session), then `/implement docs/plans/<file>` (it switches to Sonnet itself).
