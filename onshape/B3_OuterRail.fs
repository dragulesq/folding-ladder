FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Outer Rail, 3/16" sheet-metal part: stepped plate with a notch at the
 * bed-side bottom corner to clear the door handle by 1", 1" flanges turned
 * outward (+y) along the bed-side edge, the long lower edge, the foot and the
 * front edge, a hand slot, the step clip's rivet holes, and two rows of M6
 * holes for the rail brackets (the bracket part, turned to face the cabinet).
 * The bed-side and front flanges run the full length of their edges, so their
 * bends need no relief. The long flange stops 1/2" short of the notch corner,
 * with a bend relief there and the bare edge past it cut back inside the bend
 * line. Where two flanges meet at the foot, Onshape cuts a round corner
 * relief. Every flange's free corners are rounded. The notch's short edges and
 * the step tops are bare.
 *
 * Not modeled: the latch holes and the foot pad.
 */
annotation { "Feature Type Name" : "B3 Outer Rail" }
export const b3OuterRail = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const y0 = RAIL_Y;
        const y1 = RAIL_Y + PLATE_T;
        const fy1 = y1 + RAIL_FLANGE;
        const xm = GOING;
        const xf = 2 * GOING;
        const topStep = b3T(TREAD_TOP) - RAIL_STEP_DROP;
        const midStep = b3T(TREAD_MIDDLE) - RAIL_STEP_DROP;

        // The long lower edge runs from the foot through (RAIL_LINE_X, RAIL_LINE_Z)
        // up to the notch's front edge.
        const foot = vector(RAIL_FOOT_X, RAIL_FOOT_Z);
        const slope = (RAIL_LINE_Z - RAIL_FOOT_Z) / (RAIL_LINE_X - RAIL_FOOT_X);
        const notchLow = vector(RAIL_NOTCH_X, RAIL_FOOT_Z + (RAIL_NOTCH_X - RAIL_FOOT_X) * slope);

        // Plate outline in (x, z), same order as the B3 page, with its outside
        // corners rounded except where a flange's bend ends.
        const corners = [
                vector(0 * inch, topStep),
                vector(xm, topStep),
                vector(xm, midStep),
                vector(xf, midStep),
                vector(xf, RAIL_FOOT_Z),
                foot,
                notchLow,
                vector(RAIL_NOTCH_X, RAIL_BACK_Z),
                vector(0 * inch, RAIL_BACK_Z)
            ];
        const radii = [0, RAIL_CORNER_R, RAIL_STEP_R, 0, 0, 0, RAIL_NOTCH_R, RAIL_NOTCH_IN_R, 0];
        var holes = [b3Slot(vector(HAND_SLOT_X, HAND_SLOT_Z), HAND_SLOT_L, HAND_SLOT_H)];
        for (var i = 0; i < size(CLIP_RIVET_X); i += 1)
        {
            holes = append(holes, b3Hole(CLIP_RIVET_X[i], CLIP_RIVET_Z, RIVET_HOLE_DIA));
        }
        for (var n in [TREAD_TOP, TREAD_MIDDLE])
        {
            for (var k = 0; k < size(SCREW_X); k += 1)
            {
                const nut = b3RailNutJoint(n, k).origin;
                holes = append(holes, b3Hole(nut[0], nut[2], SCREW_HOLE_DIA));
            }
        }
        b3PrismXZ(context, id + "plate", b3Rounded(corners, radii), holes, y0, y1);

        // Flanges along their edges (listed so the plate is on the left), turned
        // outward, with their free corners rounded. The bed-side and front ones run
        // from corner to corner; the long one stops RAIL_FLANGE_STOP short of the
        // notch corner.
        const longDir = (foot - notchLow) / norm(foot - notchLow);
        const longTop = notchLow + longDir * RAIL_FLANGE_STOP;
        const flanges = [
                [vector(0 * inch, topStep), vector(0 * inch, RAIL_BACK_Z)],
                [longTop, foot],
                [foot, vector(xf, RAIL_FOOT_Z)],
                [vector(xf, RAIL_FOOT_Z), vector(xf, midStep)]
            ];
        var outside = [vector(xm + GOING / 2, y0, CLIP_RIVET_Z + 2.5 * inch)];
        for (var i = 0; i < size(flanges); i += 1)
        {
            const f = flanges[i];
            b3Flange(context, id + ("flange" ~ i), f[0], f[1], y0, fy1, RAIL_FLANGE_TIP_R, RAIL_FLANGE_TIP_R);
            const mid = (f[0] + f[1]) / 2;
            outside = append(outside, vector(mid[0], (y1 + fy1) / 2, mid[1]));
        }
        b3Union(context, id + "join", b3Solids(id));

        // Bend relief where the long flange stops, cut into the plate. Past it, the
        // edge is cut back inside the bend line until it runs into the notch's edge.
        const inward = vector(-longDir[1], longDir[0]);
        const bareInset = b3BareInset(PLATE_T, BEND_R_PLATE, K_PLATE);
        const bareLength = (RAIL_NOTCH_X - longTop[0] - inward[0] * bareInset) / (-longDir[0]) + 0.1 * inch;
        b3PrismXZ(context, id + "relief", b3ReliefPath(longTop, -longDir, inward, PLATE_T, BEND_R_PLATE, K_PLATE, bareLength), [],
            y0 - 0.05 * inch, y1 + 0.05 * inch);
        b3Subtract(context, id + "cutRelief", qSubtraction(b3Solids(id), b3Solids(id + "relief")), b3Solids(id + "relief"));

        b3SheetMetal(context, id + "sm", b3Solids(id), outside, PLATE_T, BEND_R_PLATE, K_PLATE);

        const rail = b3Solids(id);
        const name = "Outer rail";
        b3Finish(context, rail, name, RGB_ALU);
        b3Mc(context, id + "mcTop", rail, name, "Top tread bracket", b3RailBracketJoint(TREAD_TOP));
        b3Mc(context, id + "mcMiddle", rail, name, "Middle tread bracket", b3RailBracketJoint(TREAD_MIDDLE));
        b3Mc(context, id + "mcClip", rail, name, "Step clip", b3StepClipJoint());
        for (var k = 0; k < size(SCREW_X); k += 1)
        {
            b3Mc(context, id + ("mcNutTop" ~ k), rail, name, "Nut, top " ~ (k + 1), b3RailNutJoint(TREAD_TOP, k));
            b3Mc(context, id + ("mcNutMiddle" ~ k), rail, name, "Nut, middle " ~ (k + 1), b3RailNutJoint(TREAD_MIDDLE, k));
        }
    });
