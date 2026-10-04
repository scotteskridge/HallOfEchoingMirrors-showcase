# Code standards — Hall of Echoing Mirrors

The standard that every code review measures against: the `reviewer` agent for each change, and `/code-health` for periodic whole-codebase audits.
Project-specific rules live in `CLAUDE.md` and `.claude/rules/`. **They are part of this standard, and they win if anything here conflicts.**

Severity used in reviews:
- **🔴 Fix now:** a bug risk, broken project rule, or data loss risk.
- **🟠 Fix soon:** debt that will slow or break the next features in that area.
- **🟡 Polish:** worth doing while you're in the file anyway.

---

## 1. Design and structure

- **One reason to change.** A class does one job. If you describe it with "and" (it tracks pockets *and* formats text *and* saves), it wants splitting. For the `Simulation` partial files: one topic per file, as listed in `.claude/rules/simulation.md`.
- **Size signals** (triggers to look closer, not hard limits): file > 300 lines, method > 40 lines, nesting deeper than 3, more than 4 parameters, a class with more than ~10 fields.
- **Duplication.** The same logic in two places is a 🟠; three places is a 🔴. Extract it once there's a second real copy (rule of three for trivial snippets). Check the project's shared helpers first (`UiText`, `ActionText`, `TemplateList<T>`, `UiStyle`, `FeedNotes`, `ClaraTips`, `GameText.TitleInSentence`, `EditorUiFactory`, `Simulation.RoomFor`, `SimulationTestBase`).
- **Feature envy.** A method that mostly reads another class's data belongs in that class.
- **Data clumps.** The same 3+ values passed around together (item + amount + room) want a small type, usually a `struct`.
- **Primitive obsession.** Strings or ints standing for a concept want a type: an enum for a fixed set, a small struct for a value with rules, a content `Id` for content.
- **Type switches.** A `switch`/`if` chain on "what kind of thing is this" that appears in more than one place wants data (a field on the asset) or polymorphism. One switch in one place is fine.
- **Shotgun surgery.** If adding one kind of content means editing many files, the design is missing a data-driven hook. Report it; don't fix it without a plan.
- **Dead code.** Unused methods, fields, `using` lines, and commented-out code: delete them. Git keeps the history. Setup steps that have already run are retired as `.claude/rules/editor-tools.md` says (restore the empty `GreyboxSetup.cs`), not deleted.
- **Speculative generality.** Interfaces with one implementation, abstract bases with one child, parameters nobody passes, "for later" hooks: remove unless a test needs the seam. Future features go in `docs/BACKLOG.md`, not in the code.

## 2. Data structures

- **Pick the collection for how it's used:**
  - lookup by key or `Id` → `Dictionary<TKey, TValue>`
  - "is it in the set?" → `HashSet<T>`
  - ordered, indexed → `List<T>` or an array
  - FIFO / LIFO → `Queue<T>` / `Stack<T>`

  Repeated `List.Find` / `Contains` / LINQ `First` on the same list is a sign it should be a dictionary or set.
- **No parallel collections.** Two lists kept in step by index (`names[i]`, `counts[i]`) should be one list of a small type.
- **Protect state.** Expose `IReadOnlyList<T>` / `IReadOnlyDictionary<TKey, TValue>`, not the mutable collection. Fields are `private`; use `readonly` where the reference never changes, and `const` for true constants.
- **Small immutable values** (a cost, a range, a position) are `readonly struct` types, not classes with setters. Anything serialized (assets, saves) needs `[Serializable]` and non-readonly fields: Unity's serializer and `JsonUtility` can't read `record` types or readonly fields.
- **Per-run vs persistent.** New state goes in `LoopState` (resets) or `PersistentState` (kept), never both, and never in a static field (`GameText` is the one allowed exception).
- **Content references are assets or `Id`s**, never names or paths typed as strings.

## 3. Unity-specific

- **MonoBehaviours are thin.** They display state and forward input. Game rules live in the simulation, where tests can reach them.
- **ScriptableObjects are read-only data at runtime.** Changing an asset's fields while playing permanently edits the asset in the Editor. Per-run values belong in `LoopState` / `PersistentState`.
- **Hot paths stay cheap** (`Update`, per-tick simulation code, anything run per frame or per list row):
  - no `GetComponent`: cache the reference in `Awake` or wire it in the Inspector
  - no LINQ, `new List<>`, string concatenation, or closures allocated every frame
  - update UI text only when the value changes (`UiText` does this)
