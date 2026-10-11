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
 * Follows FOLD_ANGLE.
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

        // Flanges, each a strip PLATE_T wide inside its edge, turned outward.
        const fy1 = y1 + RAIL_FLANGE;
        const pBack = vector(0 * inch, RAIL_BACK_Z);
        const pCut = vector(RAIL_CUTBACK_X, RAIL_CUTBACK_Z);
        const pFoot = vector(RAIL_FOOT_X, RAIL_FOOT_Z);
        const pFront = vector(xf, RAIL_FOOT_Z);
        b3PrismXZ(context, id + "flangeBack", b3Strip(vector(0 * inch, RAIL_BACK_FLANGE_TOP), pBack, PLATE_T), [], y0, fy1);
        b3PrismXZ(context, id + "flangeCutback", b3Strip(pBack, pCut, PLATE_T), [], y0, fy1);
        b3PrismXZ(context, id + "flangeBottom", b3Strip(pCut, pFoot, PLATE_T), [], y0, fy1);
        b3PrismXZ(context, id + "flangeFoot", b3Strip(pFoot, pFront, PLATE_T), [], y0, fy1);
        b3PrismXZ(context, id + "flangeFront", b3Strip(pFront, vector(xf, RAIL_FRONT_FLANGE_TOP), PLATE_T), [], y0, fy1);

        // Pivot ears for both treads.
        b3RailEars(context, id + "earsTop", TREAD_TOP);
        b3RailEars(context, id + "earsMiddle", TREAD_MIDDLE);

        b3Union(context, id + "join", b3Solids(id));
        const rail = b3Solids(id);
        opTransform(context, id + "fold", { "bodies" : rail, "transform" : b3RailFold() });
        b3Finish(context, rail, "Outer rail", RGB_ALU);
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
