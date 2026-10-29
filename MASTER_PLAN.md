# AI BUILDING STUDIO: Master Build Plan for Claude Code

> **How to use:** Create an empty folder, put this file in it as `MASTER_PLAN.md`, open Claude Code there and say:
> *"Read MASTER_PLAN.md fully. Execute it phase by phase. Complete each task, run its tests, commit, and then move to the next. Stop at each GATE and show me a summary."*
> Copy Section 2 into `CLAUDE.md` so every session and sub-agent follows the same rules.

---

## 0. PRODUCT VISION

A free-to-build **WPF (.NET 8) desktop app** where the user types requirements such as:

> "Plot 30x40 ft, east-facing, G+1. Ground floor: living 14x16, kitchen 10x12, dining, 1 bedroom + attached bath, pooja, parking. First floor: 3 bedrooms, family lounge, balcony. Follow Vastu."

and the app:

1. **Generates 3-5 ranked 2D floor plan options** that respect plot, setbacks and Vastu.
2. Lets the user **drag, resize, swap and rearrange rooms**, with live Vastu score, bylaw check and area balance.
3. Builds the **3D model** (G+N, stairs, roof, openings).
4. Generates **structure** (columns, beams, slabs, footings), analysis and design.
5. Generates **interiors** (furniture layouts, finishes, lighting, 3D walkthrough).
6. Generates **exteriors** (elevation styles, facade, materials, compound wall, landscaping).
7. Outputs **drawings (plan/section/elevation), BOQ, cost estimate, compliance report and a calculation report**.

Target customers: small architects, builders, contractors, and home owners in India (tier 2/3 cities). Region 1 = **Tamil Nadu / Puducherry** (default rule pack), with an editable rule pack system for other states.

**Non-negotiable principle:** AI proposes, **deterministic rules and solvers verify**, the **human engineer approves**. An LLM never certifies structural safety.

---

## 1. INPUTS AND OUTPUTS

### Inputs (the "Brief")
| Group | Fields |
|---|---|
| Plot | boundary polygon (draw / DXF import / width x depth), north angle, road side(s), plot area |
| Orientation | facing (N/E/S/W/NE...), Vastu on/off + strictness (strict/moderate/flexible) |
| Floors | G+0 ... G+5, floor height, basement/stilt parking option |
| Rooms | per floor: type, count, target area or W x D, min/max area, must-have adjacency, attached bath yes/no |
| Special | pooja, study, store, utility, balcony, terrace, lift, staircase type, car parking count |
| Bylaws | setbacks (front/rear/sides), FAR/FSI, max height, ground coverage, parking norm |
| Site | soil SBC, seismic zone, wind speed, water table |
| Materials/Style | grade of concrete/steel, brick type, flooring, exterior style (modern/traditional/Chettinad/contemporary), colours, budget range |
| Rates | editable rate table (material, labour) per region |
| Optional | scanned floor plan image to trace; reference images for style |

