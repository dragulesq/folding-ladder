FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Hook-on Step: a 1/8" sheet-metal tray like the treads and two 3/16"
 * sheet-metal legs. Each leg is a web with an ear bent inboard at each end;
 * the ears turn on an axle through the tray lips. An ear's bend runs from 1"
 * below its axle hole up to the web's top edge, so the hole sits in the ear
 * with the bend beside it, and the ear's top runs straight from the web's top
 * edge onto the round over the hole. Below the ear, a bend relief is cut into
 * the web and the web's edge is cut back inside the bend line. The axle's back
 * end is a pin that drops into a keyhole: the front angle for the
 * cabinet-side leg, the step clip for the outer leg. Built open; the legs fold
 * flat in two layers inside the lips. Not modeled: the rubber boots.
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
            b3StepCabAxleJoint(), "Hook-on step leg, cabinet side");

        // Outer leg: web y 18 3/16 to 18 3/8, ears reach toward -y.
        const outIn = STEP_LEG_OUT_Y1 - PLATE_T;
        b3StepLeg(context, id + "outLeg", STEP_LEG_OUT_Y1, outIn, outIn - STEP_EAR_W,
            b3StepOutAxleJoint(), "Hook-on step leg, outer");
    });

// One leg. yOut and yIn are the web's faces; the ear runs from the web to
// yFar below the axle.
function b3StepLeg(context is Context, id is Id, yOut is ValueWithUnits, yIn is ValueWithUnits, yFar is ValueWithUnits,
    axle is map, partName is string)
{
    const axY = axle.origin[1];
    const axZ = axle.origin[2];
    const faces = b3EarFaces(HOOK_STEP);
    const ylo = min(yOut, yIn);
    const yhi = max(yOut, yIn);
    const top = STEP_LEG_TOP_Z;
    const earLo = axZ - STEP_EAR_DROP;

    // Web in (x, z). At each end the ear's bend runs from earLo up to the top
    // edge. Below it, a bend relief, then the edge cut back inside the bend line.
    const x0 = faces[0];
    const x1 = faces[1];
    const d = b3ReliefDepth(PLATE_T, BEND_R_PLATE, K_PLATE);
    const e = b3BareInset(PLATE_T, BEND_R_PLATE, K_PLATE);
    const zr = earLo - RELIEF_W_SCALE * PLATE_T;
    const r = LEG_CORNER_R;
    const web = [
            vector(x0 + e, STEP_LEG_BOTTOM_Z),
            vector(x1 - e, STEP_LEG_BOTTOM_Z),
            vector(x1 - e, zr),
            vector(x1 - d, zr),
            vector(x1 - d, earLo),
            vector(x1, earLo),
            vector(x1, top),
            vector(x0, top),
            vector(x0, earLo),
            vector(x0 + d, earLo),
            vector(x0 + d, zr),
            vector(x0 + e, zr)
        ];
    b3PrismXZ(context, id + "web", b3Rounded(web, [r, r, r, 0, 0, 0, 0, 0, 0, 0, 0, r]), [], ylo, yhi);

    // Ear in (y, z): full width from earLo up to the axle, a round of EAR_HOOK_R
    // on the hole, then a straight edge from the round's top side to the web's
    // top edge, touching the round.
    const c = vector(axY, axZ);
    const corner = vector(yOut, top);
    const toCorner = corner - c;
    const reach = acos(EAR_HOOK_R / norm(toCorner));
    const toCornerAngle = atan2(toCorner[1], toCorner[0]);
    const t1 = c + vector(cos(toCornerAngle - reach), sin(toCornerAngle - reach)) * EAR_HOOK_R;
    const t2 = c + vector(cos(toCornerAngle + reach), sin(toCornerAngle + reach)) * EAR_HOOK_R;
    const touch = t1[1] > t2[1] ? t1 : t2;
    const farAngle = yFar > axY ? 0 * degree : 180 * degree;
    const midAngle = (farAngle + atan2(touch[1] - axZ, touch[0] - axY)) / 2;
    const ear = concatenateArrays([
                [vector(yOut, earLo)],
                b3Corner(vector(yOut, earLo), vector(yFar, earLo), vector(yFar, axZ), r),
                [vector(yFar, axZ), { "mid" : c + vector(cos(midAngle), sin(midAngle)) * EAR_HOOK_R, "end" : touch }, corner]
            ]);
    const holes = [b3Hole(axY, axZ, HOLE_DIA)];
    const ranges = b3EarRanges(HOOK_STEP);
    b3PrismYZ(context, id + "earBack", ear, holes, ranges[0][0], ranges[0][1]);
    b3PrismYZ(context, id + "earFront", ear, holes, ranges[1][0], ranges[1][1]);
    b3Union(context, id + "join", b3Solids(id));

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
