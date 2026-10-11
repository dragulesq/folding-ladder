FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Step Ledge, 1/8" sheet-metal part: 6" angle on the panel at the bed
 * end. The folded hook-on step stands on end on its 2 1/2" shelf. Two #8
 * screws through the back leg (countersinks not modeled). The shelf's front
 * corners, at ankle height, are rounded 1/2"; the back leg's bottom corners
 * 1/4".
 */
annotation { "Feature Type Name" : "B3 Step Ledge" }
export const b3StepLedge = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const top = LEDGE_TOP_Z;
        const shelf = b3Rounded(b3Rect(0 * inch, 0 * inch, LEDGE_LEN, LEDGE_SHELF), [0, 0, LEDGE_SHELF_R, LEDGE_SHELF_R]);
        b3PrismXY(context, id + "shelf", shelf, [], top - SHEET_T, top);

        var screws = [];
        for (var i = 0; i < size(LEDGE_SCREW_X); i += 1)
        {
            screws = append(screws, b3Hole(LEDGE_SCREW_X[i], LEDGE_SCREW_Z, LEDGE_SCREW_DIA));
        }
        const leg = b3Rounded(b3Rect(0 * inch, top - LEDGE_LEG_H, LEDGE_LEN, top), [LEDGE_LEG_R, LEDGE_LEG_R, 0, 0]);
        b3PrismXZ(context, id + "leg", leg, screws, 0 * inch, SHEET_T);
        b3Union(context, id + "join", b3Solids(id));

        // Sheet metal: the back leg's face on the panel, then the shelf's top.
        b3SheetMetal(context, id + "sm", b3Solids(id), [
                    vector(LEDGE_LEN / 2, 0 * inch, LEDGE_SCREW_Z),
                    vector(LEDGE_LEN / 2, LEDGE_SHELF / 2, top)
                ], SHEET_T, BEND_R_SHEET, K_SHEET);

        const body = b3Solids(id);
        b3Finish(context, body, "Step ledge", RGB_ALU_DARK);
        b3Mc(context, id + "mcCabinet", body, "Step ledge", "Cabinet", b3StepLedgeJoint());
    });
