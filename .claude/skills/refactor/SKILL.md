---
name: refactor
description: Safely carry out one refactor (a structural change with no change in behaviour), usually an item from a code-health report or the backlog. Tests pin the behaviour first; small verified steps; one commit per step.
model: sonnet
argument-hint: "[what to refactor, or a backlog item]"
allowed-tools: Bash(git status *) Bash(git diff *) Bash(git log *)
disable-model-invocation: true
---
Refactor: $ARGUMENTS

A refactor changes how the code is built, **never what the game does.** If a step would change behaviour, stop and tell me: that's a feature or a bug fix, and it gets its own task.

0. **Lane.** Follow `docs/parallel-lanes.md` → *Starting a task* (sync with the other lanes; check this is the right folder).
1. **Branch.** If we're on `main`, propose `refactor/<short-name>` and wait for my OK before creating it and switching to it.
2. **Understand.** Read the code involved and the matching `.claude/rules/` file. Use the LSP tool (find references, call hierarchy) to list everything that depends on it; if it isn't available, say so and fall back to Grep. Tell me the blast radius in one or two sentences.
3. **Pin the behaviour.** Check that tests cover what this code does. Where they don't, write *characterisation tests* first (tests that record what the code does today, even if it's odd), run them, and confirm they pass **before** changing anything.
4. **Plan the steps.** List small steps, each leaving the code compiling and passing, e.g.: extract a method → move it to the right class → replace the old copies → delete the dead code. Keep it under ~6 steps; if it needs more, propose splitting the refactor. Show me the steps and ask, once, whether you may commit after each step; wait for my OK. A yes covers this refactor only.
5. **Step by step.** For each step:
   - make the change (use LSP rename for renames, so every reference updates). Renaming a MonoBehaviour or a serialized field: warn me first, rename the file together with its `.meta`, and add `[FormerlySerializedAs]`. Saved field names (such as `expertiseXp`) never change.
   - compile, check the Console, run the tests for the area
   - commit (if I agreed in step 4), with a message starting `refactor:` and saying what moved and why
6. **Verify nothing changed.** Run the full EditMode suite. Confirm that no test was edited except to follow a rename or a move. If an assertion had to change, that's a behaviour change: stop and flag it.
7. **Finish with `/wrap-up`.** The reviewer checks against `docs/CODE-STANDARDS.md`. Mark the item done in `docs/BACKLOG.md` and, if one exists, the code-health report.
8. **Teach.** Name the refactoring you did (Extract Method, Move Method, Replace Conditional with Polymorphism, Introduce Parameter Object…), and explain in plain language why the code is better now.