- **No `Find`, `FindObjectOfType`, `FindFirstObjectByType` or `FindAnyObjectByType` anywhere**, not only in hot paths: wire references in the Inspector. Known exceptions: `MapView` and `StoryPanel` (`.claude/rules/ui.md`); don't add more. Editor setup and layout tools (`Assets/Editor/`) may use `FindFirstObjectByType`: they have no Inspector to wire through.
- **Events are symmetric.** Every `+=` has a matching `-=` (subscribe in `OnEnable`, unsubscribe in `OnDisable`, or re-subscribe on `SimulationChanged`). A missing unsubscribe is a 🔴: it causes leaks and double-firing after a load.
- **Serialization.** `[SerializeField] private` rather than `public` fields. A renamed serialized field needs `[FormerlySerializedAs]`. Saves go through `SaveSerializer` and store content `Id`s. A save-format change bumps `SaveData.CurrentVersion` and adds an upgrade step in `SaveSerializer.FromJson`.
- **No `Time.deltaTime` or wall-clock reads in the simulation.** Ticks only. `TickEngine` is the one place real time becomes ticks; UI may use `Time.unscaledTime` for animation only; a save file's timestamp is fine.
- **No legacy `UnityEngine.Input`.** Use the Input System.

## 4. C# conventions

Based on Microsoft's .NET conventions, trimmed to what matters here:
- **Naming.** Types, methods and properties in PascalCase; locals and parameters in camelCase; private fields `_camelCase`; interfaces start with `I`; booleans read as questions (`IsFound`, `HasRoom`, `CanStart`). Names say what, not how (`RoomFor`, not `CalcTmp2`). Namespaces are `HallOfEchoingMirrors.*` (`Core`, `UI`, `EditorTools`).
- **Methods do one thing at one level of detail.** Early returns rather than deep nesting. No flag parameters that switch a method between two behaviours: make two methods.
- **Exceptions are for impossible states,** not for normal flow. Never catch and ignore. Validate at the boundary (loading, the Inspector, save files), then trust the data inside.
- **Nulls.** Check at the boundaries; don't scatter defensive null checks through the core. A null that "shouldn't happen" should fail loudly, not be skipped. (Loading skipping missing content with a warning is by design: `.claude/rules/simulation.md`.)
- **`var`** when the type is obvious from the right-hand side; spell it out otherwise.
- **String building** with interpolation (`$"…{x}…"`) for code-only strings. Player-facing text goes through `GameText`.
- **Access** as narrow as possible: `private` by default; `public` when tests or editor tools need it (they're separate assemblies, so they can't see `internal`).
- **Comments explain why.** If code needs a comment to say *what* it does, rename or extract instead. Public simulation types get a one-line `///` summary.

## 5. Tests

- **Every rule in the simulation has a test** that fails if the rule breaks. Bugs get a failing test first.
- **Test behaviour, not implementation.** Assert on what the player would see (vitality, pockets, what's on the floor), not on private fields.
- **One behaviour per test, and the name says it** as a sentence in underscored parts, like the existing suite: `RepeatingAFreeTask_StillEndsTheRun`.
- **No duplicate tests.** Parameterise (`[TestCase]`) instead of copy-pasting.
- **Use `SimulationTestBase` helpers.** No hand-built fixtures, no fixed English (`Reason(...)`), no real-time waits.
- **Tests are code too.** Duplication and unclear names in tests are findings.

## 6. Don't over-engineer

A review that only ever adds code is failing too. Before recommending a change, ask:
- Does it make the **next likely change** easier? Check `docs/BACKLOG.md`.
- Is the area **about to be redesigned**? If so, don't polish it now.
- Would a new developer find the code **easier to read** afterwards?

Don't recommend:
- design patterns for their own sake
- interfaces or factories with one user
- splitting a clear 50-line method into five 10-line ones that hide the flow
- abstractions "in case we need it"

---

*Sources: Microsoft .NET coding conventions; Unity's C# Style Guide (Unity 6 edition) and scripting and performance best-practice guides; the Fowler/Kerievsky code-smell catalogue as summarised by refactoring.guru; project rules in `CLAUDE.md` and `.claude/rules/`.*
