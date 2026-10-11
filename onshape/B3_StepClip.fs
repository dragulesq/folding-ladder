FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Step Clip, 1/8" sheet: small angle riveted inside the outer rail. Its
 * keyhole takes the hook pin on the back end of the hook-on step's outer
 * leg axle. Moves with the rail, so it follows FOLD_ANGLE.
 */
annotation { "Feature Type Name" : "B3 Step Clip" }
export const b3StepClip = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const x1 = 2 * GOING;
        const z0 = CLIP_Z0;
        const z1 = CLIP_Z0 + CLIP_H;

        // Rail leg, against the rail's inner face, with the rivet holes.
        var rivets = [];
        for (var i = 0; i < size(CLIP_RIVET_X); i += 1)
        {
            rivets = append(rivets, b3Hole(CLIP_RIVET_X[i], CLIP_RIVET_Z, RIVET_HOLE_DIA));
        }
        b3PrismXZ(context, id + "railLeg", b3Rect(CLIP_X0, z0, x1, z1), rivets, RAIL_Y - SHEET_T, RAIL_Y);

        // Keyhole leg, outer face flush with x = 12.
        b3PrismYZ(context, id + "keyLeg", b3Rect(CLIP_KEY_LEG_Y0, z0, RAIL_Y, z1),
            [b3Keyhole(CLIP_KEYHOLE_Y, CLIP_KEYHOLE_Z, CLIP_KEYHOLE_END_Z)], x1 - SHEET_T, x1);

        b3Union(context, id + "join", b3Solids(id));
        const clip = b3Solids(id);
        opTransform(context, id + "fold", { "bodies" : clip, "transform" : b3RailFold() });
        b3Finish(context, clip, "Step clip", RGB_ALU_DARK);
    });
