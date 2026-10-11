FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Outer Rail, 3/16" sheet: stepped plate with the bed-side edge cut back
 * to clear the door handle, a 3/4" flange turned outward (+y) along the
 * bed-side, cut-back, bottom, foot and front edges, a hand slot, the step
 * clip's rivet holes, and two hook-shaped pivot ears per tread bent toward
 * the cabinet. The bed-side and front flanges stop short of the ears.
 *
 * Not modeled: bend reliefs, the notches left where the ear tabs are cut,
 * and the latch holes.
 */
annotation { "Feature Type Name" : "B3 Outer Rail" }
export const b3OuterRail = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const y0 = RAIL_Y;
        const y1 = RAIL_Y + PLATE_T;
        const xm = GOING;
        const xf = 2 * GOING;
        const topStep = b3T(TREAD_TOP) - RAIL_STEP_DROP;
        const midStep = b3T(TREAD_MIDDLE) - RAIL_STEP_DROP;

        // Plate outline in (x, z), same order as the B3 page.
        const outline = [
                vector(0 * inch, topStep),
                vector(xm, topStep),
                vector(xm, midStep),
                vector(xf, midStep),
                vector(xf, RAIL_FOOT_Z),
                vector(RAIL_FOOT_X, RAIL_FOOT_Z),
                vector(RAIL_CUTBACK_X, RAIL_CUTBACK_Z),
                vector(0 * inch, RAIL_BACK_Z)
            ];
        var holes = [b3Slot(vector(HAND_SLOT_X, HAND_SLOT_Z), HAND_SLOT_L, HAND_SLOT_H)];
        for (var i = 0; i < size(CLIP_RIVET_X); i += 1)
        {
            holes = append(holes, b3Hole(CLIP_RIVET_X[i], CLIP_RIVET_Z, RIVET_HOLE_DIA));
        }
        b3PrismXZ(context, id + "plate", outline, holes, y0, y1);

        // Flanges along the bed-side, cut-back, bottom, foot and front edges, each
        // a strip PLATE_T wide inside its edge, turned outward.
        const fy1 = y1 + RAIL_FLANGE;
        const edges = [
                vector(0 * inch, RAIL_BACK_FLANGE_TOP),
                vector(0 * inch, RAIL_BACK_Z),
                vector(RAIL_CUTBACK_X, RAIL_CUTBACK_Z),
                vector(RAIL_FOOT_X, RAIL_FOOT_Z),
                vector(xf, RAIL_FOOT_Z),
                vector(xf, RAIL_FRONT_FLANGE_TOP)
            ];
        for (var i = 0; i + 1 < size(edges); i += 1)
        {
            b3PrismXZ(context, id + ("flange" ~ toString(i)), b3Strip(edges[i], edges[i + 1], PLATE_T), [], y0, fy1);
        }
        // Where the edge turns away from the plate (the cut-back's lower corner), two
        // strips touch only along a line, which Onshape can't join. Fill the V between them.
        for (var i = 1; i + 1 < size(edges); i += 1)
        {
            const d0 = edges[i] - edges[i - 1];
            const d1 = edges[i + 1] - edges[i];
            if (d0[0] * d1[1] - d0[1] * d1[0] < 0 * inch * inch)
            {
                const before = b3Strip(edges[i - 1], edges[i], PLATE_T);
                const after = b3Strip(edges[i], edges[i + 1], PLATE_T);
                b3PrismXZ(context, id + ("flangeCorner" ~ toString(i)), [edges[i], before[2], after[3]], [], y0, fy1);
            }
        }

        // Pivot ears for both treads.
        b3RailEars(context, id + "earsTop", TREAD_TOP);
        b3RailEars(context, id + "earsMiddle", TREAD_MIDDLE);

        b3Union(context, id + "join", b3Solids(id));
        const rail = b3Solids(id);
        const name = "Outer rail";
        b3Finish(context, rail, name, RGB_ALU);
        b3Mc(context, id + "mcTop", rail, name, "Top tread axle", b3OutAxleJoint(TREAD_TOP));
        b3Mc(context, id + "mcMiddle", rail, name, "Middle tread axle", b3OutAxleJoint(TREAD_MIDDLE));
        b3Mc(context, id + "mcClip", rail, name, "Step clip", b3StepClipJoint());
    });

// Two hook-shaped ears at tread n, bent toward the cabinet, in (y, z).
function b3RailEars(context is Context, id is Id, n is number)
{
    const top = b3T(n);
    const axZ = top - OUT_AXLE_DROP;
    const ear = b3EarOutline(RAIL_Y + PLATE_T, RAIL_Y - EAR_W, RAIL_Y - HOOK_U0,
        top - RAIL_EAR_ROOT_LO, top - RAIL_EAR_ROOT_HI, OUT_AXLE_Y, axZ);
    const holes = [b3Hole(OUT_AXLE_Y, axZ, HOLE_DIA)];
    const ranges = b3EarRanges(n);
    b3PrismYZ(context, id + "back", ear, holes, ranges[0][0], ranges[0][1]);
    b3PrismYZ(context, id + "front", ear, holes, ranges[1][0], ranges[1][1]);
}
