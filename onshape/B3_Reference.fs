FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Reference: simple blocks for the surroundings, to check clearances.
 * The cabinet with its side panel, the bed-end wall, the mattress on the
 * cabover floor, the cab door handle, the floor, and the room: the camper's
 * side wall, the dinette seat across the entrance aisle and the shower wall
 * across the middle aisle. Their names start with "REF". Add this feature
 * in a Part Studio of its own ("Surroundings"), so the stair's Part Studio
 * holds only the parts to make. The cabinet carries the mate connectors for
 * the brackets, the step clip, the step ledge and the two panel bumpers.
 */
annotation { "Feature Type Name" : "B3 Reference" }
export const b3Reference = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const far = DINETTE_X0 + DINETTE_T + 6 * inch;
        const back = SIDE_WALL_Y;
        const wallX = -WALL_GAP;
        b3Ref(context, id + "floor", vector(wallX, back, -0.5 * inch), vector(far, SHOWER_Y, 0 * inch), "REF floor");
        b3Ref(context, id + "cabinet", vector(wallX, back, 0 * inch), vector(wallX + CABINET_DEPTH, 0 * inch, CABINET_TOP_Z), "REF cabinet");
        b3Ref(context, id + "wall", vector(wallX - 1 * inch, 0 * inch, 0 * inch), vector(wallX, SHOWER_Y, BED_PLATFORM_Z), "REF bed-end wall");
        b3Ref(context, id + "mattress", vector(wallX - 30 * inch, back, BED_PLATFORM_Z), vector(wallX, SHOWER_Y, BED_PLATFORM_Z + MATTRESS_THICK), "REF mattress");
        b3Ref(context, id + "sideWall", vector(wallX, back - 1 * inch, 0 * inch), vector(far, back, BED_PLATFORM_Z), "REF side wall");
        b3Ref(context, id + "dinette", vector(DINETTE_X0, back, 0 * inch), vector(DINETTE_X0 + DINETTE_T, DINETTE_Y1, DINETTE_H), "REF dinette seat");
        b3Ref(context, id + "shower", vector(wallX, SHOWER_Y, 0 * inch), vector(far, SHOWER_Y + 1 * inch, BED_PLATFORM_Z), "REF shower wall");
        b3Ref(context, id + "handle", vector(wallX, HANDLE_Y, HANDLE_Z - HANDLE_H / 2), vector(wallX + HANDLE_PROJECTION, HANDLE_Y + HANDLE_W, HANDLE_Z + HANDLE_H / 2), "REF door handle");

        const cabinet = b3Solids(id + "cabinet");
        b3Mc(context, id + "mcBracketTop", cabinet, "REF cabinet", "Bracket, top tread", b3BracketJoint(TREAD_TOP));
        b3Mc(context, id + "mcBracketMiddle", cabinet, "REF cabinet", "Bracket, middle tread", b3BracketJoint(TREAD_MIDDLE));
        b3Mc(context, id + "mcStepClip", cabinet, "REF cabinet", "Step clip", b3StepClipMountJoint());
        b3Mc(context, id + "mcLedge", cabinet, "REF cabinet", "Step ledge", b3StepLedgeJoint());
        b3Mc(context, id + "mcStowBracket", cabinet, "REF cabinet", "Stow bracket", b3StowBracketJoint());
        b3Mc(context, id + "mcPlunger", cabinet, "REF cabinet", "Plunger", b3LockPlungerJoint());
        b3Mc(context, id + "mcBumperTop", cabinet, "REF cabinet", "Bumper, top tread", b3BumperJoint(TREAD_TOP));
        b3Mc(context, id + "mcBumperMiddle", cabinet, "REF cabinet", "Bumper, middle tread", b3BumperJoint(TREAD_MIDDLE));
    });

function b3Ref(context is Context, id is Id, lo is Vector, hi is Vector, partName is string)
{
    b3Box(context, id, lo, hi);
    setProperty(context, { "entities" : b3Solids(id), "propertyType" : PropertyType.NAME, "value" : partName });
    setProperty(context, { "entities" : b3Solids(id), "propertyType" : PropertyType.APPEARANCE,
                "value" : color(RGB_REF[0], RGB_REF[1], RGB_REF[2]) });
}
