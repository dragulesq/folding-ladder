FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Step Ledge, 1/8" sheet: 6" angle on the panel at the bed end. The
 * folded hook-on step stands on end on its 2 1/2" shelf. Two #8 screws
 * through the back leg (countersinks not modeled).
 */
annotation { "Feature Type Name" : "B3 Step Ledge" }
export const b3StepLedge = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const top = LEDGE_TOP_Z;
        b3Box(context, id + "shelf", vector(0 * inch, 0 * inch, top - SHEET_T), vector(LEDGE_LEN, LEDGE_SHELF, top));

        var screws = [];
        for (var i = 0; i < size(LEDGE_SCREW_X); i += 1)
        {
            screws = append(screws, b3Hole(LEDGE_SCREW_X[i], LEDGE_SCREW_Z, LEDGE_SCREW_DIA));
        }
        b3PrismXZ(context, id + "leg", b3Rect(0 * inch, top - LEDGE_LEG_H, LEDGE_LEN, top), screws, 0 * inch, SHEET_T);

        b3Union(context, id + "join", b3Solids(id));
        b3Finish(context, b3Solids(id), "Step ledge", RGB_ALU_DARK);
    });
