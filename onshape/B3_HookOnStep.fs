FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Hook-on Step (provisional): the bottom step, hooked onto the stair in
 * front of the cabinet. A 1/8" tray like the treads, plus two folding legs.
 * Each leg turns on an axle through the tray lips, and its back flange has
 * keyholes (modeled as round holes) that hang on studs: the cabinet-side leg
 * on the front angle, the outer leg on the outer rail's front flange.
 *
 * The B3 page doesn't detail this part yet, so the leg shapes here are a
 * starting point to adjust. Not modeled yet: the leg hinge ears and their
 * axles (STEP_LEG_PIVOT_DROP is reserved for them).
 */
annotation { "Feature Type Name" : "B3 Hook-on Step" }
export const b3HookOnStep = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const top = b3TreadTopZ(HOOK_STEP);
        const yb = b3TreadBackY(HOOK_STEP);
        const yf = yb + TREAD_GOING;
        const x0 = b3TreadX0();
        const x1 = x0 + STEP_WIDTH;
        const t = SHEET_T;
        const over = 0.1 * inch;

        // Tray.
        b3Prism(context, id + "tray", b3FrameYZ(x0), b3TrayOutline(yb, top, STEP_LIP), [], STEP_WIDTH);

        // Legs: plate in (Y, Z) between the lips, reaching back under the
        // back lip to a flange that sits flat against the studs.
        const gap = (1 / 16) * inch;
        const flangeTop = top - STEP_LIP - 0.25 * inch;
        const leg = [
                vector(yb, STEP_LEG_BOTTOM_Z),
                vector(yf - t - gap, STEP_LEG_BOTTOM_Z),
                vector(yf - t - gap, top - t - 0.25 * inch),
                vector(yb + t + gap, top - t - 0.25 * inch),
                vector(yb + t + gap, flangeTop),
                vector(yb, flangeTop)
            ];

        // Cabinet-side leg: plate flush with the tray's end, flange turned inward.
        b3Prism(context, id + "cabLeg" + "plate", b3FrameYZ(x0), leg, [], t);
        b3Box(context, id + "cabLeg" + "flange", vector(x0, yb, STEP_LEG_BOTTOM_Z), vector(CAB_STUD_X + 0.375 * inch, yb + t, flangeTop));
        // Outer leg: plate flush with the tray's end, flange turned outward to the rail's studs.
        b3Prism(context, id + "outLeg" + "plate", b3FrameYZ(x1 - t), leg, [], t);
        b3Box(context, id + "outLeg" + "flange", vector(x1 - t, yb, STEP_LEG_BOTTOM_Z), vector(b3OutStudX() + 0.375 * inch, yb + t, flangeTop));
        b3Union(context, id + "cabLeg" + "join", b3Solids(id + "cabLeg"));
        b3Union(context, id + "outLeg" + "join", b3Solids(id + "outLeg"));

        // Keyholes, round for now.
        b3CylY(context, id + "cut" + "cab1", CAB_STUD_X, STUD_Z1, yb - over, yb + t + over, STUD_HOLE_DIA / 2);
        b3CylY(context, id + "cut" + "cab2", CAB_STUD_X, STUD_Z2, yb - over, yb + t + over, STUD_HOLE_DIA / 2);
        b3CylY(context, id + "cut" + "out1", b3OutStudX(), STUD_Z1, yb - over, yb + t + over, STUD_HOLE_DIA / 2);
        b3CylY(context, id + "cut" + "out2", b3OutStudX(), STUD_Z2, yb - over, yb + t + over, STUD_HOLE_DIA / 2);
        const cutters = b3Solids(id + "cut");
        b3Subtract(context, id + "holes", qUnion([b3Solids(id + "cabLeg"), b3Solids(id + "outLeg")]), cutters);

        b3Finish(context, b3Solids(id + "tray"), "Hook-on step tray", RGB_STEP);
        b3Finish(context, b3Solids(id + "cabLeg"), "Hook-on step leg, cabinet side", RGB_STEP);
        b3Finish(context, b3Solids(id + "outLeg"), "Hook-on step leg, outer", RGB_STEP);
    });
