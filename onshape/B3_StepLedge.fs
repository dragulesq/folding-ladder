FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Step Ledge: short 1/8" angle on the panel just above the floor at the
 * bed end. The folded hook-on step stands on end on its shelf.
 */
annotation { "Feature Type Name" : "B3 Step Ledge" }
export const b3StepLedge = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const y0 = STAIR_Y0;
        const y1 = STAIR_Y0 + LEDGE_LEN;
        const over = 0.1 * inch;

        b3Box(context, id + "shelf", vector(0 * inch, y0, LEDGE_Z), vector(LEDGE_SHELF, y1, LEDGE_Z + SHEET_T));
        b3Box(context, id + "leg", vector(0 * inch, y0, LEDGE_Z), vector(SHEET_T, y1, LEDGE_Z + LEDGE_LEG_H));
        b3Union(context, id + "join", b3Solids(id));

        // Two screws into the Plyboo.
        const zs = LEDGE_Z + (SHEET_T + LEDGE_LEG_H) / 2;
        b3CylX(context, id + "screw1", y0 + 1.5 * inch, zs, -over, SHEET_T + over, (5 / 32) * inch / 2);
        b3CylX(context, id + "screw2", y1 - 1.5 * inch, zs, -over, SHEET_T + over, (5 / 32) * inch / 2);

        const ledge = qUnion([b3Solids(id + "shelf"), b3Solids(id + "leg")]);
        b3Subtract(context, id + "holes", ledge, qSubtraction(b3Solids(id), ledge));
        b3Finish(context, b3Solids(id), "Step ledge", RGB_ALU_DARK);
    });
