---
# Content data, content types, game text, story and blurbs.
paths:
  - "Assets/Text/**"
  - "Assets/Story/**"
  - "Assets/Data/**"
  - "Assets/Scripts/**/GameText*.cs"
  - "Assets/Scripts/**/*Definition.cs"
  - "Assets/Scripts/**/ContentAsset.cs"
  - "Assets/Scripts/**/GameContent*.cs"
  - "Assets/Scripts/Core/StoryBeat.cs"
  - "Assets/Scripts/Core/Blurb*.cs"
  - "Assets/Scripts/UI/TextKey.cs"
  - "Assets/Editor/BlurbImporter.cs"
  - "Assets/Editor/SceneLabelsWindow.cs"
  - "Assets/Tests/EditMode/{GameText,TutorialContent,Blurb,ContentId}Tests.cs"
---
# Content, game text and story

## Content is data
- `Assets/Data/GameContent.asset` lists every task (in display order), switch, node, skill, the common verbs (`travelVerb`, `exploreVerb`, `pickUpVerb`, `putDownVerb`: defined once, not one asset per use), the start node and the opening story.
- **All content inherits `ContentAsset`**, whose permanent `Id` (the asset's GUID) is what saves store. New content types must inherit it too.
- Resources are `ResourceDefinition` assets in `Assets/Data/Items/`, with **Lasts** (This Run / Forever / Carried) and a maximum. Tasks list them in Needs, Gives and Takes; switches point at them. Places are `NodeDefinition` assets in `Assets/Data/Places/`.
- Tasks have a `kind` (Search, Study, Gather, Instantiate…) that systems react to, e.g. which stat trains.

## Game text (`Assets/Text/game_text.txt`)
- Format: `key: text` under `## section`, `{name}` placeholders, `\n` for new lines. Hot-reloads while playing.
- Code reads it with `GameText.Get("section.key", ("name", value))`. `GameTextTests` checks every key the code uses exists.
- The simulation's skip/stall reasons are under `## reasons`, entry names under `## names`, scene labels under `## scene`.
- A way's shut message is a `game_text.txt` key (`Way.shutMessageKey`); empty falls back to `reasons.way_shut`.
- **Room names mid-sentence:** use `GameText.TitleInSentence`, which lowercases only a leading The/A/An ("Travel to the Dark Corridor"). `LowerFirst` is for action names ("Can't light a candle"). Title case keeps small words (of, with, the, a, and) lower case unless first.

## Story (`Assets/Story/`)
- Passages are `.txt` files: first line the title, the rest the passage. Story beats point at them.
- Never overwrite the user's story prose. Placeholders are marked `[PLACEHOLDER]`, in Clara's voice (first person, present tense).
- Blurbs (short lines for the story feed) are `Assets/Story/Blurbs/*.txt` (`## bucket` sections, `#` comments), imported with *Hall of Echoing Mirrors → Story → Import Blurbs* into `BlurbBucket` assets in `Assets/Data/Blurbs/` (lines replaced, rules kept).