### Outputs
- 2D plans (ranked options + the user's edited final one), with Vastu report per room
- 3D model (walls, slabs, openings, stairs, roof, columns, beams, footings)
- Interior packages (furniture, finishes, lighting, 3D views)
- Exterior packages (elevations in 4 sides, materials, landscaping, compound wall/gate)
- Structural layout, analysis results, member design, rebar schedule (BBS)
- Drawing set: PDF + DXF (plans, sections, elevations, structural, schedules)
- BOQ and cost estimate (Excel), cost comparison between options
- Compliance report (bylaw and Vastu), calculation report (PDF)
- Exports: IFC, glTF/OBJ, DXF, PDF, XLSX, project JSON

---

## 2. CLAUDE.md (copy this block into `CLAUDE.md`)

```
# Project: AI Building Studio (WPF / .NET 8)
- Language: C# 12, .NET 8, WPF, MVVM (CommunityToolkit.Mvvm). Nullable enabled. Warnings as errors.
- Engines are UI-free class libraries. The WPF app only references them via interfaces + DI.
- Single source of truth: Core.Model.BuildingModel. All engines read/write it; never duplicate geometry.
- Units: internal = millimetres (double), angles = radians, Y-up in 3D, plan is X-Y. Convert only at the UI/IO edge.
- Every engine function is deterministic and pure where possible. Randomness takes an explicit seed.
- Tests first: xUnit + FluentAssertions. Every structural/design/BOQ formula needs a hand-calculated golden test with source clause noted (e.g. "IS 456 cl. 26.2.1").
- Rules (Vastu, bylaws) live in JSON rule packs under /rules, never hardcoded.
- Use only free, permissively licensed NuGet packages. Record each in THIRD_PARTY.md with license.
- Never claim structural safety from AI output. Show an "Engineer must verify" banner on all structural reports.
- Commit after each task: "<phase>.<task>: message". Do not start the next task until tests pass.
- Keep a PROGRESS.md log: what was done, what is next, known issues.
- Keep files < 400 lines, classes focused, public APIs documented.
```

---

## 3. TECH STACK (all free)

| Area | Packages / Tools |
|---|---|
| Core | .NET 8, WPF, `CommunityToolkit.Mvvm`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Hosting`, `Serilog`, `System.Text.Json`, `FluentValidation` |
| UI | `Dirkster.AvalonDock`, `MaterialDesignThemes` (or `HandyControl`), `Microsoft.Xaml.Behaviors.Wpf` |
| 2D editor | WPF `DrawingVisual` / `SkiaSharp.Views.WPF` for performance canvas |
| 3D | `HelixToolkit.SharpDX.Core.Wpf` (fallback `HelixToolkit.Wpf`) |
| Geometry | `NetTopologySuite`, `Clipper2`, `Triangle.NET`, `MathNet.Spatial`, `Geometry3D` helpers |
| Math / FEM | `MathNet.Numerics` (sparse + dense LA) |
| Layout optimisation | `Google.OrTools` (CP-SAT), `GeneticSharp` |
| CAD/BIM | `netDxf`, `ACadSharp`, `Xbim.Essentials` + `Xbim.Geometry` (IFC), `AssimpNet`, `SharpGLTF.Toolkit` |
| Documents | `QuestPDF` (community license; verify), `PdfSharpCore`, `ClosedXML` |
| Data | `LiteDB` (project/library store) or `Microsoft.Data.Sqlite` |
| AI local | `Microsoft.ML.OnnxRuntime`, `OpenCvSharp4`, `Tesseract`, `LLamaSharp` or Ollama HTTP, `Microsoft.SemanticKernel` |
| AI optional cloud | Provider-abstracted `IAiProvider` (Gemini free tier, Ollama local). App must work fully offline for non-LLM features |
| Rendering extras | glTF export + Three.js HTML viewer for sharing walkthrough with clients |
| Quality | `xUnit`, `FluentAssertions`, `Moq`, `BenchmarkDotNet`, `Verify.Xunit` (golden files) |
| Packaging | MSIX or Inno Setup, GitHub Actions free CI |

---

## 4. SOLUTION STRUCTURE

```
BuildingStudio.sln
src/
  Core.Geometry/        Vec2/Vec3, Polygon ops, offsets, booleans, tolerance
  Core.Model/           BuildingModel, Site, Storey, Room, Wall, Opening, Slab, Column, Beam, Footing, Stair, Roof, Furniture, Finish
  Core.Rules/           Rule pack loader (Vastu, Bylaws, Codes) + evaluator
  Engine.Brief/         Requirement parsing (form + natural language) to Brief object
  Engine.Layout/        Room-program solver, Vastu scoring, option ranking
  Engine.Vastu/         Zone map (16 zones / 9 grid), per-room rules, scoring and explanation
  Engine.Compliance/    Setbacks, FAR, height, parking, ventilation, min room size, stair rules
  Engine.Structure/     Grid gen, loads, frame solver, IS 456/1893/875/13920 design, rebar
  Engine.Interior/      Furniture library, auto-furnish, finishes, lighting, colour palettes
  Engine.Exterior/      Facade generator, elevation styles, materials, compound wall, landscape
  Engine.Drawing/       Plan/section/elevation projection, dimensioning, hatching, sheets
  Engine.Quantity/      BOQ take-off, rate tables, cost
  IO.Dxf/ IO.Ifc/ IO.Pdf/ IO.Excel/ IO.Gltf/
  AI.Services/          IAiProvider, prompt templates, plan-image recognition, NL commands
  App.Wpf/              Shell, 2D canvas editor, 3D viewport, panels, wizards
rules/
  vastu.default.json  bylaws.tn.json  codes.is.json  rates.tn.json  furniture.catalog.json
tests/ (one project per src project)
docs/ (SPEC, VALIDATION reports)
```

---

## 5. CORE DATA MODEL (Phase 0 deliverable)

```
BuildingModel { Id, Name, Version, Site, Brief, Storeys[], Structure, Interior, Exterior, Metadata }
Site         { Boundary(Polygon), NorthAngle, RoadSides[], Setbacks, Soil{SBC}, SeismicZone, WindSpeed }
Storey       { Level, Elevation, Height, Rooms[], Walls[], Openings[], Slab, Stairs[], Furniture[] }
Room         { Id, Type, Name, Polygon, TargetArea, Zone (Vastu direction), Locked(bool), Adjacent[], Finish }
Wall         { Id, Start, End, Thickness, Type(Ext/Int/Load), Material, RoomLeft, RoomRight }
Opening      { Id, WallId, Offset, Width, Height, Sill, Kind(Door/Window/Ventilator), Swing }
Column/Beam/Footing/Slab with Section, Material grade, Reinforcement
FurnitureItem{ CatalogId, Position, Rotation, RoomId }
```
All objects have stable GUIDs; every mutation goes through `ICommand` objects (undo/redo + change events so engines can recompute incrementally).

---

## 6. THE HEART: REQUIREMENT TO VASTU PLAN TO REARRANGE

### 6.1 Brief intake
- **Wizard form:** plot size, facing, floors, room list with sizes, Vastu strictness.
- **Natural-language box:** "30x40 east facing G+1, 3 bedrooms, pooja in NE" parsed to `Brief` by LLM, then **shown as an editable form** for confirmation. Never generate from unconfirmed parse.
- Validate: sum of requested room areas <= buildable area x efficiency (about 0.80-0.85 after walls and circulation). If not, show a clear message with suggestions (reduce area, add floor).

### 6.2 Vastu engine (rule pack `rules/vastu.default.json`)
Divide the buildable envelope into a **3x3 grid (9 zones)**, optionally a 16-zone refinement, oriented by North angle. Rooms get **preferred / acceptable / avoid** zones with weights. Strictness scales penalties.

| Room | Preferred | Acceptable | Avoid |
|---|---|---|---|
| Main entrance | N, NE, E (by facing pada) | NW | S-SW, SW |
| Living / hall | N, NE, E | NW | SW |
| Kitchen | SE | NW | NE, SW, centre |
| Master bedroom | SW | S, W | NE, SE |
| Children / guest bedroom | W, NW | N, E | SE (kitchen clash) |
| Pooja / prayer | NE | E, N | S, toilets adjacency, under stairs |
| Dining | W, E | S, N | SW |
| Study / office | W, NE, N | E | SW |
| Toilet / bath | NW, W | S | NE, SW, centre, next to pooja |
| Staircase | S, SW, W | NW | NE, centre |
| Store | NW, SW | W | NE |
| Water tank (underground) | NE | N, E | SW, SE |
| Overhead tank / heavy items | SW | S, W | NE |
| Septic | NW | W, S | NE, N, E |
| Brahmasthan (centre) | open / courtyard / light | | toilet, kitchen, stair, pillar |
| Parking | NW, SE | N, E | SW |

Extra rules: openings larger in N and E walls; heavier/higher mass in S and W; lower plinth in N-E; no cut/extension in NE and SW corners (corner-cut penalty); stairs rise clockwise; kitchen cooking platform faces east; bed head to south/west; entrance not facing directly into a stair or toilet.

**Scoring:** `VastuScore = 100 - sum(penalties)`, with per-room breakdown and a plain-language explanation ("Kitchen in NW: acceptable, -5. SE is preferred but occupied by master bedroom"). Rules are **soft constraints**: plot reality sometimes overrides, so always show the trade-off instead of silently failing.

### 6.3 Layout generator (`Engine.Layout`)
Pipeline:
1. **Zone allocation:** assign room types to Vastu zones (CP-SAT assignment problem: maximise Vastu weight subject to area fit).
2. **Space partitioning:** use **squarified treemap / BSP slicing within the envelope**, seeded by zone assignment, snapped to a 150 mm module (or 3 inch).
3. **Genetic refinement** (GeneticSharp): chromosome = slicing tree + room order; fitness =
   `w1*VastuScore + w2*AreaMatch + w3*Adjacency(kitchen-dining, bed-bath) + w4*Daylight/Ventilation(each habitable room touches an external wall) + w5*CirculationEfficiency + w6*StructuralRegularity(wall lines align to a column grid, aligned floors) + w7*Compactness(aspect ratio <= 2:1)`.
4. **Circulation:** add passages/lobby; ensure every room reachable; door placement rules (door near corner, swing not clashing, not facing toilet directly).
5. **Windows:** place per habitable room on external walls, preferring N and E for large openings.
6. **Multi-floor:** upper-floor rooms stack over ground-floor walls where possible (loadpath), staircase aligned across floors, wet areas stacked vertically.
7. **Output 3-5 ranked options** with thumbnails, scores and trade-off summary; deterministic given seed.

Fallback if solver can't satisfy all: relax lowest-weight constraints first and report which were relaxed.

### 6.4 Interactive rearrangement (the "make it perfect" step)
- Drag rooms; **neighbours auto-adjust** (shared walls move, constraint-based) or swap rooms by drag-onto.
- Resize room by dragging a wall; area changes shown live; **lock** rooms/walls to stop the solver moving them.
- **Live side panel:** Vastu score (per room), bylaw status, area table (target vs actual), ventilation check.
- **"Re-solve around my edits":** re-run the generator keeping locked rooms fixed.
- **"Explain" button** per room (why this location, what the penalty is).
- **"Fix suggestions":** one-click suggestions, e.g. "Swap kitchen and store to reach SE: Vastu +12, area -2%."
- Full undo/redo, version snapshots ("Option A", "Client revision 2"), side-by-side compare.

---

## 7. INTERIORS (`Engine.Interior`)
- **Furniture catalog** (JSON + glTF/box proxies): bed (single/double/king), wardrobe, sofa sets, TV unit, dining set (4/6), kitchen modular (L/U/straight, counter, hob, sink, fridge), study desk, pooja unit, shoe rack, toilets fixtures.
- **Auto-furnish:** per room type, run rule + optimisation (clearances: 600 mm walkway, 900 mm door swing, bed head to Vastu direction, TV opposite sofa, no furniture blocking windows/doors). Multiple style variants.
- **Finishes:** flooring (vitrified, wood-look, granite), wall paint palette, false ceiling (simple/cove), tiles in wet areas, skirting; auto colour palettes by style and Vastu colour guidance (editable).
- **Lighting plan:** points per room by area (lux rule), switch positions, fan points; output electrical point schedule.
- **Interior BOQ:** flooring area, paint area, tiles, ceiling area, modular kitchen running metres, wardrobe area.
- **Views:** per-room 3D view, walkthrough camera path, glTF export, shareable HTML viewer.
- **AI assist:** style chat ("make living room modern warm minimal"), reference-image mood extraction to palette/materials (local model or free-tier API), optional.

## 8. EXTERIORS (`Engine.Exterior`)
- **Elevation generator** from plan: window/door rhythm alignment, parapet, sunshade (chajja), balcony railings, planter boxes, roof forms (flat terrace, sloped Mangalore-tile, pergola).
- **Style presets:** Modern box, Contemporary mixed, Traditional South Indian / Chettinad, Colonial. Each = parametric rules (proportions, materials, cornices, columns, textures).
- **Materials:** exterior paint, cladding (stone, wood-look, HPL, ACP), texture plaster, tile; palette suggestions; weather suitability notes (coastal Puducherry humidity).
- **Site items:** compound wall, gate, driveway, car porch, lawn, trees, rainwater harvesting pit, solar water heater and panel space, overhead tank and septic placement per Vastu.
- **Outputs:** 4 elevations, perspective renders (viewport capture), day/night lighting presets, glTF export.

## 9. STRUCTURE (`Engine.Structure`), RCC G+N
- **Grid generation:** columns at corners, wall junctions, spans 3-5 m preferred (max 6 m), aligned vertically; avoid columns in the middle of rooms; Vastu-friendly (no column at brahmasthan where avoidable).
- **Members:** slab type detection (one-way/two-way), thickness by span/depth, beams along wall lines, plinth beams, footings (isolated/combined) from SBC.
- **Loads:** DL (self weight, finishes, walls), LL (IS 875-2), wind (IS 875-3), seismic static (IS 1893-1) with zone factor, soil type; combinations per IS 456 / IS 1893.
- **Solver:** own 3D frame stiffness-method (6 DOF/node), rigid floor diaphragm option, MathNet sparse solve. **Validate** against textbook cases and OpenSees/ETABS outputs before trusting it (Phase 5.5 gate).
- **Design:** IS 456 limit state for slab, beam (flexure, shear, torsion basic, deflection), column (axial + biaxial), footing (bearing, punching, one-way shear); IS 13920 ductile detailing; development length, lap, cover checks.
- **Rebar and BBS:** bar marks, shapes (IS 2502), cut lengths, weight, schedule export.
- **Report:** PDF calc report with clause references; banner "Preliminary. Must be reviewed and certified by a licensed structural engineer."

## 10. DRAWINGS AND DOCUMENTS (`Engine.Drawing`)
- Plans (furniture optional), sections (user-placed cut lines), 4 elevations via 3D projection + hidden-line removal, auto dimension chains, room tags with areas, door/window schedule, column/beam/footing schedules, layer standards (A-WALL, A-DOOR, S-COLM...), title blocks, scales (1:50/1:100), sheet composer, export **PDF and DXF**.
- **Compliance report:** setbacks, FAR, ground coverage, height, parking, min room sizes, ventilation, stair width/riser/tread, with pass/fail and plan highlights.

## 11. BOQ AND COST (`Engine.Quantity`)
- Take-off from model: excavation, PCC, concrete by member and grade, steel (from BBS), shuttering, brickwork/blockwork, plaster (deduct openings), flooring, painting, doors/windows, plumbing and electrical points allowances, interior items.
- Editable rate tables (region/year), contractor margin, GST, contingency; cost per sq ft; **option comparison** (e.g. layout A vs B, M20 vs M25).
- Excel export with formulas via ClosedXML.

## 12. AI LAYER (`AI.Services`), where AI really adds value
| Feature | Method | Guardrail |
|---|---|---|
| Brief from text/voice | LLM to structured JSON, schema-validated | Show editable form, confirm |
| Layout generation | Solver (CP-SAT + GA); LLM only for scoring *explanations* | Deterministic scoring |
| Scanned plan to editable plan | OpenCV (walls via Hough/contours) + OCR dimensions + optional ONNX model | User corrects in editor |
| NL edits ("move kitchen to SE", "make living 16x14") | LLM emits `ICommand` JSON | Preview + undo |
| Style/interior suggestions | LLM + rule catalog | Only catalog items |
| Design review notes | LLM summarises engine findings | Cites actual numbers from engines |
| Chat assistant | Retrieval over bylaws/code JSON | Always cites rule ID |
Provider abstraction `IAiProvider` (Ollama local / Gemini free / none). App fully usable with AI disabled.

---

## 13. EXECUTION PLAN (agent-sized tasks; each ends with tests and a commit)

### PHASE 0, Foundation
- 0.1 Create solution, projects, DI host, Serilog, AvalonDock shell, theme, `THIRD_PARTY.md`, CI workflow.
- 0.2 `Core.Geometry` (Vec2/Vec3, Polygon via NetTopologySuite, offset/boolean via Clipper2, tolerance). Tests.
- 0.3 `Core.Model` classes + JSON serialisation + versioned migrations + sample project.
- 0.4 Command system (undo/redo), event bus, project save/load.
- **GATE 0:** app opens, shows empty 2D + 3D panes, saves/loads a sample project. Summarise.

### PHASE 1, Brief, rules and Vastu engine
- 1.1 `Core.Rules` JSON loader + schema + `vastu.default.json`, `bylaws.tn.json`.
- 1.2 `Engine.Brief`: model, validation, wizard UI (plot, facing, rooms table, Vastu strictness).
- 1.3 `Engine.Vastu`: zone grid from envelope + north angle; per-room scoring with explanations. Unit tests for each rule row in Section 6.2.
- 1.4 Compliance engine v1 (setbacks, FAR, coverage, min sizes).
- **GATE 1:** given a hand-drawn sample plan, the Vastu score and bylaw report match a manual check.

### PHASE 2, Layout generator (the wow feature)
- 2.1 Buildable envelope from plot + setbacks; module snapping.
- 2.2 CP-SAT zone allocation; 2.3 treemap/BSP partition; 2.4 GA refinement + fitness terms.
- 2.5 Circulation, doors, windows placement; 2.6 multi-floor stacking and staircase alignment.
- 2.7 Option ranking and thumbnails; deterministic seeds; benchmark < 10 s per option.
- **GATE 2:** for 5 preset briefs (e.g., 30x40 E-facing 2BHK, 30x50 N-facing G+1 3BHK), produce 3+ valid options, with no overlaps, all rooms reachable and the Vastu score reported.

### PHASE 3, 2D editor and rearrangement
- 3.1 Canvas (pan/zoom/snap/grid/layers), 3.2 draw/edit walls, doors, windows, rooms.
- 3.3 Drag/swap/resize rooms with neighbour auto-adjust; lock; 3.4 live score panel and explain/fix suggestions.
- 3.5 "Re-solve around locks"; versions and compare; 3.6 DXF import/export of plans.
- **GATE 3:** user generates options, rearranges rooms, sees live Vastu/area/bylaw updates, saves a version.

### PHASE 4, 3D building
- 4.1 Wall extrusion with openings (boolean), slabs, floors stacking G+N; 4.2 stairs and roof/parapet/terrace.
- 4.3 Materials, lighting, section cut, orbit/walk modes; 4.4 glTF export.
- **GATE 4:** 3D of the chosen plan matches 2D after any edit (single model).

### PHASE 5, Structure
- 5.1 Grid/columns/beams/slabs/footings auto-layout with manual override.
- 5.2 Loads and combinations; 5.3 3D frame solver; 5.4 seismic and wind; 5.5 **validation suite vs hand calcs and OpenSees** (write `docs/VALIDATION.md`).
- 5.6 Member design (IS 456/13920); 5.7 rebar and BBS; 5.8 calc report PDF.
- **GATE 5:** all validation tests pass within tolerance (<2% on forces, documented). Do not proceed otherwise.

### PHASE 6, Interiors
- 6.1 Furniture catalog and rules; 6.2 auto-furnish per room with variants; 6.3 finishes/palettes/ceiling; 6.4 lighting and electrical points; 6.5 per-room views and walkthrough; 6.6 interior BOQ.
- **GATE 6:** every room type auto-furnishes without clashes (tests for clearances).

### PHASE 7, Exteriors
- 7.1 Facade rhythm and elevation styles; 7.2 materials and palettes; 7.3 site elements (compound wall, gate, landscape, tank/septic placement); 7.4 elevations render sets; 7.5 day/night.
- **GATE 7:** four style presets produce distinct, plausible 4-side elevations for the sample project.

### PHASE 8, Drawings, BOQ, reports
- 8.1 Plan/section/elevation projection and hidden-line; 8.2 dimensions, tags, schedules; 8.3 sheet composer (PDF/DXF).
- 8.4 Quantity take-off; 8.5 rates and Excel; 8.6 option cost comparison; 8.7 combined client report (plan, Vastu, 3D views, BOQ).
- **GATE 8:** full sheet set and BOQ generated from the sample project in one click.

### PHASE 9, AI layer
- 9.1 `IAiProvider` (Ollama/Gemini/none); 9.2 NL brief parser; 9.3 NL edit commands; 9.4 scanned plan to plan (OpenCV/OCR); 9.5 explanations and review notes; 9.6 style/mood chat.
- **GATE 9:** all AI features degrade gracefully when AI is off.

### PHASE 10, Polish and release
- IFC export; performance pass (large buildings); installer; licensing (offline key); crash reporting; sample projects (3); in-app tutorial; user manual; disclaimers and terms (engineer sign-off).

---

## 14. SUB-AGENT PARALLELISM (Claude Code)
After GATE 0, these can run in parallel in separate worktrees/branches because they only share `Core.Model`:
- Agent A: Phase 1.3 Vastu engine, Agent B: Phase 1.4 compliance, Agent C: Phase 5.3 frame solver (pure math, big test suite), Agent D: Phase 6.1 furniture catalog.
Then merge in order, running the full test suite after each merge. Use a **reviewer agent** to check each PR against this plan and code clauses.

## 15. WHAT MAKES CLIENTS BUY (build these polish items)
1. **"Plot to full drawing set and BOQ in 10 minutes"** demo flow.
2. **Vastu explained**, not just scored. Show the trade-offs and one-click fixes.
3. **One model:** moving a wall updates 3D, structure, drawings, BOQ, compliance.
4. **Approval-ready sheets** in local authority format.
5. **Published validation report** showing the solver matches known software.
6. **Offline, one-time price or low subscription**, a contrast to Revit + ETABS costs.
7. **Client presentation mode:** shareable 3D walkthrough link/HTML and a branded PDF.
8. **Revision tracking:** client changes compared side by side with cost deltas.
9. **Regional rule packs** sold as add-ons (state bylaws, Vastu schools, rate tables).

## 16. RISKS AND HONEST NOTES
- This is a large product. Ship **G+N residential RCC** (Phases 0-8) first; AI layer and IFC later.
- Vastu has regional schools and variations: keep rules editable and show sources in the rule pack.
- Structural output has legal and safety consequences: validation gates, engineer sign-off, disclaimers.
- Bylaws differ per municipality: treat the default pack as a template and verify with local authority.
- Verify every NuGet license (QuestPDF, Xbim, etc.) before commercial sale.
- Free LLMs vary in quality: keep AI to parsing, explaining, suggesting; keep decisions in solvers.

---

## 17. FIRST COMMAND FOR CLAUDE CODE
```
Read MASTER_PLAN.md and CLAUDE.md. Start PHASE 0.
Create the solution exactly as in Section 4, add the packages from Section 3 as needed per project,
write PROGRESS.md, and finish Phase 0 through GATE 0. Run dotnet build and dotnet test.
Then stop and report before starting Phase 1.
```
