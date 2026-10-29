# AI Building Studio

Desktop app (WPF, .NET 8) that turns a house brief such as

> "Plot 30x40 ft, east-facing, G+1. Ground floor: living 14x16, kitchen 10x12, dining, 1 bedroom + attached bath, pooja, parking. First floor: 3 bedrooms, family lounge, balcony. Follow Vastu."

into ranked Vastu-scored floor plans you can rearrange, then a 3D model, preliminary structure, interiors, exteriors,
a drawing set (SVG/DXF), a BOQ (Excel), a client report (PDF) and a shareable 3D viewer (glTF + HTML).

**AI proposes, deterministic rules verify, the engineer approves.** Structural output is preliminary and must be
checked by a licensed structural engineer. Bylaw and rate packs are templates to verify locally.

See `MASTER_PLAN.md` for the full plan and `PROGRESS.md` for what is done.

## Run the desktop app (Windows)

```
dotnet run --project src/App.Wpf
```

Flow: **Brief** tab (type or pick a preset → Parse → check the form → Generate) → **Options** tab (apply, compare
costs, re-solve around locked rooms) → **Plan editor** (drag a room onto another to swap, drag a wall to move it,
click to select/lock, or type edits like "move pooja to NE") → 3D, Structure, BOQ and Drawings tabs → **Export**.

## Run from the command line (any OS)

```
dotnet run --project src/App.Cli -- demo --preset 4 --out out/demo
dotnet run --project src/App.Cli -- demo --text "30x50 north facing G+1, 3 bedrooms, pooja, parking, vastu" --style Traditional
dotnet run --project src/App.Cli -- edit --project out/demo/project.json --cmd "swap kitchen and dining"
dotnet run --project src/App.Cli -- gate2
```

`demo` writes `project.json`, `drawings/*.svg|dxf`, `boq.xlsx`, `report.pdf`, `model.glb`, `viewer.html` and `summary.md`.

Optional AI: `--ai ollama --model llama3.1` (local Ollama) or `--ai gemini` with `GEMINI_API_KEY`. Everything works with AI off.

## Tests

```
dotnet test BuildingStudio.sln
```

Gate tests: Vastu hand-check (Phase 1), 5 presets × 3+ valid options (2), one-model edits (4), furnishing without
clashes (6), four distinct styles (7), one-click export (8), AI off (9). Structural formulas have IS-code golden tests
(`docs/VALIDATION.md`).

## Layout

```
src/Core.Geometry  Core.Model  Core.Rules            geometry, the single BuildingModel, rule-pack loader
src/Engine.*        Brief Vastu Compliance Layout Structure Interior Exterior Quantity Drawing Model3D
src/IO.*            Gltf (+HTML viewer) Dxf Excel Pdf
src/AI.Services     provider abstraction, brief/edit parsing, review notes
src/Studio.Pipeline orchestration used by both apps
src/App.Wpf         desktop app        src/App.Cli   command line
rules/              vastu.default, bylaws.tn, rates.tn, furniture.catalog, exterior.styles (JSON, editable)
samples/            three generated sample projects (open in the app)
```

Units are millimetres internally; feet and metres only at the UI/IO edge.
