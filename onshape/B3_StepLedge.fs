FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Step Ledge, 1/8" sheet-metal part with two bends, on the panel at the
 * bed end: the ledge the folded hook-on step stands on and the lower stow
 * hook in one. Its base is the back wall on the panel, 5 7/8 long, from the
 * shelf's underside up to 2 5/8, with three M6 button-head screws behind the
 * stowed step. The 2 1/2" shelf is bent out along the wall's bottom edge. The
 * hook plate is bent out along the wall's front end; its open-top slot takes
 * the stowed step's lower pin. Nothing of the part is below the shelf or in
 * front of the hook plate. Where the shelf's end meets the plate, Onshape
 * cuts a round corner relief. The shelf's free corner, at ankle height, is
 * rounded 1/2".
 */
annotation { "Feature Type Name" : "B3 Step Ledge" }
export const b3StepLedge = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const top = LEDGE_TOP_Z;
        const z0 = top - SHEET_T;
        const z1 = STOW_LO_Z1;
        const hx0 = STOW_HOOK_X0;
        const hx1 = STOW_HOOK_X0 + SHEET_T;

        var screws = [];
        for (var i = 0; i < size(LEDGE_SCREW_X); i += 1)
        {
            screws = append(screws, b3Hole(LEDGE_SCREW_X[i], LEDGE_SCREW_Z, LEDGE_SCREW_DIA));
        }
        // Back wall on the panel.
        b3PrismXZ(context, id + "wall", b3Rounded(b3Rect(0 * inch, z0, hx1, z1), [0, 0, 0, LEDGE_LEG_R]), screws, 0 * inch, SHEET_T);
        // Shelf, up to the hook plate.
        b3PrismXY(context, id + "shelf", b3Rounded(b3Rect(0 * inch, 0 * inch, LEDGE_LEN, LEDGE_SHELF), [0, 0, 0, LEDGE_SHELF_R]), [], z0, top);
        // Hook plate, its slot's round end a little below the resting pin.
        b3PrismYZ(context, id + "plate", b3SlottedPlate(STOW_LO_D, z0, z1, STOW_PIN_LO_Y, STOW_PIN_LO_Z - STOW_SLOT_DROP), [], hx0, hx1);
        b3Union(context, id + "join", b3Solids(id));

        // Sheet metal: the wall's face on the panel, then the shelf's underside and the
        // plate's face toward the cabinet front.
        b3SheetMetal(context, id + "sm", b3Solids(id), [
                    vector(2 * inch, 0 * inch, z0 + 0.6 * inch),
                    vector(2 * inch, LEDGE_SHELF / 2, z0),
                    vector(hx1, 0.75 * inch, z0 + 0.5 * inch)
                ], SHEET_T, BEND_R_SHEET, K_SHEET);

        const body = b3Solids(id);
        b3Finish(context, body, "Step ledge", RGB_ALU_DARK);
        b3Mc(context, id + "mcCabinet", body, "Step ledge", "Cabinet", b3StepLedgeJoint());
        b3Mc(context, id + "mcStrip", body, "Step ledge", "Rubber strip", b3LedgeStripJoint());
        b3Mc(context, id + "mcPin", body, "Step ledge", "Pin", b3StowPinJoint());
    });
