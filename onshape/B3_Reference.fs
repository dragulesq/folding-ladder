FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Reference: simple blocks for the surroundings, to check clearances.
 * The cabinet with its side panel, the bed-end wall, the mattress, the cab
 * door handle and the floor. Their names start with "REF". Add this feature
 * in a Part Studio of its own ("Surroundings"), so the stair's Part Studio
 * holds only the parts to make. The cabinet carries the mate connectors for
 * the brackets, the front angle and the step ledge.
 */
annotation { "Feature Type Name" : "B3 Reference" }
export const b3Reference = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const far = 30 * inch;
        const wallX = -WALL_GAP;
        b3Ref(context, id + "floor", vector(-far, -CABINET_WIDTH, -0.5 * inch), vector(far, far, 0 * inch), "REF floor");
        b3Ref(context, id + "cabinet", vector(wallX, -CABINET_WIDTH, 0 * inch), vector(wallX + CABINET_DEPTH, 0 * inch, CABINET_TOP_Z), "REF cabinet");
        b3Ref(context, id + "wall", vector(wallX - 1 * inch, 0 * inch, 0 * inch), vector(wallX, far, BED_PLATFORM_Z), "REF bed-end wall");
        b3Ref(context, id + "mattress", vector(-far, -CABINET_WIDTH, BED_PLATFORM_Z), vector(wallX, far, BED_PLATFORM_Z + MATTRESS_THICK), "REF mattress");
        b3Ref(context, id + "handle", vector(wallX, HANDLE_Y, HANDLE_Z - 0.5 * inch), vector(wallX + HANDLE_PROJECTION, HANDLE_Y + 4 * inch, HANDLE_Z + 0.5 * inch), "REF door handle");

        const cabinet = b3Solids(id + "cabinet");
        b3Mc(context, id + "mcBracketTop", cabinet, "REF cabinet", "Bracket, top tread", b3BracketJoint(TREAD_TOP));
        b3Mc(context, id + "mcBracketMiddle", cabinet, "REF cabinet", "Bracket, middle tread", b3BracketJoint(TREAD_MIDDLE));
        b3Mc(context, id + "mcFrontAngle", cabinet, "REF cabinet", "Front angle", b3FrontAngleJoint());
        b3Mc(context, id + "mcLedge", cabinet, "REF cabinet", "Step ledge", b3StepLedgeJoint());
    });

function b3Ref(context is Context, id is Id, lo is Vector, hi is Vector, partName is string)
{
    b3Box(context, id, lo, hi);
    setProperty(context, { "entities" : b3Solids(id), "propertyType" : PropertyType.NAME, "value" : partName });
    setProperty(context, { "entities" : b3Solids(id), "propertyType" : PropertyType.APPEARANCE,
                "value" : color(RGB_REF[0], RGB_REF[1], RGB_REF[2]) });
}
