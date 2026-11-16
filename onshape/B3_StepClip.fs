FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Step Clip, 1/8" sheet-metal part: small angle bolted inside the outer
 * rail with two M6 button-head screws and nyloc nuts. Its keyhole takes the
 * hook pin on the back end of the hook-on step's outer axle. The free corners
 * of both legs are rounded.
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
        const r = CLIP_CORNER_R;

        // Rail leg, against the rail's inner face, with the screw holes.
        var screws = [];
        for (var k = 0; k < size(CLIP_SCREW_X); k += 1)
        {
            screws = append(screws, b3Hole(CLIP_SCREW_X[k], CLIP_SCREW_Z, SCREW_HOLE_DIA));
        }
        b3PrismXZ(context, id + "railLeg", b3Rounded(b3Rect(CLIP_X0, z0, x1, z1), [r, 0, 0, r]), screws, RAIL_Y - SHEET_T, RAIL_Y);

        // Keyhole leg, outer face flush with x = 12.
        b3PrismYZ(context, id + "keyLeg", b3Rounded(b3Rect(CLIP_KEY_LEG_Y0, z0, RAIL_Y, z1), [r, 0, 0, r]),
            [b3Keyhole(CLIP_KEYHOLE_Y, CLIP_KEYHOLE_Z, CLIP_KEYHOLE_END_Z)], x1 - SHEET_T, x1);
        b3Union(context, id + "join", b3Solids(id));

        // Sheet metal: the face against the rail, then the keyhole leg's outer face.
        b3SheetMetal(context, id + "sm", b3Solids(id), [
                    vector((CLIP_SCREW_X[0] + CLIP_SCREW_X[1]) / 2, RAIL_Y, z0 + CLIP_H / 4),
                    vector(x1, (CLIP_KEY_LEG_Y0 + CLIP_KEYHOLE_Y) / 2, z0 + CLIP_H / 4)
                ], SHEET_T, BEND_R_SHEET, K_SHEET);

        const clip = b3Solids(id);
        b3Finish(context, clip, "Step clip", RGB_ALU_DARK);
        b3Mc(context, id + "mcRail", clip, "Step clip", "Outer rail", b3StepClipJoint());
        for (var k = 0; k < size(CLIP_SCREW_X); k += 1)
        {
            b3Mc(context, id + ("mcScrew" ~ k), clip, "Step clip", "Screw " ~ (k + 1), b3ClipScrewJoint(k));
        }
    });
