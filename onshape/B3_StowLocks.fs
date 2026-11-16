FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Stow Locks, the parts on the panel that hold the folded stair and the
 * stowed hook-on step for travel:
 *   Stow bracket (1): 1/8" sheet-metal part with two bends. Its base is the
 *   travel lock's shelf, 17" up: 2 1/4" deep behind the hook plate, with a
 *   1 1/8" tongue out to under where the rail's foot flange lands when
 *   folded. The panel leg is bent down along the shelf's panel edge and takes
 *   three #8 screws. The upper hook plate is bent up along the shelf's bed-end
 *   edge; its slot, open to the room, takes the stowed step's upper pin. A
 *   notch in the shelf's corner keeps the two bends apart. The spring plunger
 *   (McMaster 8501A58) points up through the tongue into the hole in the foot
 *   flange.
 *   Plunger jam nut (1): M10 thin jam nut on the shelf; a regular nut under
 *   the shelf clamps the plunger in its plain hole.
 *   Stow hook, lower (1): 1/8" sheet-metal angle, one bend. Its plate takes
 *   the stowed step's lower pin in an open-top slot.
 * Not modeled: the #8 screws and the regular nut under the shelf.
 */
annotation { "Feature Type Name" : "B3 Stow Locks" }
export const b3StowLocks = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        // Stow bracket: shelf (base), panel leg bent down, hook plate bent up.
        const lx0 = LOCK_LEDGE_X0;
        const lx1 = LOCK_LEDGE_X0 + LOCK_LEDGE_W;
        const shelfTop = LOCK_SHELF_TOP_Z;
        const hx0 = STOW_HOOK_X0;
        const hx1 = STOW_HOOK_X0 + SHEET_T;
        const xn = STOW_NOTCH_X;
        const yh = STOW_UP_Y0;
        const z0 = STOW_BRACKET_Z0;
        const sr = STOW_SHELF_R;
        const shelf = b3Rounded([
                    vector(xn, 0 * inch),
                    vector(lx1, 0 * inch),
                    vector(lx1, LOCK_LEDGE_REACH),
                    vector(lx0, LOCK_LEDGE_REACH),
                    vector(lx0, STOW_UP_D),
                    vector(hx0, STOW_UP_D),
                    vector(hx0, yh),
                    vector(xn, yh)
                ], [0, 0, sr, sr, sr, 0, 0, 0]);
        b3PrismXY(context, id + "bracket" + "shelf", shelf, [b3Hole(LOCK_X, LOCK_FOLD_Y, LOCK_SHELF_HOLE)], shelfTop - SHEET_T, shelfTop);
        var screws = [];
        for (var k = 0; k < size(STOW_BRACKET_SCREW_X); k += 1)
        {
            screws = append(screws, b3Hole(STOW_BRACKET_SCREW_X[k], STOW_BRACKET_SCREW_Z, STOW_SCREW_DIA));
        }
        b3PrismXZ(context, id + "bracket" + "leg", b3Rounded(b3Rect(xn, z0, lx1, shelfTop), [sr, sr, 0, 0]), screws, 0 * inch, SHEET_T);
        // The pin seats on the slot's round end.
        b3PrismYZ(context, id + "bracket" + "hook", b3SideSlottedPlate(yh, STOW_UP_D, shelfTop - SHEET_T, STOW_UP_Z1, STOW_PIN_UP_Z, STOW_PIN_UP_Y),
            [], hx0, hx1);
        b3Union(context, id + "bracket" + "join", b3Solids(id + "bracket"));
        // Sheet metal, one side of the sheet: the shelf's top, the leg's face on the panel,
        // and the hook plate's face toward the cabinet front (the inside of its bend).
        b3SheetMetal(context, id + "bracket" + "sm", b3Solids(id + "bracket"), [
                    vector(xn + 0.25 * inch, 1 * inch, shelfTop),
                    vector(xn + 0.75 * inch, 0 * inch, z0 + 0.25 * inch),
                    vector(hx1, (yh + STOW_PIN_UP_Y) / 2, shelfTop + 0.25 * inch)
                ], SHEET_T, BEND_R_SHEET, K_SHEET);
        b3Finish(context, b3Solids(id + "bracket"), "Stow bracket", RGB_ALU_DARK);
        b3Mc(context, id + "mcBracket", b3Solids(id + "bracket"), "Stow bracket", "Cabinet", b3StowBracketJoint());

        // Thin jam nut on the shelf, drawn round at its across-flats size.
        const pj = b3LockPlungerJoint().origin;
        b3Cyl(context, id + "nut", vector(pj[0], pj[1], shelfTop), vector(pj[0], pj[1], LOCK_BLOCK_TOP_Z), LOCK_NUT_AF);
        b3Cyl(context, id + "nutBore", vector(pj[0], pj[1], shelfTop - 1 * millimeter), vector(pj[0], pj[1], LOCK_BLOCK_TOP_Z + 1 * millimeter), PLUNGER_TAP);
        b3Subtract(context, id + "nutCut", b3Solids(id + "nut"), b3Solids(id + "nutBore"));
        b3FinishSteel(context, b3Solids(id + "nut"), "Plunger jam nut");
        b3Mc(context, id + "mcNut", b3Solids(id + "nut"), "Plunger jam nut", "Ledge", b3LockPlungerJoint());

        // Lower hook: panel leg (base) and the slotted plate bent out at its bed end.
        const lz0 = STOW_LO_Z0;
        const lz1 = STOW_LO_Z1;
        b3PrismYZ(context, id + "hookLo" + "plate", b3SlottedPlate(STOW_LO_D, lz0, lz1, STOW_PIN_LO_Y, STOW_PIN_LO_Z - STOW_SLOT_DROP),
            [], hx0, hx1);
        b3PrismXZ(context, id + "hookLo" + "leg", b3Rounded(b3Rect(hx0, lz0, hx1 + STOW_HOOK_LEG, lz1), [0, sr, sr, 0]),
            [b3Hole(STOW_LO_SCREW_X[0], (lz0 + lz1) / 2, STOW_SCREW_DIA), b3Hole(STOW_LO_SCREW_X[1], (lz0 + lz1) / 2, STOW_SCREW_DIA)],
            0 * inch, SHEET_T);
        b3Union(context, id + "hookLo" + "join", b3Solids(id + "hookLo"));
        // Sheet metal: the leg's face on the panel, then the plate's face toward the bed end.
        b3SheetMetal(context, id + "hookLo" + "sm", b3Solids(id + "hookLo"), [
                    vector(hx1 + STOW_HOOK_LEG / 2, 0 * inch, lz0 + 0.25 * inch),
                    vector(hx0, 0.75 * inch, lz0 + 0.25 * inch)
                ], SHEET_T, BEND_R_SHEET, K_SHEET);
        b3Finish(context, b3Solids(id + "hookLo"), "Stow hook, lower", RGB_ALU_DARK);
        b3Mc(context, id + "mcHookLo", b3Solids(id + "hookLo"), "Stow hook, lower", "Cabinet", b3StowHookJoint());
        b3Mc(context, id + "mcHookPin", b3Solids(id + "hookLo"), "Stow hook, lower", "Pin", b3StowPinJoint());
    });
