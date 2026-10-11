FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Hook-on Step: a 1/8" tray like the treads (no corner relief) and two
 * 3/16" legs. Each leg is a web with a hook-topped ear bent inboard at each
 * end; the ears turn on an axle through the tray lips. The axle's back end
 * is a pin that drops into a keyhole: the front angle for the cabinet-side
 * leg, the step clip for the outer leg. Built open; the legs fold flat in
 * two layers inside the lips. Not modeled: the rubber boots.
 */
annotation { "Feature Type Name" : "B3 Hook-on Step" }
export const b3HookOnStep = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const top = b3T(HOOK_STEP);
        const holes = [
                b3Hole(STEP_CAB_AXLE_Y, STEP_CAB_AXLE_Z, HOLE_DIA),
                b3Hole(STEP_OUT_AXLE_Y, STEP_OUT_AXLE_Z, HOLE_DIA)
            ];
        b3Tray(context, id + "tray", b3GoingX(HOOK_STEP) + TRAY_INSET, top, holes, 0 * inch);
        b3Finish(context, b3Solids(id + "tray"), "Hook-on step tray", RGB_STEP);

        // Cabinet-side leg: web y 3/8 to 9/16, ears reach toward +y.
        const cabIn = STEP_LEG_CAB_Y0 + PLATE_T;
        b3StepLeg(context, id + "cabLeg", STEP_LEG_CAB_Y0, cabIn, cabIn + STEP_EAR_W,
            STEP_CAB_AXLE_Y - EAR_HOOK_R, STEP_CAB_AXLE_Y, STEP_CAB_AXLE_Z, "Hook-on step leg, cabinet side");

        // Outer leg: web y 18 3/16 to 18 3/8, ears reach toward -y.
        const outIn = STEP_LEG_OUT_Y1 - PLATE_T;
        b3StepLeg(context, id + "outLeg", STEP_LEG_OUT_Y1, outIn, outIn - STEP_EAR_W,
            STEP_OUT_AXLE_Y + EAR_HOOK_R, STEP_OUT_AXLE_Y, STEP_OUT_AXLE_Z, "Hook-on step leg, outer");
    });

// One leg. yOut and yIn are the web's faces; the ear runs from the web to
// yFar below the axle, and its round top ends at yArc on the web side.
function b3StepLeg(context is Context, id is Id, yOut is ValueWithUnits, yIn is ValueWithUnits, yFar is ValueWithUnits,
    yArc is ValueWithUnits, axY is ValueWithUnits, axZ is ValueWithUnits, partName is string)
{
    const faces = b3EarFaces(HOOK_STEP);
    const ylo = min(yOut, yIn);
    const yhi = max(yOut, yIn);
    b3PrismXZ(context, id + "web", b3Rect(faces[0], STEP_LEG_BOTTOM_Z, faces[1], STEP_LEG_TOP_Z), [], ylo, yhi);

    const ear = [
            vector(yOut, axZ - STEP_EAR_DROP),
            vector(yFar, axZ - STEP_EAR_DROP),
            vector(yFar, axZ),
            { "mid" : vector(axY, axZ + EAR_HOOK_R), "end" : vector(yArc, axZ) },
            vector(yOut, axZ)
        ];
    const holes = [b3Hole(axY, axZ, HOLE_DIA)];
    const ranges = b3EarRanges(HOOK_STEP);
    b3PrismYZ(context, id + "earBack", ear, holes, ranges[0][0], ranges[0][1]);
    b3PrismYZ(context, id + "earFront", ear, holes, ranges[1][0], ranges[1][1]);

    b3Union(context, id + "join", b3Solids(id));
    b3Finish(context, b3Solids(id), partName, RGB_STEP);
}
