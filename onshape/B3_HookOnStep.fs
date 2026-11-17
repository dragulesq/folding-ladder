FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Hook-on Step Legs, two 1/8" sheet-metal parts with the same outline. The
 * hook-on step's tray is the tread part, which the assembly places at the
 * step. Each leg is a 5 1/8 x 10 web bent into a shallow channel: a 5/8" side
 * flange at each x end, full height, that widens at the top into a pivot ear,
 * and a 5/8" foot flange along the bottom. The ears have holes on the tray's
 * two pivot lines, 7/8" apart: the "Hook-on step leg" (cabinet side) turns on
 * the upper one, and the "Hook-on step leg, outer", turned to face the
 * cabinet, on the lower one. Each leg's front ear has the other hole too,
 * tapped M6 for its lock: a McMaster 8501A54 twist-lock spring plunger whose
 * nose springs through the tray's front lip when the leg is open, with an M6
 * thin jam nut inside the ear. The side flanges run to the top edge, so their
 * bends need no relief; where they meet the foot flange, Onshape cuts round
 * corner reliefs and the flanges' tips stay square. The top edge is the open
 * stop against the tray's skin. Both are built where the cabinet-side leg
 * sits, open. The rubber pad under the foot is its own part (B3 Bushings and
 * Pads).
 */
annotation { "Feature Type Name" : "B3 Hook-on Step Leg" }
export const b3HookOnStep = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const n = HOOK_STEP;
        const upper = b3T(n) - CAB_AXLE_DROP;
        const lower = b3T(n) - OUT_AXLE_DROP;
        const faces = b3EarFaces(n);

        // Cabinet-side leg: turns on the upper holes, lock in the front ear's lower hole.
        b3StepLeg(context, id + "cab", "Hook-on step leg", upper, [], [b3Hole(CAB_AXLE_Y, lower, LEG_LOCK_TAP)], b3LegLockJoint());
        // Outer leg: turns on the lower holes. Turned half a turn, the ear built at the
        // back faces the front, and takes the lock in its upper hole.
        b3StepLeg(context, id + "out", "Hook-on step leg, outer", lower, [b3Hole(CAB_AXLE_Y, upper, LEG_LOCK_TAP)], [], b3OuterLegLockJoint());

        // Jam nut on the plunger's thread, inside the cabinet-side leg's front ear, drawn
        // round at its across-flats size.
        const xn = faces[1] - SHEET_T;
        b3TubeX(context, id + "nut", CAB_AXLE_Y, lower, xn - LEG_LOCK_NUT_T, xn, LEG_LOCK_NUT_AF, LEG_LOCK_TAP);
        b3FinishSteel(context, b3Solids(id + "nut"), "M6 jam nut");
        b3Mc(context, id + "mcNut", b3Solids(id + "nut"), "M6 jam nut", "Ear", b3LegLockJoint());
    });

// One leg, built at the cabinet-side leg's place: its pivot holes at height
// pivotZ in both ears, and the lock's tapped hole in the back or the front ear.
function b3StepLeg(context is Context, id is Id, name is string, pivotZ is ValueWithUnits, backHoles is array, frontHoles is array, lock is map)
{
    const n = HOOK_STEP;
    const faces = b3EarFaces(n);
    const y0 = STEP_LEG_Y0;
    const yTip = y0 + STEP_LEG_FLANGE;
    const yEar = y0 + STEP_LEG_EAR;
    const zb = STEP_LEG_BOTTOM_Z;
    const zt = STEP_LEG_TOP_Z;
    const r = R_OUTSIDE_SMALL;

    // Web, its outer face toward the cabinet.
    b3PrismXZ(context, id + "web", b3Rect(faces[0], zb, faces[1], zt), [], y0, y0 + SHEET_T);

    // Side flanges in (y, z), outer faces at the ear faces, widening into the ears.
    const side = b3Rounded([
                vector(y0, zb),
                vector(yTip, zb),
                vector(yTip, STEP_LEG_EAR_Z),
                vector(yEar, STEP_LEG_EAR_Z),
                vector(yEar, zt),
                vector(y0, zt)
            ], [0, 0, R_INSIDE_MIN, r, STEP_LEG_EAR_R, 0]);
    const pivot = [b3Hole(CAB_AXLE_Y, pivotZ, HOLE_DIA)];
    b3PrismYZ(context, id + "sideBack", side, concatenateArrays([pivot, backHoles]), faces[0], faces[0] + SHEET_T);
    b3PrismYZ(context, id + "sideFront", side, concatenateArrays([pivot, frontHoles]), faces[1] - SHEET_T, faces[1]);

    // Foot flange in (x, y).
    b3PrismXY(context, id + "foot", b3Rect(faces[0], y0, faces[1], yTip), [], zb, zb + SHEET_T);
    b3Union(context, id + "join", b3Solids(id));

    // Sheet metal: the web's outer face, then the side flanges' outer faces and the foot's underside.
    const midX = (faces[0] + faces[1]) / 2;
    const midY = (y0 + yTip) / 2;
    b3SheetMetal(context, id + "sm", b3Solids(id), [
                vector(midX, y0, zt / 2),
                vector(faces[0], midY, zt / 2),
                vector(faces[1], midY, zt / 2),
                vector(midX, midY, zb)
            ], SHEET_T, BEND_R_SHEET, K_SHEET);

    const leg = b3Solids(id);
    b3Finish(context, leg, name, RGB_STEP);
    b3Mc(context, id + "mcAxle", leg, name, "Axle", b3CabAxleJoint(n));
    b3Mc(context, id + "mcAxleTurned", leg, name, "Axle, turned", b3TurnInGoing(n, b3OutAxleJoint(n), 0 * inch));
    // The other end of each hole, for the bushing in the second ear, and the pad's seat.
    b3Mc(context, id + "mcAxleFront", leg, name, "Axle, front", b3CabAxleTurnedJoint(n));
    b3Mc(context, id + "mcAxleLower", leg, name, "Lower axle", b3StepLegLowerJoint());
    b3Mc(context, id + "mcPad", leg, name, "Foot pad", b3StepPadJoint());
    b3Mc(context, id + "mcLock", leg, name, "Lock", lock);
}
