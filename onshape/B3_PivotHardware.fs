FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Pivot Hardware: the four 5/16" stainless axles and the four 3/16" level
 * stop pins, for checking fit. Bushings and washers aren't modeled. The
 * outer axles follow FOLD_ANGLE.
 */
annotation { "Feature Type Name" : "B3 Pivot Hardware" }
export const b3PivotHardware = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        b3TreadHardware(context, id + "top", TREAD_TOP, "top");
        b3TreadHardware(context, id + "middle", TREAD_MIDDLE, "middle");
    });

function b3TreadHardware(context is Context, id is Id, n is number, label is string)
{
    const yb = b3TreadBackY(n);
    const yf = yb + TREAD_GOING;
    const ym = b3TreadMidY(n);
    const cabAx = b3CabAxle(n);
    const outAx = b3OutAxle(n);

    b3CylY(context, id + "cabAxle", cabAx[0], cabAx[2], ym - AXLE_LEN / 2, ym + AXLE_LEN / 2, AXLE_DIA / 2);
    b3FinishSteel(context, b3Solids(id + "cabAxle"), "Axle, cabinet, " ~ label);

    b3CylY(context, id + "outAxle", outAx[0], outAx[2], ym - AXLE_LEN / 2, ym + AXLE_LEN / 2, AXLE_DIA / 2);
    opTransform(context, id + "fold", { "bodies" : b3Solids(id + "outAxle"), "transform" : b3RailFold() });
    b3FinishSteel(context, b3Solids(id + "outAxle"), "Axle, outer, " ~ label);

    // Stop pins: from the lip's outer face through the bracket ear.
    const px = cabAx[0] + STOP_PIN_OUT;
    const pz = b3TreadTopZ(n) - STOP_PIN_DROP;
    b3CylY(context, id + "pinBack", px, pz, yb, yb + EAR_INSET + PLATE_T, STOP_PIN_DIA / 2);
    b3CylY(context, id + "pinFront", px, pz, yf - EAR_INSET - PLATE_T, yf, STOP_PIN_DIA / 2);
    b3FinishSteel(context, b3Solids(id + "pinBack"), "Stop pin, back, " ~ label);
    b3FinishSteel(context, b3Solids(id + "pinFront"), "Stop pin, front, " ~ label);
}
