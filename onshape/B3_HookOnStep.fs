FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Hook-on Step: a 1/8" sheet-metal tray like the treads and two 3/16"
 * sheet-metal legs. Each leg is a web with a hook-topped ear bent inboard at
 * each end; the ears turn on an axle through the tray lips. An ear's bend
 * covers only its 1" root, so a relief is cut into the web at both ends of
 * it. The axle's back end is a pin that drops into a keyhole: the front
 * angle for the cabinet-side leg, the step clip for the outer leg. Built
 * open; the legs fold flat in two layers inside the lips. Not modeled: the
 * rubber boots.
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
        b3Tray(context, id + "tray", b3GoingX(HOOK_STEP) + TRAY_INSET, top, holes);
        const tray = b3Solids(id + "tray");
        const name = "Hook-on step tray";
        b3Finish(context, tray, name, RGB_STEP);
        b3Mc(context, id + "mcCab", tray, name, "Cabinet-side axle", b3StepCabAxleJoint());
        b3Mc(context, id + "mcOut", tray, name, "Outer axle", b3StepOutAxleJoint());

        // Cabinet-side leg: web y 3/8 to 9/16, ears reach toward +y.
        const cabIn = STEP_LEG_CAB_Y0 + PLATE_T;
        b3StepLeg(context, id + "cabLeg", STEP_LEG_CAB_Y0, cabIn, cabIn + STEP_EAR_W,
            STEP_CAB_AXLE_Y - EAR_HOOK_R, b3StepCabAxleJoint(), "Hook-on step leg, cabinet side");

        // Outer leg: web y 18 3/16 to 18 3/8, ears reach toward -y.
        const outIn = STEP_LEG_OUT_Y1 - PLATE_T;
        b3StepLeg(context, id + "outLeg", STEP_LEG_OUT_Y1, outIn, outIn - STEP_EAR_W,
            STEP_OUT_AXLE_Y + EAR_HOOK_R, b3StepOutAxleJoint(), "Hook-on step leg, outer");
    });

// One leg. yOut and yIn are the web's faces; the ear runs from the web to
// yFar below the axle, and its round top ends at yArc on the web side.
function b3StepLeg(context is Context, id is Id, yOut is ValueWithUnits, yIn is ValueWithUnits, yFar is ValueWithUnits,
    yArc is ValueWithUnits, axle is map, partName is string)
{
    const axY = axle.origin[1];
    const axZ = axle.origin[2];
    const faces = b3EarFaces(HOOK_STEP);
    const ylo = min(yOut, yIn);
    const yhi = max(yOut, yIn);
    const r = LEG_CORNER_R;
    const web = b3Rounded(b3Rect(faces[0], STEP_LEG_BOTTOM_Z, faces[1], STEP_LEG_TOP_Z), [r, r, r, r]);
    b3PrismXZ(context, id + "web", web, [], ylo, yhi);

    const earLo = axZ - STEP_EAR_DROP;
    const ear = [
            vector(yOut, earLo),
            vector(yFar, earLo),
            vector(yFar, axZ),
            { "mid" : vector(axY, axZ + EAR_HOOK_R), "end" : vector(yArc, axZ) },
            vector(yOut, axZ)
        ];
    const holes = [b3Hole(axY, axZ, HOLE_DIA)];
    const ranges = b3EarRanges(HOOK_STEP);
    b3PrismYZ(context, id + "earBack", ear, holes, ranges[0][0], ranges[0][1]);
    b3PrismYZ(context, id + "earFront", ear, holes, ranges[1][0], ranges[1][1]);
    b3Union(context, id + "join", b3Solids(id));

    // Bend reliefs at both ends of each ear's root, cut into the web.
    const reliefs = [
            [vector(faces[0], earLo), vector(0, -1), vector(1, 0)],
            [vector(faces[0], axZ), vector(0, 1), vector(1, 0)],
            [vector(faces[1], earLo), vector(0, -1), vector(-1, 0)],
            [vector(faces[1], axZ), vector(0, 1), vector(-1, 0)]
        ];
    for (var i = 0; i < size(reliefs); i += 1)
    {
        const rl = reliefs[i];
        b3PrismXZ(context, id + "relief" + ("r" ~ i), b3ReliefPath(rl[0], rl[1], rl[2], PLATE_T, BEND_R_PLATE), [],
            ylo - 0.05 * inch, yhi + 0.05 * inch);
    }
    b3Subtract(context, id + "cutReliefs", qSubtraction(b3Solids(id), b3Solids(id + "relief")), b3Solids(id + "relief"));

    // Sheet metal: the web's outer face, then each ear's outer face.
    const earY = (yOut + yFar) / 2;
    const midX = (faces[0] + faces[1]) / 2;
    b3SheetMetal(context, id + "sm", b3Solids(id), [
                vector(midX, yOut, STEP_LEG_BOTTOM_Z + 1 * inch),
                vector(faces[0], earY, earLo + STEP_EAR_DROP / 4),
                vector(faces[1], earY, earLo + STEP_EAR_DROP / 4)
            ], PLATE_T, BEND_R_PLATE, K_PLATE);

    b3Finish(context, b3Solids(id), partName, RGB_STEP);
    b3Mc(context, id + "mcAxle", b3Solids(id), partName, "Axle", axle);
}
