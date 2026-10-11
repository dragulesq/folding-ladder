FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Cabinet Brackets: one per tread, 3/16" plate. The web screws flat to the
 * panel with a row of M6 holes; a hook-shaped ear bends out at each end to
 * carry the tread's cabinet axle and its level stop pin.
 *
 * Flat in reality, the ears are modeled as sharp 90 degree bends.
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
    const top = b3TreadTopZ(n);
    const yb = b3TreadBackY(n);
    const yf = yb + TREAD_GOING;
    const y0 = yb + EAR_INSET;           // web ends = ears' outer faces
    const y1 = yf - EAR_INSET;
    const webTop = top - BRACKET_DROP;
    const webBot = webTop - BRACKET_H;

    // Web, flat on the panel, with the screw row.
    var screws = [];
    for (var k = 0; k < SCREW_COUNT; k += 1)
    {
        const y = (y0 + y1) / 2 + (k - (SCREW_COUNT - 1) / 2) * SCREW_PITCH;
        screws = append(screws, { "center" : vector(y, top - SCREW_DROP), "radius" : SCREW_HOLE_DIA / 2 });
    }
    const web = [vector(y0, webBot), vector(y1, webBot), vector(y1, webTop), vector(y0, webTop)];
    b3Prism(context, id + "web", b3FrameYZ(0 * inch), web, screws, PLATE_T);

    // Ears, in (X, Z): full height at the web, then a hook over the axle.
    const ax = PLATE_T + CAB_AXLE_OUT;
    const az = top - CAB_AXLE_DROP;
    const r = EAR_HOOK_R;
    const ear = [
            vector(0 * inch, webBot),
            vector(ax + r, webBot),
            vector(ax + r, az),
            { "mid" : vector(ax, az + r), "end" : vector(ax - r, az) },
            vector(ax - r, webTop),
            vector(0 * inch, webTop)
        ];
    const earHoles = [
            { "center" : vector(ax, az), "radius" : BUSHING_OD / 2 },
            { "center" : vector(ax + STOP_PIN_OUT, top - STOP_PIN_DROP), "radius" : STOP_PIN_DIA / 2 }
        ];
    b3Prism(context, id + "earBack", b3FrameXZ(y0 + PLATE_T), ear, earHoles, PLATE_T);
    b3Prism(context, id + "earFront", b3FrameXZ(y1), ear, earHoles, PLATE_T);

    b3Union(context, id + "join", b3Solids(id));
    b3Finish(context, b3Solids(id), partName, RGB_ALU_DARK);
}
