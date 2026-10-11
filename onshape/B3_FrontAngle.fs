FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Front Angle, 1/8" sheet, on the cabinet's front post. The panel leg
 * takes three screws into the post and stops at 16 1/2" to clear the middle
 * bracket. The outward leg is flush with the front edge and carries the
 * keyhole for the hook-on step's cabinet-side pin. Not modeled: the keeper
 * holes (drill to suit the latch) and the bend relief.
 */
annotation { "Feature Type Name" : "B3 Front Angle" }
export const b3FrontAngle = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        var screws = [];
        for (var i = 0; i < size(FA_SCREW_Z); i += 1)
        {
            screws = append(screws, b3Hole(FA_SCREW_X, FA_SCREW_Z[i], FA_SCREW_HOLE_DIA));
        }
        b3PrismXZ(context, id + "panelLeg", b3Rect(FA_PANEL_LEG_X0, FA_BOTTOM_Z, FA_X1, FA_PANEL_LEG_TOP), screws, 0 * inch, SHEET_T);

        b3PrismYZ(context, id + "outLeg", b3Rect(0 * inch, FA_BOTTOM_Z, FA_OUT_LEG, FA_TOP_Z),
            [b3Keyhole(FA_KEYHOLE_Y, FA_KEYHOLE_Z, FA_KEYHOLE_END_Z)], FA_X1 - SHEET_T, FA_X1);

        b3Union(context, id + "join", b3Solids(id));
        b3Finish(context, b3Solids(id), "Front angle", RGB_ALU_DARK);
    });
