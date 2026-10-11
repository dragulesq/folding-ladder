FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Pivot Hardware: the four tread axles and the two hook-on step axles
 * (their back ends are the hook pins), all 12 mm aluminum tube with a 5 mm
 * bore, each end capped by an M6 fender washer and button-head screw, plus
 * the four 3/16" stop pins, for checking fit and folded clearances.
 * Bushings, thrust washers and wave washers aren't modeled. The outer tread
 * axles and their screws follow FOLD_ANGLE.
 */
annotation { "Feature Type Name" : "B3 Pivot Hardware" }
export const b3PivotHardware = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        b3TreadHardware(context, id + "top", TREAD_TOP, "top");
        b3TreadHardware(context, id + "middle", TREAD_MIDDLE, "middle");

        b3Axle(context, id + "stepCab", STEP_CAB_AXLE_Y, STEP_CAB_AXLE_Z, STEP_AXLE_X0, STEP_AXLE_X1, "step, cabinet side");
        b3Axle(context, id + "stepOut", STEP_OUT_AXLE_Y, STEP_OUT_AXLE_Z, STEP_AXLE_X0, STEP_AXLE_X1, "step, outer");
    });

function b3TreadHardware(context is Context, id is Id, n is number, label is string)
{
    const top = b3T(n);
    const x0 = b3GoingX(n) + TREAD_AXLE_X0;
    const x1 = x0 + TREAD_AXLE_LEN;

    b3Axle(context, id + "cabAxle", CAB_AXLE_Y, top - CAB_AXLE_DROP, x0, x1, "cabinet, " ~ label);

    b3Axle(context, id + "outAxle", OUT_AXLE_Y, top - OUT_AXLE_DROP, x0, x1, "outer, " ~ label);
    opTransform(context, id + "fold", { "bodies" : b3Solids(id + "outAxle"), "transform" : b3RailFold() });

    // Stop pins: flush with each bracket ear's inside face, sticking out under the lip.
    const ranges = b3EarRanges(n);
    const pz = top - STOP_PIN_DROP;
    b3CylX(context, id + "pinBack", STOP_PIN_Y, pz, ranges[0][0] - STOP_PIN_OUT, ranges[0][1], STOP_PIN_DIA);
    b3CylX(context, id + "pinFront", STOP_PIN_Y, pz, ranges[1][0], ranges[1][1] + STOP_PIN_OUT, STOP_PIN_DIA);
    b3FinishSteel(context, b3Solids(id + "pinBack"), "Stop pin, back, " ~ label);
    b3FinishSteel(context, b3Solids(id + "pinFront"), "Stop pin, front, " ~ label);
}

// One axle from x0 to x1: the tube, then a fender washer and button head on each end.
function b3Axle(context is Context, id is Id, y is ValueWithUnits, z is ValueWithUnits,
    x0 is ValueWithUnits, x1 is ValueWithUnits, label is string)
{
    b3TubeX(context, id + "tube", y, z, x0, x1, AXLE_DIA, AXLE_BORE);
    b3FinishTube(context, b3Solids(id + "tube"), "Axle, " ~ label);
    b3AxleEnd(context, id + "back", y, z, x0, -1, "Axle screw, " ~ label ~ ", back");
    b3AxleEnd(context, id + "front", y, z, x1, 1, "Axle screw, " ~ label ~ ", front");
}

// Fender washer and button head as one part, stacked outward from the tube end at x
// (dir -1 toward the bed end, 1 toward the front).
function b3AxleEnd(context is Context, id is Id, y is ValueWithUnits, z is ValueWithUnits, x is ValueWithUnits,
    dir is number, partName is string)
{
    const w = x + dir * END_WASHER_T;
    b3CylX(context, id + "washer", y, z, x, w, END_WASHER_DIA);
    b3CylX(context, id + "head", y, z, w, w + dir * END_HEAD_H, END_HEAD_DIA);
    b3Union(context, id + "join", b3Solids(id));
    b3FinishSteel(context, b3Solids(id), partName);
}
