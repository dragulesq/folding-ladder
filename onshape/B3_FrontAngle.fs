FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Front Angle: 1/8" angle screwed to the cabinet's front post. The leg on
 * the panel takes the M6 screws and stops short of the middle bracket; the
 * outward leg sits flush with the front edge, carries the hook-on step's
 * cabinet-side studs and runs up to the travel-latch keeper.
 */
annotation { "Feature Type Name" : "B3 Front Angle" }
export const b3FrontAngle = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const yf = b3StairFrontY();
        const over = 0.1 * inch;

        b3Box(context, id + "panelLeg", vector(0 * inch, yf - FA_PANEL_LEG, FA_BOTTOM_Z), vector(SHEET_T, yf, FA_PANEL_LEG_TOP));
        b3Box(context, id + "outLeg", vector(0 * inch, yf - SHEET_T, FA_BOTTOM_Z), vector(FA_OUT_LEG, yf, FA_TOP_Z));
        b3Union(context, id + "join", b3Solids(id));

        // M6 screws into the front post.
        for (var i = 0; i < size(FA_SCREW_Z); i += 1)
        {
            b3CylX(context, id + ("screw" ~ toString(i)), yf - FA_SCREW_INSET, FA_SCREW_Z[i], -over, SHEET_T + over, SCREW_HOLE_DIA / 2);
        }
        // Hook-on step studs and the latch keeper, through the outward leg.
        b3CylY(context, id + "stud1", CAB_STUD_X, STUD_Z1, yf - SHEET_T - over, yf + over, STUD_HOLE_DIA / 2);
        b3CylY(context, id + "stud2", CAB_STUD_X, STUD_Z2, yf - SHEET_T - over, yf + over, STUD_HOLE_DIA / 2);
        b3CylY(context, id + "keeper", CAB_STUD_X, KEEPER_Z, yf - SHEET_T - over, yf + over, STUD_HOLE_DIA / 2);

        const fa = qUnion([b3Solids(id + "panelLeg"), b3Solids(id + "outLeg")]);
        const cutters = qSubtraction(b3Solids(id), fa);
        b3Subtract(context, id + "holes", fa, cutters);
        b3Finish(context, b3Solids(id), "Front angle", RGB_ALU_DARK);
    });
