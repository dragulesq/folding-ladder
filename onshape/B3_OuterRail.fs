FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Outer Rail, 3/16" sheet-metal part: stepped plate whose lower edge runs
 * in one straight line from the bottom of the bed-side edge to the foot, 1"
 * flanges turned outward (+y) along the bed-side edge, the riser between the
 * steps, the long lower edge, the foot and the front edge, a hand slot, the
 * step clip's M6 holes, and two rows of M6 holes for the rail brackets (the
 * bracket part, turned to face the cabinet). The stair is narrow enough that
 * the rail and its flanges stay inside the cab door handle, so there is no
 * notch around it. The riser flange runs up to the top edge and stops a
 * relief's width above the middle step, with a bend relief below it. The
 * other four flanges run the full length of their edges and meet end to end:
 * at the bottom of the bed-side edge and at both ends of the foot, Onshape
 * cuts a round corner relief and the flanges' tips stay square. Every free
 * flange corner is rounded. The step tops are bare.
 *
 * The foot is 7" long. Folded, its back inch stands in front of the stowed
 * hook-on step's top, and a rubber bumper stuck on the inner face there
 * presses on the step, so the travel lock holds the step too.
 *
 * The foot pads and the bumper are their own parts (B3 Bushings and Pads).
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

        // The long lower edge runs straight from the bottom of the bed-side edge
        // to the back of the foot.
        const foot = vector(RAIL_FOOT_X, RAIL_FOOT_Z);
        const back = vector(0 * inch, RAIL_BACK_Z);

        // Plate outline in (x, z), same order as the B3 page. Every corner is the
        // end of a flange's bend, so it stays sharp; the flanges' free corners are
        // rounded.
        const corners = [
                vector(0 * inch, topStep),
                vector(xm, topStep),
                vector(xm, midStep),
                vector(xf, midStep),
                vector(xf, RAIL_FOOT_Z),
                foot,
                back
            ];
        // The hand slot.
        var holes = [b3Slot(vector(HAND_SLOT_X, HAND_SLOT_Z), HAND_SLOT_L, HAND_SLOT_H)];
        for (var k = 0; k < size(CLIP_SCREW_X); k += 1)
        {
            holes = append(holes, b3Hole(CLIP_SCREW_X[k], CLIP_SCREW_Z, SCREW_HOLE_DIA));
        }
        for (var n in [TREAD_TOP, TREAD_MIDDLE])
        {
            for (var k = 0; k < size(SCREW_X); k += 1)
            {
                const nut = b3RailNutJoint(n, k).origin;
                holes = append(holes, b3Hole(nut[0], nut[2], SCREW_HOLE_DIA));
            }
        }
        b3PrismXZ(context, id + "plate", corners, holes, y0, y1);

        // Flanges along their edges (listed so the plate is on the left), turned
        // outward, each with the radii of its two tip corners. The free corners are
        // rounded; where two flanges meet (the bottom of the bed-side edge and both
        // ends of the foot), the tips stay square beside the corner relief. The
        // bed-side, long, foot and front ones run from corner to corner; the riser
        // one from a relief's width above the middle step up to the top edge.
        const riserBottom = vector(xm, midStep + RELIEF_W_SCALE * PLATE_T);
        const tip = RAIL_FLANGE_TIP_R;
        const flanges = [
                [vector(0 * inch, topStep), back, tip, 0 * inch],
                [riserBottom, vector(xm, topStep), tip, tip],
                [back, foot, 0 * inch, 0 * inch],
                [foot, vector(xf, RAIL_FOOT_Z), 0 * inch, 0 * inch],
                [vector(xf, RAIL_FOOT_Z), vector(xf, midStep), 0 * inch, tip]
            ];
        var outside = [vector(xm + GOING / 2, y0, CLIP_SCREW_Z + 2.5 * inch)];
        for (var i = 0; i < size(flanges); i += 1)
        {
            const f = flanges[i];
            b3Flange(context, id + ("flange" ~ i), f[0], f[1], y0, fy1, f[2], f[3]);
            // A point on each flange's outside face; on the foot, off the travel lock's hole at mid-foot.
            const mid = i == 3 ? (f[0] * 3 + f[1]) / 4 : (f[0] + f[1]) / 2;
            outside = append(outside, vector(mid[0], (y1 + fy1) / 2, mid[1]));
        }
        b3Union(context, id + "join", b3Solids(id));

        // Bend relief, cut into the plate below the riser flange, down to the
        // middle step, so the flange's flat pattern clears the plate below it.
        b3PrismXZ(context, id + "riserRelief", b3ReliefPath(riserBottom, vector(0, -1), vector(-1, 0), PLATE_T, BEND_R_PLATE, K_PLATE,
                0 * inch), [], y0 - 0.05 * inch, y1 + 0.05 * inch);
        const reliefs = b3Solids(id + "riserRelief");
        b3Subtract(context, id + "cutRelief", qSubtraction(b3Solids(id), reliefs), reliefs);

        // The travel lock's hole in the foot flange: the plunger's nose comes up
        // through it when the stair is folded.
        b3Cyl(context, id + "lockHole", vector(LOCK_X, LOCK_Y, RAIL_FOOT_Z - 1 * millimeter),
            vector(LOCK_X, LOCK_Y, RAIL_FOOT_Z + PLATE_T + 1 * millimeter), LOCK_HOLE);
        const lockHole = b3Solids(id + "lockHole");
        b3Subtract(context, id + "cutLock", qSubtraction(b3Solids(id), lockHole), lockHole);


        b3SheetMetal(context, id + "sm", b3Solids(id), outside, PLATE_T, BEND_R_PLATE, K_PLATE);

        const rail = b3Solids(id);
        const name = "Outer rail";
        b3Finish(context, rail, name, RGB_ALU);
        b3Mc(context, id + "mcTop", rail, name, "Top tread bracket", b3RailBracketJoint(TREAD_TOP));
        b3Mc(context, id + "mcMiddle", rail, name, "Middle tread bracket", b3RailBracketJoint(TREAD_MIDDLE));
        b3Mc(context, id + "mcClip", rail, name, "Step clip", b3StepClipJoint());
        b3Mc(context, id + "mcPad", rail, name, "Foot pad", b3RailPadJoint(1));
        b3Mc(context, id + "mcPadBack", rail, name, "Foot pad, back", b3RailPadJoint(0));
        b3Mc(context, id + "mcBumper", rail, name, "Step bumper", b3RailBumperJoint());
        for (var k = 0; k < size(CLIP_SCREW_X); k += 1)
        {
            b3Mc(context, id + ("mcNutClip" ~ k), rail, name, "Nut, clip " ~ (k + 1), b3ClipNutJoint(k));
        }
        for (var k = 0; k < size(SCREW_X); k += 1)
        {
            b3Mc(context, id + ("mcNutTop" ~ k), rail, name, "Nut, top " ~ (k + 1), b3RailNutJoint(TREAD_TOP, k));
            b3Mc(context, id + ("mcNutMiddle" ~ k), rail, name, "Nut, middle " ~ (k + 1), b3RailNutJoint(TREAD_MIDDLE, k));
        }
    });
