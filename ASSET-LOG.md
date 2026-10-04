# Asset log

Every piece of art, audio, font and third-party code that goes into the game, with where it came from and its licence. Kept up to date as assets are added, because:

- **Steam asks for an AI-content disclosure** on the store page: what was generated, with which tools, and whether anything is generated live in the game. This log is where the answers come from.
- **Every asset's licence must allow commercial use.** Check the tool's or creator's terms when adding the asset, and again before release, since terms change.

Add one row per asset (or per batch from the same source). "AI" means generated or substantially made with an AI tool.

## Art

| File | What it's for | Source / tool | AI? | Licence / terms | Added | Notes |
|---|---|---|---|---|---|---|
| `Assets/Art/Backgrounds/page_background.jpg` | The painted page behind every screen | Grok Imagine (xAI) | Yes | xAI consumer terms: the user checked them 2026-09-30 and found nothing against use with a Steam AI disclosure; see `Assets/Art/GROK-IMAGINE-LICENCE.md` | 2026-09-24 | First art in the project. Was `grok-image-5550d8d2-….jpg` (renamed 2026-09-29). |
| `Assets/Art/MirrorFrames/*.png` (3 frames) | Room frames on the map | Grok Imagine (xAI) (the user, 2026-09-30) | Yes | xAI consumer terms: the user checked them 2026-09-30 and found nothing against use with a Steam AI disclosure; see `Assets/Art/GROK-IMAGINE-LICENCE.md` | before 2026-09-29 | |
| `Assets/Art/Icons/*_64.png` (5 stats, 5 skills, vitality) | Stat and skill icons (plan 010) | Drawn by a Python script written in a Claude (AI) session | Yes | Made for this game | 2026-09-29 | The SVG sources and script were kept outside the project. |
| `Assets/Art/Backgrounds/map_backdrop.png`, `map_vignette.png` | Placeholder map backdrop and vignette (plan ui-026) | Drawn by the editor's Setup Step 96 (plain maths: a dark gradient and a radial fade) | No | Made for this game | 2026-09-30 | Placeholders, to be replaced by painted art. |
| `Assets/Art/Candles/grok-image-3bae3bc1-….png` | Placeholder candle flames on the planning screen (plan ui-024c) | Grok Imagine (xAI) | Yes | xAI consumer terms: the user checked them 2026-09-30 and found nothing against use with a Steam AI disclosure; details and the open attribution clause in `Assets/Art/GROK-IMAGINE-LICENCE.md` | 2026-09-30 | To be replaced or kept at the art pass (UI-BACKLOG-later). Its warm light is drawn live by a hand-written shader (plan ui-024c), not AI. |

## Fonts

| File | What it's for | Source | Licence | Added | Notes |
|---|---|---|---|---|---|
| `Assets/Fonts/BoecklinsUniverse/` (`Boecklins Universe.ttf`, its SDF asset, a second `.ttf` and the font's PDF sample sheets) | The menu title only (from plan 011, 2026-09-29) | Peter Wiegel, via fontspace.com (`info.txt`) | SIL Open Font License 1.1 (`Open Font License-*.txt` beside it, © 2013 and 2018 Peter Wiegel): **the user to confirm** | before 2026-09-24 | OFL allows commercial use and embedding. Moved into its own folder 2026-09-29. `Boecklinsuniverse-x3jKm.ttf` there is unused: keep or remove. |
| `Assets/Fonts/EB_Garamond/static/EBGaramond-{Regular,SemiBold,Italic,SemiBoldItalic}.ttf`, and their SDF assets in `Assets/Fonts/` | Headings, room and action names, story text (plan 011) | Google Fonts | SIL Open Font License (`OFL.txt` beside them) | 2026-09-29 | Fine for commercial use and embedding. Chosen over Cormorant Garamond to match the design mock-up. |
| `Assets/Fonts/Inter/Inter_18pt-{Regular,SemiBold,Italic,SemiBoldItalic}.ttf`, and their SDF assets in `Assets/Fonts/` | Body text, tooltips, buttons, chips, numbers, small print; TextMeshPro's default font (plan 011) | Google Fonts | SIL Open Font License (`OFL_Inter.txt` beside them) | 2026-09-29 | Fine for commercial use and embedding. |
| `Assets/TextMesh Pro/…` (LiberationSans) | TextMeshPro's last fallback font | Unity TextMeshPro package | SIL Open Font License | project start | Fine for commercial use. From plan 011 the fallbacks in TMP Settings are Inter, then the LiberationSans ones already there. |

## Audio

| File | What it's for | Source / tool | AI? | Licence / terms | Added | Notes |
|---|---|---|---|---|---|---|
| *(none yet)* | | | | | | |

## Code and packages

| Package / code | Source | Licence | Notes |
|---|---|---|---|
| Unity packages (URP, Input System, TextMeshPro, 2D, Test Framework) | Unity | Unity Companion / Unity Terms | Covered by the Unity licence. |

## Story text

Written by the author (Scott Eskridge), based on the author's own novel. Placeholder passages marked `[PLACEHOLDER]` were drafted with Claude (AI) and are to be rewritten before release. If any AI-drafted text ships, it needs mentioning in the Steam disclosure.
