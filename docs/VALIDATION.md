# Validation report (structure and quantities)

> **Status: Phase 5 GATE NOT MET.** The plan requires a 3D frame stiffness solver validated against hand calculations and
> OpenSees/ETABS to within 2%. That solver has **not** been built. The current structure engine does preliminary
> gravity sizing only. Every structural output carries the banner
> "PRELIMINARY - MUST BE REVIEWED AND CERTIFIED BY A LICENSED STRUCTURAL ENGINEER".

## What is implemented and how it is checked

Each formula has a hand-calculated golden test in `tests/Engine.Structure.Tests/CodeFormulaTests.cs`.

| Item | Code reference | Golden case | Expected | Test |
|---|---|---|---|---|
| Tension steel, singly reinforced | IS 456 Annex G-1.1(b) | Mu 50 kNm, 230x410, M20/Fe500 | 305.18 mm² | `Ast_singly_reinforced_IS456_AnnexG_1_1b` |
| Limiting moment | IS 456 Annex G-1.1(c), cl. 38.1 | 230x410, Fe500 (xu,max = 0.46d) | 103.31 kNm | `Mu_limit_IS456_AnnexG_1_1c` |
| Minimum beam steel | IS 456 cl. 26.5.1.1 | 230x410, Fe500 | 160.31 mm² | `Min_beam_steel_IS456_26_5_1_1` |
| Short column axial | IS 456 cl. 39.3, 26.5.3.1(a) | Pu 1000 kN, 300x300 | 856.27 mm² (0.8% min = 720) | `Column_axial_IS456_39_3` |
| Concrete shear strength | IS 456 Table 19 | M20, pt 0.375% | 0.42 N/mm² | `Tau_c_Table19_M20` |
| Stirrup spacing | IS 456 cl. 40.4(a), 26.5.1.5/6 | Vu 150 kN, 230x410, 2L-8 | 154.5 mm | `Stirrup_spacing_limited_by_IS456_26_5_1_5` |
| Slab depth | IS 456 cl. 24.1 note 2, 23.2.1 | short span 3.0 m / 4.5 m | 120 / 170 mm | `Slab_thickness_IS456_24_1_and_23_2_1` |
| Two-way moment coefficient | IS 456 Table 27 | ly/lx 1.25 | 0.0885 | `Alpha_x_Table27_interpolates` |
| Tie spacing | IS 456 cl. 26.5.3.2(c) | 230 column, 12 φ | 192 mm | `Tie_spacing_IS456_26_5_3_2c` |
| Punching shear strength | IS 456 cl. 31.6.3.1 | M20, βc 0.77 | 1.118 N/mm² | `Punching_strength_IS456_31_6_3_1` |
| Footing plan size | SBC check (+10% self weight) | Pu 750 kN, SBC 150 | 2.0 m square | `Footing_size_from_SBC` |
| Seismic coefficient | IS 1893-1:2016 cl. 6.4.2, 7.6.2 | Zone III, h 6.9 m | T 0.319 s, Ah 0.04 | `Seismic_IS1893_6_4_2_and_7_6_2` |
| Wind pressure | IS 875-3:2015 cl. 6.3, 7.2 | Vb 50 m/s, TC3, h 6.9 m | 1242.15 N/m² | `Wind_IS875_3` |
| Bar weight | d²/162 | 12 φ | 0.889 kg/m | `Bar_weight_d2_over_162` |

Quantity take-off golden tests (`tests/Engine.Quantity.Tests`): a 4 x 5 m single room gives 11.90 m³ of 230 mm brickwork
(centre-line method), 87.75 m² of external plaster including the parapet, and correct deductions for openings. Cost
roll-up arithmetic (contingency, margin, GST) is checked.

## Known gaps before the structural output can be trusted

1. No frame analysis: beam moments use wL²/10 and columns use tributary area. Lateral loads are reported as base shear
   and pressure only and are **not** distributed to members.
2. No biaxial bending, slenderness or minimum-eccentricity checks for columns.
3. No deflection check beyond span/depth ratios; no crack-width checks.
4. Isolated footings only; overlapping footings are flagged for combined footings.
5. IS 13920 ductile detailing is reflected only in end-zone stirrup/tie spacing notes.

Next step (Phase 5.3–5.5): a 3D frame solver with a validation suite against textbook cases and OpenSees.
