# NNN — Feature name

**Status:** Draft | Approved | Done
**Design:** GDD v0.x §…, decisions-log YYYY-MM-DD  <!-- what this builds against -->
**Pillar:** which `VISION.md` pillar this serves (or "none: tooling")

## Goal
One or two sentences: what the player can do or see afterwards that they couldn't before.

## Out of scope
- Things that sound related but are NOT part of this plan.

## Design assumptions
- Anything the design doesn't settle, and the placeholder we're using for it.

## Reuse
Existing code and assets this builds on (found by searching, not assumed):
- `TypeName` in `path/File.cs`: what it's used for here

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Simulation.Places.cs` | Edit | ... |
| `game_text.txt` | Edit | new keys: ... |

New content fields (each needs a Balance Sheet column / item section if balance-relevant):
- ...

Save format change? No / Yes → bump `SaveData.CurrentVersion`, upgrade step: ...

## Steps
1. Write failing EditMode tests for ...
2. Implement ...
3. Setup script (if needed): *Hall of Echoing Mirrors → Setup → ...*

## Tests
| Test (class.method) | Proves |
| --- | --- |
| | |

## Done when
- [ ] Tests above pass; compile and Console clean
- [ ] In Unity: <what to click, what should appear>
- [ ] decisions-log updated; GDD § edit proposed; changelog line added if the player can see it (player language); rules-file line proposed if a new system was added

## Notes after implementation
<!-- filled in at wrap-up: what changed from the plan and why -->
