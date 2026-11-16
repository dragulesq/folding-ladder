FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Stow Locks, the parts on the panel that hold the folded stair and the
 * stowed hook-on step for travel:
 *   Rail lock ledge (1): 3/16" angle under where the rail's foot flange lands
 *   when folded. The spring plunger (McMaster 8501A58) points up through its
 *   shelf and the tapped block on it, into the hole in the foot flange.
 *   Plunger block (1): 3/16" aluminum on the shelf, tapped M10.
 *   Stow hook, upper and lower (1 each): 1/8" angles whose plates stand where
 *   the stowed step's two pins point. The lower pin drops into an open-top
 *   slot; the upper pin slides into a slot open toward the room as the step
 *   is tipped in to the panel.
 *   Stow tab (1): 1/8" turn tab on the upper hook, turned across the slot's mouth.
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
        // Rail lock ledge: shelf out from the panel, leg down the panel.
        const lx0 = LOCK_LEDGE_X0;
        const lx1 = LOCK_LEDGE_X0 + LOCK_LEDGE_W;
        const shelfTop = LOCK_SHELF_TOP_Z;
        b3PrismXY(context, id + "ledge" + "shelf", b3Rounded(b3Rect(lx0, 0 * inch, lx1, LOCK_LEDGE_REACH), [0, 0, R_OUTSIDE_SMALL, R_OUTSIDE_SMALL]),
            [b3Hole(LOCK_X, LOCK_FOLD_Y, LOCK_SHELF_HOLE)], shelfTop - PLATE_T, shelfTop);
        b3PrismXZ(context, id + "ledge" + "leg", b3Rect(lx0, shelfTop - LOCK_LEDGE_LEG, lx1, shelfTop),
            [b3Hole(lx0 + (3 / 8) * inch, shelfTop - LOCK_SCREW_DROP, STOW_SCREW_DIA), b3Hole(lx1 - (3 / 8) * inch, shelfTop - LOCK_SCREW_DROP, STOW_SCREW_DIA)],
            0 * inch, PLATE_T);
        b3Union(context, id + "ledgeJoin", b3Solids(id + "ledge"));
        b3Finish(context, b3Solids(id + "ledge"), "Rail lock ledge", RGB_ALU_DARK);
        b3Mc(context, id + "mcLedge", b3Solids(id + "ledge"), "Rail lock ledge", "Cabinet", b3LockLedgeJoint());

        // Plunger block on the shelf, tapped.
        const pj = b3LockPlungerJoint().origin;
        b3Box(context, id + "block", vector(pj[0] - LOCK_BLOCK_W / 2, pj[1] - LOCK_BLOCK_D / 2, shelfTop),
            vector(pj[0] + LOCK_BLOCK_W / 2, pj[1] + LOCK_BLOCK_D / 2, LOCK_BLOCK_TOP_Z));
        b3Cyl(context, id + "tap", vector(pj[0], pj[1], shelfTop - 1 * millimeter), vector(pj[0], pj[1], LOCK_BLOCK_TOP_Z + 1 * millimeter), PLUNGER_TAP);
        b3Subtract(context, id + "tapCut", b3Solids(id + "block"), b3Solids(id + "tap"));
        b3Finish(context, b3Solids(id + "block"), "Plunger block", RGB_ALU_DARK);
        b3Mc(context, id + "mcBlock", b3Solids(id + "block"), "Plunger block", "Ledge", b3LockPlungerJoint());

        // Hook plates for the stowed step's pins.
        const hx0 = STOW_HOOK_X0;
        const hx1 = STOW_HOOK_X0 + SHEET_T;
        b3PrismYZ(context, id + "hookUp" + "plate", b3SideSlottedPlate(STOW_UP_D, STOW_UP_Z0, STOW_UP_Z1, STOW_PIN_UP_Z, STOW_PIN_UP_Y - STOW_SLOT_DROP),
            [b3Hole((STOW_TAB_Y0 + STOW_TAB_Y1) / 2, STOW_PIN_UP_Z + STOW_TAB_PIVOT_UP, 0.14 * inch)], hx0, hx1);
        b3PrismXZ(context, id + "hookUp" + "leg", b3Rect(hx0, STOW_UP_Z0, hx1 + STOW_HOOK_LEG, STOW_UP_LEG_Z1),
            [b3Hole(hx1 + STOW_HOOK_LEG - (1 / 2) * inch, (STOW_UP_Z0 + STOW_UP_LEG_Z1) / 2, STOW_SCREW_DIA),
                b3Hole(hx1 + (3 / 8) * inch, (STOW_UP_Z0 + STOW_UP_LEG_Z1) / 2, STOW_SCREW_DIA)], 0 * inch, SHEET_T);
        b3Union(context, id + "hookUpJoin", b3Solids(id + "hookUp"));
        b3Finish(context, b3Solids(id + "hookUp"), "Stow hook, upper", RGB_ALU_DARK);
        b3Mc(context, id + "mcHookUp", b3Solids(id + "hookUp"), "Stow hook, upper", "Cabinet", b3StowHookJoint(true));

        b3PrismYZ(context, id + "hookLo" + "plate", b3SlottedPlate(STOW_LO_D, STOW_LO_Z0, STOW_LO_Z1, STOW_PIN_LO_Y, STOW_PIN_LO_Z - STOW_SLOT_DROP),
            [], hx0, hx1);
        b3PrismXZ(context, id + "hookLo" + "leg", b3Rect(hx0, STOW_LO_Z0, hx1 + STOW_HOOK_LEG, STOW_LO_Z1),
            [b3Hole(hx1 + STOW_HOOK_LEG - (1 / 2) * inch, (STOW_LO_Z0 + STOW_LO_Z1) / 2, STOW_SCREW_DIA),
                b3Hole(hx1 + (3 / 8) * inch, (STOW_LO_Z0 + STOW_LO_Z1) / 2, STOW_SCREW_DIA)], 0 * inch, SHEET_T);
        b3Union(context, id + "hookLoJoin", b3Solids(id + "hookLo"));
        b3Finish(context, b3Solids(id + "hookLo"), "Stow hook, lower", RGB_ALU_DARK);
        b3Mc(context, id + "mcHookLo", b3Solids(id + "hookLo"), "Stow hook, lower", "Cabinet", b3StowHookJoint(false));

        // Turn tab, drawn closed: upright across the upper slot's mouth, on the plate's bed-side face.
        b3Box(context, id + "tab", vector(hx0 - SHEET_T, STOW_TAB_Y0, STOW_PIN_UP_Z - STOW_TAB_HALF), vector(hx0, STOW_TAB_Y1, STOW_PIN_UP_Z + STOW_TAB_HALF));
        b3Finish(context, b3Solids(id + "tab"), "Stow tab", RGB_STEEL);
        b3Mc(context, id + "mcTab", b3Solids(id + "tab"), "Stow tab", "Hook", b3StowTabJoint());
    });
