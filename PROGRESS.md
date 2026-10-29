# PROGRESS

Status of each phase against `MASTER_PLAN.md`. 150 automated tests pass (`dotnet test BuildingStudio.sln`).

| Phase | Status | Notes |
|---|---|---|
| 0 Foundation | Done | Solution, Core.Geometry, Core.Model (JSON save/load, versioned migrations, undo/redo commands), DI shell, CI. AvalonDock/MaterialDesign/Serilog not used (plain WPF tabs). |
| 1 Brief, rules, Vastu | Done | JSON rule packs (Vastu, TN bylaws template, rates, furniture, styles); brief form + offline NL parser + validator; Vastu 9-zone engine with explanations and fix suggestions; compliance engine. **GATE 1 test passes** (hand-drawn plan = manual score 88). |
| 2 Layout generator | Done (v1) | Corridor-spine generator, seeded simulated annealing (no OR-Tools/GeneticSharp), multi-floor stacking with aligned stairs, doors/windows/main entrance (Vastu pada). **GATE 2 passes**: 5 presets × 5 valid options, < 1.5 s each. |
| 3 2D editor | Done (v1) | Drag-swap, shared-wall move with neighbour adjust, lock, live Vastu/bylaw/area panel, explain, one-click fixes, re-solve around locks, text edits. Version snapshots exist in the command layer; compare view is the options cost table. DXF *import* not done (export only). |
| 4 3D | Done | Walls with real openings, glass, doors, chajjas, slabs with stair wells, dog-legged stairs, roofs (flat/parapet, pergola, sloped tile), floor cut-away, structure view; glTF + shareable HTML viewer. Walk mode is an orbit/tour, not first-person. |
| 5 Structure | **Partial — gate not met** | Grid/columns/beams/slabs/footings, IS 456 sizing, IS 1893/875 summaries, BBS, golden tests. No frame solver, no OpenSees validation. See `docs/VALIDATION.md`. |
| 6 Interiors | Done | Catalog + auto-furnish with clearances (bed head S/W, cooking facing E, TV opposite sofa), finishes, lumen-method electrical schedule. **GATE 6 test passes** for all 13 room types. Per-room 3D views use the main viewport. |
| 7 Exteriors | Done | Four style presets, materials notes (coastal), compound wall, gate, driveway, sump/septic/RWH/trees placed by Vastu, OHT on roof, 4 elevations. **GATE 7 test passes.** Day/night in the HTML viewer. |
| 8 Drawings, BOQ, reports | Done | Site, floor plans, 4 elevations, section, column/footing layout, schedules → SVG + DXF; BOQ Excel with formulas; PDF client report; option cost comparison. **GATE 8 test passes** (one-click export). Sheets are model-space drawings, not authority title-block format. |
| 9 AI layer | Partial | Provider abstraction (none / Ollama / Gemini), AI brief → schema-checked JSON with offline fallback, NL edits, review notes. **GATE 9 test passes** (everything works with AI off). Scanned-plan recognition (9.4) not built. |
| 10 Release | Partial | 3 sample projects, CI with Windows publish artifact. Not done: IFC export, installer, licensing, crash reporting, tutorial. |

## Next
1. Frame solver + validation suite (Phase 5.3–5.5), then lift the "preliminary" scope.
2. Non-rectangular plots and L-shaped rooms (switch geometry to NetTopologySuite/Clipper2).
3. DXF import, scanned-plan recognition, IFC export, installer.

## Known issues
- The WPF app compiles on Linux CI but was not launched in this environment (Windows only); test it on Windows first.
- Bylaw and rate packs are templates; verify locally.
- Options for very small plots can be similar to each other.
