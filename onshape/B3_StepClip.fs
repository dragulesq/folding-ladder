FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Step Clip, 1/8" sheet-metal part, used twice: on the cabinet's front
 * post, and upside down inside the outer rail. A 3 1/8" tall mounting leg
 * with two M6 holes, one above the other on the middle of the front post's
 * slot, and a 2 1/8" tall keyhole leg bent out at the stair's front end. The
 * keyhole takes the hook pin on the back end of one of the hook-on step's
 * axles; it has a slot above and below its hole, so the part is the same
 * either way up. The mounting leg runs 1/2" past the bend at each end, with a
 * bend relief there. Built where the cabinet's clip sits. On the cabinet: M6
 * screws into the post's T-nuts. On the rail: M6 button heads and nyloc nuts.
 */
annotation { "Feature Type Name" : "B3 Step Clip" }
export const b3StepClip = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const x0 = 2 * GOING - CLIP_MOUNT_L;
        const x1 = 2 * GOING;
        const zc = CLIP_CAB_Z;
        const mh = CLIP_MOUNT_H / 2;
        const kh = CLIP_KEY_H / 2;
        const r = CLIP_CORNER_R;

        // Mounting leg, on the panel, with the two screw holes.
        const screws = [b3Hole(CLIP_HOLE_X, zc - CLIP_HOLE_DZ, SCREW_HOLE_DIA), b3Hole(CLIP_HOLE_X, zc + CLIP_HOLE_DZ, SCREW_HOLE_DIA)];
        b3PrismXZ(context, id + "mountLeg", b3Rounded(b3Rect(x0, zc - mh, x1, zc + mh), [r, 0, 0, r]), screws, 0 * inch, SHEET_T);

        // Keyhole leg, outer face flush with the stair's front end.
        b3PrismYZ(context, id + "keyLeg", b3Rounded(b3Rect(0 * inch, zc - kh, CLIP_KEY_LEG, zc + kh), [0, r, r, 0]),
            [b3DoubleKeyhole(CAB_AXLE_Y, zc, CLIP_PIN_DROP + CLIP_SLOT_DROP)], x1 - SHEET_T, x1);
        b3Union(context, id + "join", b3Solids(id));

        // Bend reliefs in the mounting leg, where it runs on past each end of the bend.
        b3PrismXZ(context, id + "reliefTop", b3ReliefPath(vector(x1, zc + kh), vector(0, 1), vector(-1, 0), SHEET_T, BEND_R_SHEET, K_SHEET,
                mh - kh + 0.05 * inch), [], -0.05 * inch, SHEET_T + 0.05 * inch);
        b3PrismXZ(context, id + "reliefBottom", b3ReliefPath(vector(x1, zc - kh), vector(0, -1), vector(-1, 0), SHEET_T, BEND_R_SHEET, K_SHEET,
                mh - kh + 0.05 * inch), [], -0.05 * inch, SHEET_T + 0.05 * inch);
        const reliefs = qUnion([b3Solids(id + "reliefTop"), b3Solids(id + "reliefBottom")]);
        b3Subtract(context, id + "cutRelief", qSubtraction(b3Solids(id), reliefs), reliefs);

        // Sheet metal: the face on the panel, then the keyhole leg's outer face.
        b3SheetMetal(context, id + "sm", b3Solids(id), [
                    vector(x0 + 0.3 * inch, 0 * inch, zc),
                    vector(x1, CLIP_KEY_LEG - 0.3 * inch, zc)
                ], SHEET_T, BEND_R_SHEET, K_SHEET);

        const clip = b3Solids(id);
        b3Finish(context, clip, "Step clip", RGB_ALU_DARK);
        b3Mc(context, id + "mcMount", clip, "Step clip", "Mount", b3StepClipMountJoint());
        b3Mc(context, id + "mcStep", clip, "Step clip", "Hook-on step", b3CabAxleJoint(HOOK_STEP));
        for (var k = 0; k < 2; k += 1)
        {
            b3Mc(context, id + ("mcScrew" ~ k), clip, "Step clip", "Screw " ~ (k + 1), b3ClipScrewJoint(k));
        }
    });
