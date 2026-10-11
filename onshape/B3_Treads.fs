FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Treads: the top and middle treads. 1/8" tray with lips bent down at the
 * front and back, and pivot holes through both lips for the cabinet axle and
 * the outer axle. Follows FOLD_ANGLE.
 *
 * Still to add (see the B3 page): corner relief near the outer rail.
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
    const top = b3TreadTopZ(n);
    const yb = b3TreadBackY(n);
    const yf = yb + TREAD_GOING;
    const over = 0.1 * inch;

    b3Prism(context, id + "tray", b3FrameYZ(b3TreadX0()), b3TrayOutline(yb, top, TREAD_LIP), [], TREAD_WIDTH);

    const cabAx = b3CabAxle(n);
    const outAx = b3OutAxle(n);
    b3CylY(context, id + "cabHole", cabAx[0], cabAx[2], yb - over, yf + over, BUSHING_OD / 2);
    b3CylY(context, id + "outHole", outAx[0], outAx[2], yb - over, yf + over, BUSHING_OD / 2);
    b3Subtract(context, id + "holes", b3Solids(id + "tray"), qUnion([b3Solids(id + "cabHole"), b3Solids(id + "outHole")]));

    const body = b3Solids(id + "tray");
    opTransform(context, id + "fold", { "bodies" : body, "transform" : b3TreadFold(n) });
    b3Finish(context, body, partName, RGB_ALU);
}
