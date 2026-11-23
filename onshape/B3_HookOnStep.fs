FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Hook-on Step Leg, 1/8" sheet-metal part, used twice. The hook-on step's
 * tray is the tread part, which the assembly places at the step. Each leg is
 * a 5 1/8 x 10 web bent into a channel: a 5/8" side flange at each x
 * end, full height, that widens at the top into a pivot ear, and a 5/8" foot
 * flange along the bottom. Each ear has two 14 mm holes 7/8" apart, on the
 * tray's two pivot lines: the cabinet-side leg turns on the upper one, and the
 * outer leg, the same part turned to face the cabinet, on the lower one.
 * Halfway between them and 0.3" toward the web is a hole tapped M6
 * (drawn at its 5 mm tap drill; only the ear that faces the front uses it)
 * for the leg's lock: a McMaster 8501A54
 * twist-lock spring plunger whose nose springs through the tray's front lip
 * when the leg is open, with an M6 thin jam nut inside the ear. The side
 * flanges run to the top edge, so their bends need no relief; where they meet
 * the foot flange, Onshape cuts round corner reliefs and the flanges' tips
 * stay square. The top edge is the open stop against the tray's skin. Built
 * where the cabinet-side leg sits, open. The rubber pad under the foot is its
 * own part (B3 Bushings and Pads).
 */
annotation { "Feature Type Name" : "B3 Hook-on Step Leg" }
export const b3HookOnStep = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
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
        b3PrismXZ(context, id + "leg" + "web", b3Rect(faces[0], zb, faces[1], zt), [], y0, y0 + SHEET_T);

        // Side flanges in (y, z), outer faces at the ear faces, widening into the ears.
        const side = b3Rounded([
                    vector(y0, zb),
                    vector(yTip, zb),
                    vector(yTip, STEP_LEG_EAR_Z),
                    vector(yEar, STEP_LEG_EAR_Z),
                    vector(yEar, zt),
                    vector(y0, zt)
                ], [0, 0, R_INSIDE_MIN, r, STEP_LEG_EAR_R, 0]);
        const holes = [
                b3Hole(CAB_AXLE_Y, b3T(n) - CAB_AXLE_DROP, HOLE_DIA),
                b3Hole(CAB_AXLE_Y, b3T(n) - OUT_AXLE_DROP, HOLE_DIA),
                b3Hole(CAB_AXLE_Y + LEG_LOCK_DY, b3T(n) - LEG_LOCK_DROP, LEG_LOCK_TAP)
            ];
        b3PrismYZ(context, id + "leg" + "sideBack", side, holes, faces[0], faces[0] + SHEET_T);
        b3PrismYZ(context, id + "leg" + "sideFront", side, holes, faces[1] - SHEET_T, faces[1]);

        // Foot flange in (x, y).
        b3PrismXY(context, id + "leg" + "foot", b3Rect(faces[0], y0, faces[1], yTip), [], zb, zb + SHEET_T);
        b3Union(context, id + "leg" + "join", b3Solids(id + "leg"));

        // Sheet metal: the web's outer face, then the side flanges' outer faces and the foot's underside.
        const midX = (faces[0] + faces[1]) / 2;
        const midY = (y0 + yTip) / 2;
        b3SheetMetal(context, id + "leg" + "sm", b3Solids(id + "leg"), [
                    vector(midX, y0, zt / 2),
                    vector(faces[0], midY, zt / 2),
                    vector(faces[1], midY, zt / 2),
                    vector(midX, midY, zb)
                ], SHEET_T, BEND_R_SHEET, K_SHEET);

        const leg = b3Solids(id + "leg");
        const name = "Hook-on step leg";
        b3Finish(context, leg, name, RGB_STEP);
        b3Mc(context, id + "mcAxle", leg, name, "Axle", b3CabAxleJoint(n));
        b3Mc(context, id + "mcAxleTurned", leg, name, "Axle, turned", b3TurnInGoing(n, b3OutAxleJoint(n), 0 * inch));
        // The other end of each hole, for the bushing in the second ear, and the pad's seat.
        b3Mc(context, id + "mcAxleFront", leg, name, "Axle, front", b3CabAxleTurnedJoint(n));
        b3Mc(context, id + "mcAxleLower", leg, name, "Lower axle", b3StepLegLowerJoint());
        b3Mc(context, id + "mcPad", leg, name, "Foot pad", b3StepPadJoint());
        b3Mc(context, id + "mcLock", leg, name, "Lock", b3LegLockJoint());
        b3Mc(context, id + "mcLockTurned", leg, name, "Lock, turned", b3LegLockTurnedJoint());

        // Jam nut on the plunger's thread, inside the cabinet-side leg's front ear, drawn
        // round at its across-flats size.
        const lock = b3LegLockJoint().origin;
        const xn = faces[1] - SHEET_T;
        b3TubeX(context, id + "nut", lock[1], lock[2], xn - LEG_LOCK_NUT_T, xn, LEG_LOCK_NUT_AF, LEG_LOCK_TAP);
        b3FinishSteel(context, b3Solids(id + "nut"), "M6 jam nut");
        b3Mc(context, id + "mcNut", b3Solids(id + "nut"), "M6 jam nut", "Ear", b3LegLockJoint());
    });
