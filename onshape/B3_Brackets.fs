FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Cabinet Brackets (2), 3/16" sheet. The 5 1/8 x 2 web sits flat on the
 * panel with four M6 holes (countersinks not modeled); a hook-shaped ear is
 * bent 90 degrees toward the room at each end to carry the tread's cabinet
 * axle and its stop pin.
 */
annotation { "Feature Type Name" : "B3 Cabinet Brackets" }
export const b3CabinetBrackets = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        b3Bracket(context, id + "top", TREAD_TOP, "Bracket, top tread");
        b3Bracket(context, id + "middle", TREAD_MIDDLE, "Bracket, middle tread");
    });

function b3Bracket(context is Context, id is Id, n is number, partName is string)
{
    const top = b3T(n);
    const xg = b3GoingX(n);
    const faces = b3EarFaces(n);
    const webTop = top - BRACKET_TOP_DROP;
    const webBot = webTop - BRACKET_H;

    // Web, flat on the panel, with the screw row.
    var screws = [];
    for (var k = 0; k < size(SCREW_X); k += 1)
    {
        screws = append(screws, b3Hole(xg + SCREW_X[k], top - SCREW_DROP, SCREW_HOLE_DIA));
    }
    b3PrismXZ(context, id + "web", b3Rect(faces[0], webBot, faces[1], webTop), screws, 0 * inch, PLATE_T);

    // Ears, in (y, z).
    const axZ = top - CAB_AXLE_DROP;
    const ear = b3EarOutline(0 * inch, PLATE_T + EAR_W, PLATE_T + HOOK_U0, webBot, webTop, CAB_AXLE_Y, axZ);
    const earHoles = [
            b3Hole(CAB_AXLE_Y, axZ, HOLE_DIA),
            b3Hole(STOP_PIN_Y, top - STOP_PIN_DROP, STOP_PIN_DIA)
        ];
    const ranges = b3EarRanges(n);
    b3PrismYZ(context, id + "earBack", ear, earHoles, ranges[0][0], ranges[0][1]);
    b3PrismYZ(context, id + "earFront", ear, earHoles, ranges[1][0], ranges[1][1]);

    b3Union(context, id + "join", b3Solids(id));
    b3Finish(context, b3Solids(id), partName, RGB_ALU_DARK);
}
