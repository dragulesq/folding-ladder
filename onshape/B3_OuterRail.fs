FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Outer Rail: 3/16" stepped plate with a 3/4" flange turned outward along
 * the bed-side, bottom and front edges (1" along the foot), a hand slot, and
 * two hook-shaped pivot ears per tread bent inward. The front flange carries
 * the holes for the hook-on step's outer studs. Follows FOLD_ANGLE.
 *
 * The bed-side edge is cut back below the top step so the rail and its
 * flange pass the cab door handle.
 *
 * Bends are modeled sharp; add bend reliefs where the flanges meet.
 */
annotation { "Feature Type Name" : "B3 Outer Rail" }
export const b3OuterRail = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const x0 = b3RailX0();
        const yb = b3TreadBackY(TREAD_TOP);
        const ym = yb + TREAD_GOING;
        const yf = ym + TREAD_GOING;
        const topStep = b3TreadTopZ(TREAD_TOP) - RAIL_STEP_DROP;
        const midStep = b3TreadTopZ(TREAD_MIDDLE) - RAIL_STEP_DROP;

        // Plate outline in (Y, Z), counterclockwise so the flanges offset inward.
        const pA = vector(yb, topStep);
        const pB = vector(yb, topStep - RAIL_BACK_DROP);
        const pB2 = vector(yb + RAIL_CUTBACK, RAIL_CUTBACK_Z);   // cut back to clear the door handle
        const pC = vector(yf - RAIL_FOOT_LEN, RAIL_FOOT_Z);
        const pD = vector(yf, RAIL_FOOT_Z);
        const pE = vector(yf, midStep);
        const pF = vector(ym, midStep);
        const pG = vector(ym, topStep);
        const slot = b3Slot(vector(yb + HAND_SLOT_IN, HAND_SLOT_Z), HAND_SLOT_W, HAND_SLOT_H);
        b3Prism(context, id + "plate", b3FrameYZ(x0), [pA, pB, pB2, pC, pD, pE, pF, pG], [slot], PLATE_T);

        // Flanges, turned outward (+X).
        b3Prism(context, id + "flangeBack", b3FrameYZ(x0), b3Strip(pA, pB, PLATE_T), [], PLATE_T + RAIL_FLANGE);
        b3Prism(context, id + "flangeCutback", b3FrameYZ(x0), b3Strip(pB, pB2, PLATE_T), [], PLATE_T + RAIL_FLANGE);
        b3Prism(context, id + "flangeBottom", b3FrameYZ(x0), b3Strip(pB2, pC, PLATE_T), [], PLATE_T + RAIL_FLANGE);
        b3Prism(context, id + "flangeFoot", b3FrameYZ(x0), b3Strip(pC, pD, PLATE_T), [], PLATE_T + RAIL_FOOT_FLANGE);
        b3Prism(context, id + "flangeFront", b3FrameYZ(x0), b3Strip(pD, pE, PLATE_T), [], PLATE_T + RAIL_FLANGE);

        // Pivot ears for both treads.
        b3RailEars(context, id + "earsTop", TREAD_TOP);
        b3RailEars(context, id + "earsMiddle", TREAD_MIDDLE);

        b3Union(context, id + "join", b3Solids(id));

        // Holes for the hook-on step's outer studs, through the front flange.
        b3CylY(context, id + "stud1", b3OutStudX(), STUD_Z1, yf - PLATE_T - 0.1 * inch, yf + 0.1 * inch, STUD_HOLE_DIA / 2);
        b3CylY(context, id + "stud2", b3OutStudX(), STUD_Z2, yf - PLATE_T - 0.1 * inch, yf + 0.1 * inch, STUD_HOLE_DIA / 2);
        const studs = qUnion([b3Solids(id + "stud1"), b3Solids(id + "stud2")]);
        b3Subtract(context, id + "studHoles", qSubtraction(b3Solids(id), studs), studs);
        const rail = b3Solids(id);

        opTransform(context, id + "fold", { "bodies" : rail, "transform" : b3RailFold() });
        b3Finish(context, rail, "Outer rail", RGB_ALU);
    });

// Two hook-shaped ears at tread n, bent inward from the rail, in (X, Z).
function b3RailEars(context is Context, id is Id, n is number)
{
    const top = b3TreadTopZ(n);
    const yb = b3TreadBackY(n);
    const yf = yb + TREAD_GOING;
    const xr = b3RailX0() + PLATE_T;
    const outAx = b3OutAxle(n);
    const ax = outAx[0];
    const az = outAx[2];
    const r = EAR_HOOK_R;
    const ear = [
            vector(xr, top - RAIL_EAR_BOTTOM_DROP),
            vector(ax - r, top - RAIL_EAR_BOTTOM_DROP),
            vector(ax - r, az),
            { "mid" : vector(ax, az + r), "end" : vector(ax + r, az) },
            vector(ax + r, top - RAIL_NOTCH_DROP),
            vector(xr, top - RAIL_NOTCH_DROP)
        ];
    const holes = [{ "center" : vector(ax, az), "radius" : BUSHING_OD / 2 }];
    b3Prism(context, id + "back", b3FrameXZ(yb + EAR_INSET + PLATE_T), ear, holes, PLATE_T);
    b3Prism(context, id + "front", b3FrameXZ(yf - EAR_INSET), ear, holes, PLATE_T);
}
