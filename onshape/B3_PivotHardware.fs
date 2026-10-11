FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Pivot Hardware: the four 5/16" tread axles, the two hook-on step axles
 * (their back ends are the hook pins) and the four 3/16" stop pins, for
 * checking fit. Bushings, washers and E-clips aren't modeled. The outer
 * tread axles follow FOLD_ANGLE.
 */
annotation { "Feature Type Name" : "B3 Pivot Hardware" }
export const b3PivotHardware = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        b3TreadHardware(context, id + "top", TREAD_TOP, "top");
        b3TreadHardware(context, id + "middle", TREAD_MIDDLE, "middle");

        b3CylX(context, id + "stepCab", STEP_CAB_AXLE_Y, STEP_CAB_AXLE_Z, STEP_AXLE_X0, STEP_AXLE_X1, AXLE_DIA);
        b3FinishSteel(context, b3Solids(id + "stepCab"), "Axle, step, cabinet side");
        b3CylX(context, id + "stepOut", STEP_OUT_AXLE_Y, STEP_OUT_AXLE_Z, STEP_AXLE_X0, STEP_AXLE_X1, AXLE_DIA);
        b3FinishSteel(context, b3Solids(id + "stepOut"), "Axle, step, outer");
    });

function b3TreadHardware(context is Context, id is Id, n is number, label is string)
{
    const top = b3T(n);
    const x0 = b3GoingX(n) + TREAD_AXLE_X0;
    const x1 = x0 + TREAD_AXLE_LEN;

    b3CylX(context, id + "cabAxle", CAB_AXLE_Y, top - CAB_AXLE_DROP, x0, x1, AXLE_DIA);
    b3FinishSteel(context, b3Solids(id + "cabAxle"), "Axle, cabinet, " ~ label);

    b3CylX(context, id + "outAxle", OUT_AXLE_Y, top - OUT_AXLE_DROP, x0, x1, AXLE_DIA);
    opTransform(context, id + "fold", { "bodies" : b3Solids(id + "outAxle"), "transform" : b3RailFold() });
    b3FinishSteel(context, b3Solids(id + "outAxle"), "Axle, outer, " ~ label);

    // Stop pins: flush with each bracket ear's inside face, sticking out under the lip.
    const ranges = b3EarRanges(n);
    const pz = top - STOP_PIN_DROP;
    b3CylX(context, id + "pinBack", STOP_PIN_Y, pz, ranges[0][0] - STOP_PIN_OUT, ranges[0][1], STOP_PIN_DIA);
    b3CylX(context, id + "pinFront", STOP_PIN_Y, pz, ranges[1][0], ranges[1][1] + STOP_PIN_OUT, STOP_PIN_DIA);
    b3FinishSteel(context, b3Solids(id + "pinBack"), "Stop pin, back, " ~ label);
    b3FinishSteel(context, b3Solids(id + "pinFront"), "Stop pin, front, " ~ label);
}
