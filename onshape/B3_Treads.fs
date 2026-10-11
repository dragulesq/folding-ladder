FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Tread, 1/8" sheet: 18 x 5 5/8 skin with 2" lips bent down at the x
 * ends, bushing holes through both lips for the cabinet and outer axles,
 * and a 1/2" radius on each lip's bottom outer corner to clear the rail.
 * The stair uses it twice; it is built once, where the top tread sits, and
 * the assembly places both.
 */
annotation { "Feature Type Name" : "B3 Tread" }
export const b3Treads = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const n = TREAD_TOP;
        const top = b3T(n);
        const holes = [
                b3Hole(CAB_AXLE_Y, top - CAB_AXLE_DROP, HOLE_DIA),
                b3Hole(OUT_AXLE_Y, top - OUT_AXLE_DROP, HOLE_DIA)
            ];
        b3Tray(context, id + "tray", b3GoingX(n) + TRAY_INSET, top, holes, LIP_CORNER_R);

        const body = b3Solids(id + "tray");
        b3Finish(context, body, "Tread", RGB_ALU);
        b3Mc(context, id + "mcCab", body, "Tread", "Cabinet axle", b3CabAxleJoint(n));
        b3Mc(context, id + "mcOut", body, "Tread", "Outer axle", b3OutAxleJoint(n));
    });
