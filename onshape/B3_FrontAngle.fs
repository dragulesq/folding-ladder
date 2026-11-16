FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Front Angle, 1/8" sheet-metal part, on the cabinet's front post: a 6"
 * angle around the keyhole. The panel leg takes two screws into the post. The
 * outward leg is flush with the cabinet's front edge and carries the keyhole
 * for the hook-on step's cabinet-side pin. Corners are rounded 1/4", and 1/2"
 * on the outward leg's top outer corner, which faces the entry.
 */
annotation { "Feature Type Name" : "B3 Front Angle" }
export const b3FrontAngle = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const r = FA_CORNER_R;
        var screws = [];
        for (var i = 0; i < size(FA_SCREW_Z); i += 1)
        {
            screws = append(screws, b3Hole(FA_SCREW_X, FA_SCREW_Z[i], FA_SCREW_HOLE_DIA));
        }
        b3PrismXZ(context, id + "panelLeg", b3Rounded(b3Rect(FA_PANEL_LEG_X0, FA_BOTTOM_Z, FA_X1, FA_TOP_Z), [r, 0, 0, r]),
            screws, 0 * inch, SHEET_T);
        b3PrismYZ(context, id + "outLeg", b3Rounded(b3Rect(0 * inch, FA_BOTTOM_Z, FA_OUT_LEG, FA_TOP_Z), [0, r, FA_ENTRY_R, 0]),
            [b3Keyhole(FA_KEYHOLE_Y, FA_KEYHOLE_Z, FA_KEYHOLE_END_Z)], FA_X1 - SHEET_T, FA_X1);
        b3Union(context, id + "join", b3Solids(id));

        // Sheet metal: the panel leg's face on the panel, then the outward leg's outer face.
        b3SheetMetal(context, id + "sm", b3Solids(id), [
                    vector((FA_PANEL_LEG_X0 + FA_SCREW_X) / 2, 0 * inch, FA_BOTTOM_Z + 0.5 * inch),
                    vector(FA_X1, FA_OUT_LEG / 2, FA_BOTTOM_Z + 0.5 * inch)
                ], SHEET_T, BEND_R_SHEET, K_SHEET);

        const body = b3Solids(id);
        const name = "Front angle";
        b3Finish(context, body, name, RGB_ALU_DARK);
        b3Mc(context, id + "mcCabinet", body, name, "Cabinet", b3FrontAngleJoint());
        b3Mc(context, id + "mcStep", body, name, "Hook-on step", b3CabAxleJoint(HOOK_STEP));
    });
