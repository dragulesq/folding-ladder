FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Reference: simple blocks for the surroundings, to check clearances.
 * The cabinet with its side panel, the bed-end wall, the mattress, the cab
 * door handle and the floor. Their names start with "REF"; hide them or
 * leave them out of exports.
 */
annotation { "Feature Type Name" : "B3 Reference" }
export const b3Reference = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const far = 30 * inch;
        b3Ref(context, id + "floor", vector(-CABINET_WIDTH, -far, -0.5 * inch), vector(far, far, 0 * inch), "REF floor");
        b3Ref(context, id + "cabinet", vector(-CABINET_WIDTH, 0 * inch, 0 * inch), vector(0 * inch, CABINET_DEPTH, CABINET_TOP_Z), "REF cabinet");
        b3Ref(context, id + "wall", vector(0 * inch, -1 * inch, 0 * inch), vector(far, 0 * inch, BED_PLATFORM_Z), "REF bed-end wall");
        b3Ref(context, id + "mattress", vector(-CABINET_WIDTH, -far, BED_PLATFORM_Z), vector(far, 0 * inch, BED_PLATFORM_Z + MATTRESS_THICK), "REF mattress");
        b3Ref(context, id + "handle", vector(HANDLE_X, 0 * inch, HANDLE_Z - 0.5 * inch), vector(HANDLE_X + 4 * inch, HANDLE_PROJECTION, HANDLE_Z + 0.5 * inch), "REF door handle");
    });

function b3Ref(context is Context, id is Id, lo is Vector, hi is Vector, partName is string)
{
    b3Box(context, id, lo, hi);
    setProperty(context, { "entities" : b3Solids(id), "propertyType" : PropertyType.NAME, "value" : partName });
    setProperty(context, { "entities" : b3Solids(id), "propertyType" : PropertyType.APPEARANCE,
                "value" : color(RGB_REF[0], RGB_REF[1], RGB_REF[2]) });
}
