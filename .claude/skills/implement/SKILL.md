---
name: implement
description: "Build an approved plan from docs/plans/ step by step with tests, then hand over to /wrap-up. Use when the user says implement <plan>."
model: sonnet
effort: medium
argument-hint: "[docs/plans/NNN-name.md]"
---
Implement this plan: $ARGUMENTS

0. **Lane.** Follow `docs/parallel-lanes.md` → *Starting a task*. A `ui-` plan is built in the UI lane, any other plan in Features; if this is the wrong folder, say which one to use and stop.
1. **Read the plan** whole, then only the files it names. Its Status must be Approved; if it's Draft or Done, stop and ask. If the plan file isn't there after syncing, it hasn't been shared yet: tell me to `/wrap-up` the session that wrote it.
2. **Check before building.** If a file or type the plan names doesn't exist or has changed, or a step needs a design answer the plan doesn't give, stop and ask; never settle it quietly (CLAUDE.md → Hard rules → Design).
3. **Build in the plan's order:** failing tests first for Core changes; after each step, CLAUDE.md → "Checking your work"; one progress line per step. Nothing beyond the plan: new ideas go in a list at the end, for the backlog.
4. **Unity setup:** use the plan's one-click setup menu items; give me numbered editor steps for anything manual.
5. **Finish:** say all steps pass (with the test results), list anything that differs from the plan, and tell me to run `/wrap-up`. Don't commit here.
