FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Stow Locks, the parts that hold the folded stair and the stowed hook-on
 * step for travel:
 *   Stow bracket (1): 1/8" sheet on the panel. One leg along the panel, bent
 *   out at its bed end into the upper hook plate (a slot open to the room
 *   takes the stowed step's upper pin) and along its top into the travel
 *   lock's shelf, under where the rail's foot flange lands when folded, with
 *   a flange down each side of the shelf. The spring plunger (McMaster
 *   8501A58) points up through the shelf into the hole in the foot flange.
 *   Plunger jam nut (1): M10 thin jam nut on the shelf; a regular nut under
 *   the shelf clamps the plunger in its plain hole.
 *   Stow hook, lower (1): 1/8" angle whose plate takes the stowed step's
 *   lower pin in an open-top slot.
 *   Step keeper (1): 3/16 x 3/4 flat bar bolted inside the rail's plate. The
 *   step is stowed before the stair is folded; folded, the keeper stands in
 *   front of the step's top, so the plunger locks the step too.
 * Modeled as plain solids, not sheet metal. Not modeled: the block's two
 * screws, the jam nut under the shelf, the tab's pivot screw (countersunk
 * from the cabinet-front face, nyloc nut on the tab side).
 */
annotation { "Feature Type Name" : "B3 Stow Locks" }
export const b3StowLocks = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        // Stow bracket, 1/8" sheet: one panel leg from the upper hook plate (bent out at its
        // bed end) to the travel lock's shelf (bent out along its top, with a flange down
        // each side of the shelf).
        const lx0 = LOCK_LEDGE_X0;
        const lx1 = LOCK_LEDGE_X0 + LOCK_LEDGE_W;
        const shelfTop = LOCK_SHELF_TOP_Z;
        const hx0 = STOW_HOOK_X0;
        const hx1 = STOW_HOOK_X0 + SHEET_T;
        const z0 = STOW_BRACKET_Z0;
        var screws = [];
        for (var k = 0; k < size(STOW_BRACKET_SCREW_X); k += 1)
        {
            screws = append(screws, b3Hole(STOW_BRACKET_SCREW_X[k], STOW_BRACKET_SCREW_Z, STOW_SCREW_DIA));
        }
        b3PrismXZ(context, id + "bracket" + "leg", b3Rect(hx0, z0, lx1, shelfTop), screws, 0 * inch, SHEET_T);
        b3PrismYZ(context, id + "bracket" + "hook", b3SideSlottedPlate(STOW_UP_D, z0, STOW_UP_Z1, STOW_PIN_UP_Z, STOW_PIN_UP_Y - STOW_SLOT_DROP),
            [], hx0, hx1);
        b3PrismXY(context, id + "bracket" + "shelf", b3Rounded(b3Rect(lx0, 0 * inch, lx1, LOCK_LEDGE_REACH), [0, 0, R_OUTSIDE_SMALL, R_OUTSIDE_SMALL]),
            [b3Hole(LOCK_X, LOCK_FOLD_Y, LOCK_SHELF_HOLE)], shelfTop - SHEET_T, shelfTop);
        for (var side in [0, 1])
        {
            const fx0 = side == 0 ? lx0 : lx1 - SHEET_T;
            b3PrismYZ(context, id + "bracket" + ("flange" ~ side), b3Rect(SHEET_T, shelfTop - SHEET_T - LOCK_SHELF_FLANGE, LOCK_LEDGE_REACH - R_OUTSIDE_SMALL, shelfTop - SHEET_T),
                [], fx0, fx0 + SHEET_T);
        }
        b3Union(context, id + "bracketJoin", b3Solids(id + "bracket"));
        b3Finish(context, b3Solids(id + "bracket"), "Stow bracket", RGB_ALU_DARK);
        b3Mc(context, id + "mcBracket", b3Solids(id + "bracket"), "Stow bracket", "Cabinet", b3StowBracketJoint());

        // Thin jam nut on the shelf, drawn round at its across-flats size.
        const pj = b3LockPlungerJoint().origin;
        b3Cyl(context, id + "nut", vector(pj[0], pj[1], shelfTop), vector(pj[0], pj[1], LOCK_BLOCK_TOP_Z), LOCK_NUT_AF);
        b3Cyl(context, id + "nutBore", vector(pj[0], pj[1], shelfTop - 1 * millimeter), vector(pj[0], pj[1], LOCK_BLOCK_TOP_Z + 1 * millimeter), PLUNGER_TAP);
        b3Subtract(context, id + "nutCut", b3Solids(id + "nut"), b3Solids(id + "nutBore"));
        b3FinishSteel(context, b3Solids(id + "nut"), "Plunger jam nut");
        b3Mc(context, id + "mcNut", b3Solids(id + "nut"), "Plunger jam nut", "Ledge", b3LockPlungerJoint());

        // Lower hook plate for the stowed step's lower pin.
        b3PrismYZ(context, id + "hookLo" + "plate", b3SlottedPlate(STOW_LO_D, STOW_LO_Z0, STOW_LO_Z1, STOW_PIN_LO_Y, STOW_PIN_LO_Z - STOW_SLOT_DROP),
            [], hx0, hx1);
        b3PrismXZ(context, id + "hookLo" + "leg", b3Rect(hx0, STOW_LO_Z0, hx1 + STOW_HOOK_LEG, STOW_LO_Z1),
            [b3Hole(hx1 + STOW_HOOK_LEG - (1 / 2) * inch, (STOW_LO_Z0 + STOW_LO_Z1) / 2, STOW_SCREW_DIA),
                b3Hole(hx1 + (3 / 8) * inch, (STOW_LO_Z0 + STOW_LO_Z1) / 2, STOW_SCREW_DIA)], 0 * inch, SHEET_T);
        b3Union(context, id + "hookLoJoin", b3Solids(id + "hookLo"));
        b3Finish(context, b3Solids(id + "hookLo"), "Stow hook, lower", RGB_ALU_DARK);
        b3Mc(context, id + "mcHookLo", b3Solids(id + "hookLo"), "Stow hook, lower", "Cabinet", b3StowHookJoint());
        b3Mc(context, id + "mcHookPin", b3Solids(id + "hookLo"), "Stow hook, lower", "Pin", b3StowPinJoint());

        // Step keeper: flat bar bolted inside the rail's plate, hanging below its lower edge.
        b3Box(context, id + "keeper", vector(KEEP_X0, RAIL_Y - KEEP_T, KEEP_Z0), vector(KEEP_X0 + KEEP_W, RAIL_Y, KEEP_Z1));
        b3Finish(context, b3Solids(id + "keeper"), "Step keeper", RGB_ALU_DARK);
        b3Mc(context, id + "mcKeeper", b3Solids(id + "keeper"), "Step keeper", "Rail", b3KeeperJoint());
    });
