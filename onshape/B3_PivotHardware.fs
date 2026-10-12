FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Pivot Hardware, one of each part, for checking fit and folded
 * clearances in the assembly:
 *   Tread axle (4 used) and step axle (2 used; the back end is the hook
 *   pin), 12 mm aluminum tube with a 5 mm bore tapped M6 at both ends.
 *   M6 fender washer (12, one on each axle end) and M6 button-head screw (14:
 *   one on each axle end, and two bolting the step clip to the outer rail).
 *   Stop pin (4 used), 3/16" pin through each cabinet bracket ear.
 *   M6 x 16 flat-head screw (8), bolting the two rail brackets inside the
 *   outer rail, and M6 nyloc nut (10), on the rail's outer face for those and
 *   the step clip's screws.
 * Each is built where its first use sits: the top tread's cabinet axle, the
 * step's cabinet-side axle, the washer and screw on the top tread's cabinet
 * axle at the bed end, the top cabinet bracket's back stop pin, and the
 * first screw of the top rail bracket with its nut.
 * Bushings, thrust washers and wave washers aren't modeled.
 */
annotation { "Feature Type Name" : "B3 Pivot Hardware" }
export const b3PivotHardware = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const tread = b3CabAxleJoint(TREAD_TOP);
        b3Axle(context, id + "treadAxle", tread, TREAD_AXLE_LEN, "Tread axle");
        b3Axle(context, id + "stepAxle", b3StepCabAxleJoint(), STEP_AXLE_X1 - STEP_AXLE_X0, "Step axle");
        // Where the step axle meets the step's tray (a tread), for placing it on either pivot.
        b3Mc(context, id + "mcStepTread", b3Solids(id + "stepAxle"), "Step axle", "Tread", b3CabAxleJoint(HOOK_STEP));

        // Washer and screw head, stacked out from the axle's bed-end tip.
        const p = tread.origin;
        const x0 = p[0];
        const w = x0 - END_WASHER_T;
        b3CylX(context, id + "washer", p[1], p[2], w, x0, END_WASHER_DIA);
        const washer = b3Solids(id + "washer");
        b3FinishSteel(context, washer, "M6 fender washer");
        b3Mc(context, id + "mcWasherAxle", washer, "M6 fender washer", "Axle", tread);
        const under = b3AxleJoint(w, p[1], p[2]);
        b3Mc(context, id + "mcWasherScrew", washer, "M6 fender washer", "Screw", under);

        b3CylX(context, id + "screw", p[1], p[2], w - END_HEAD_H, w, END_HEAD_DIA);
        const screw = b3Solids(id + "screw");
        b3FinishSteel(context, screw, "M6 button-head screw");
        b3Mc(context, id + "mcScrew", screw, "M6 button-head screw", "Washer", under);

        // Stop pin: flush with the bracket ear's inside face, sticking out under the lip.
        const pin = b3StopPinJoint(TREAD_TOP, false);
        const q = pin.origin;
        b3CylX(context, id + "pin", q[1], q[2], q[0] - PLATE_T - STOP_PIN_OUT, q[0], STOP_PIN_DIA);
        b3FinishSteel(context, b3Solids(id + "pin"), "Stop pin");
        b3Mc(context, id + "mcPin", b3Solids(id + "pin"), "Stop pin", "Bracket", pin);

        // Flat-head screw: 90 degree head flush with the bracket web's ear side, shank through the rail.
        const head = b3OnRail(TREAD_TOP, b3BracketScrewJoint(TREAD_TOP, 0));
        const s0 = head.origin;
        const along = head.zAxis;
        const headH = (FH_HEAD_DIA - FH_DIA) / 2;
        fCone(context, id + "fh" + "head", { "bottomCenter" : b3V(s0), "topCenter" : b3V(s0 + along * headH),
                    "bottomRadius" : FH_HEAD_DIA / 2, "topRadius" : FH_DIA / 2 });
        fCylinder(context, id + "fh" + "shank", { "bottomCenter" : b3V(s0 + along * headH), "topCenter" : b3V(s0 + along * FH_LEN),
                    "radius" : FH_DIA / 2 });
        b3Union(context, id + "fh" + "join", b3Solids(id + "fh"));
        const fh = b3Solids(id + "fh");
        b3FinishSteel(context, fh, "M6 flat-head screw");
        b3Mc(context, id + "mcFh", fh, "M6 flat-head screw", "Head", head);

        // Nyloc nut on the rail's outer face, a hex prism around the screw.
        const nutJoint = b3RailNutJoint(TREAD_TOP, 0);
        const c = nutJoint.origin;
        var hex = [];
        for (var i = 0; i < 6; i += 1)
        {
            const a = (30 + 60 * i) * degree;
            hex = append(hex, vector(c[0] + NUT_AF / sqrt(3) * cos(a), c[2] + NUT_AF / sqrt(3) * sin(a)));
        }
        b3PrismXZ(context, id + "nut", hex, [b3Hole(c[0], c[2], FH_DIA)], c[1], c[1] + NUT_H);
        b3FinishSteel(context, b3Solids(id + "nut"), "M6 nyloc nut");
        b3Mc(context, id + "mcNut", b3Solids(id + "nut"), "M6 nyloc nut", "Rail", nutJoint);
    });

// A tube axle of length len from the joint at its bed-end tip, with a mate
// connector on each tip pointing out of the tube.
function b3Axle(context is Context, id is Id, tip is map, len is ValueWithUnits, partName is string)
{
    const p = tip.origin;
    b3TubeX(context, id + "tube", p[1], p[2], p[0], p[0] + len, AXLE_DIA, AXLE_BORE);
    const tube = b3Solids(id + "tube");
    b3FinishTube(context, tube, partName);
    b3Mc(context, id + "mcBack", tube, partName, "Back end", tip);
    b3Mc(context, id + "mcFront", tube, partName, "Front end",
        b3Joint(vector(p[0] + len, p[1], p[2]), vector(1, 0, 0), vector(0, 1, 0)));
}
