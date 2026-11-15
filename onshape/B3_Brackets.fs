FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Bracket, 3/16" sheet-metal part, used four times. The 5 1/8 x 2 web
 * has four M6 holes (countersinks not modeled); a hook-shaped ear is bent 90
 * degrees off each end to carry a tread axle. Two sit on the cabinet panel,
 * ears toward the room, with a stop pin through each ear. The other two are
 * the same part turned to face the cabinet and bolted inside the outer rail
 * with M6 x 20 flat-head screws and nyloc nuts; their stop-pin holes stay
 * empty. The ears run the web's full height and the hook starts past the
 * bend, so the bends need no extra reliefs. Built once, where the top
 * tread's cabinet bracket sits.
 */
annotation { "Feature Type Name" : "B3 Bracket" }
export const b3CabinetBrackets = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const n = TREAD_TOP;
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

        // Sheet metal: the web's panel face, then each ear's outer face.
        const earY = (PLATE_T + HOOK_U0) / 2;
        const earZ = webBot + BRACKET_H / 4;
        b3SheetMetal(context, id + "sm", b3Solids(id), [
                    vector(xg + GOING / 2, 0 * inch, earZ),
                    vector(faces[0], earY, earZ),
                    vector(faces[1], earY, earZ)
                ], PLATE_T, BEND_R_PLATE, K_PLATE);

        const body = b3Solids(id);
        const name = "Bracket";
        b3Finish(context, body, name, RGB_ALU_DARK);
        b3Mc(context, id + "mcCabinet", body, name, "Mounting face", b3BracketJoint(n));
        b3Mc(context, id + "mcAxle", body, name, "Axle", b3CabAxleJoint(n));
        b3Mc(context, id + "mcAxleTurned", body, name, "Axle, turned", b3BracketAxleTurnedJoint(n));
        b3Mc(context, id + "mcPinBack", body, name, "Stop pin, back", b3StopPinJoint(n, false));
        b3Mc(context, id + "mcPinFront", body, name, "Stop pin, front", b3StopPinJoint(n, true));
        for (var k = 0; k < size(SCREW_X); k += 1)
        {
            b3Mc(context, id + ("mcScrew" ~ k), body, name, "Screw " ~ (k + 1), b3BracketScrewJoint(n, k));
        }
    });
