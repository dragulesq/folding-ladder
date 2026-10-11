FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Treads (2), 1/8" sheet: 18 x 5 5/8 skin with 2" lips bent down at the
 * x ends, bushing holes through both lips for the cabinet and outer axles,
 * and a 1/2" radius on each lip's bottom outer corner to clear the rail.
 * Follows FOLD_ANGLE.
 */
annotation { "Feature Type Name" : "B3 Treads" }
export const b3Treads = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        b3Tread(context, id + "top", TREAD_TOP, "Top tread");
        b3Tread(context, id + "middle", TREAD_MIDDLE, "Middle tread");
    });

function b3Tread(context is Context, id is Id, n is number, partName is string)
{
    const top = b3T(n);
    const holes = [
            b3Hole(CAB_AXLE_Y, top - CAB_AXLE_DROP, HOLE_DIA),
            b3Hole(OUT_AXLE_Y, top - OUT_AXLE_DROP, HOLE_DIA)
        ];
    b3Tray(context, id, b3GoingX(n) + TRAY_INSET, top, holes, LIP_CORNER_R);

    const body = b3Solids(id);
    opTransform(context, id + "fold", { "bodies" : body, "transform" : b3TreadFold(n) });
    b3Finish(context, body, partName, RGB_ALU);
}
