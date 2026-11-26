FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar
// and pick "B3 Lib".
// import(path : "B3 Lib", version : "");
// </B3-IMPORT>

/*
 * B3 Bushings and Pads, one of each bought-in soft part, so the assembly
 * shows every pivot's full stack and every rubber part:
 *   Lip bushing (16 used): flanged plastic bushing in each tread lip, flange
 *   outside the lip, and in each step leg ear, flange toward the lip.
 *   Ear bushing (8): the longer one in each bracket ear, flange toward the lip.
 *   Thrust washer (12): nylon (modeled with PTFE material), against the inside of each lip.
 *   Wave washer (6): one per axle, in the back gap between the thrust washer
 *   and the ear bushing's flange, drawn as a flat ring squeezed to that gap.
 *   Neoprene washer (2): on each step pin, behind the back lip's bushing.
 *   Rail foot pad (2), step leg pad (2), ledge strip (1): rubber under the
 *   rail's foot and the step legs' feet, and on the step ledge's shelf.
 *   Panel bumper (2): rubber, on the panel behind each folded tread.
 * The pivot parts are built on the back end of the top tread's cabinet axle,
 * the neoprene washer on the step's cabinet-side pin, each pad where it is
 * used first. Their sizes come from the tube (AXLE_DIA, BUSHING_WALL) and the
 * catalog sizes in B3 Parameters.
 */
annotation { "Feature Type Name" : "B3 Bushings and Pads" }
export const b3BushingsAndPads = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        // The stack along the top tread's cabinet axle, from its bed end: lip
        // bushing flange, back lip, thrust washer, wave washer, ear bushing flange,
        // back ear.
        const axle = b3CabAxleJoint(TREAD_TOP);
        const y = axle.origin[1];
        const z = axle.origin[2];
        const lipOut = b3GoingX(TREAD_TOP) + TRAY_INSET;
        const lipIn = lipOut + SHEET_T;
        const earOut = b3EarFaces(TREAD_TOP)[0];

        b3Bushing(context, id + "lipBushing", y, z, lipOut, LIP_BUSHING_LEN);
        const lipBushing = b3Solids(id + "lipBushing");
        b3FinishBushing(context, lipBushing, "Lip bushing");
        b3Mc(context, id + "mcLipBushing", lipBushing, "Lip bushing", "Axle", axle);
        b3Mc(context, id + "mcLipBushingLeg", lipBushing, "Lip bushing", "Leg ear", b3LegEarBushingJoint());

        b3Bushing(context, id + "earBushing", y, z, earOut, EAR_BUSHING_LEN);
        const earBushing = b3Solids(id + "earBushing");
        b3FinishBushing(context, earBushing, "Ear bushing");
        b3Mc(context, id + "mcEarBushing", earBushing, "Ear bushing", "Axle", axle);

        const waveX0 = lipIn + THRUST_T;
        const waveX1 = earOut - BUSHING_FLANGE_T;
        if (waveX1 - waveX0 < 0.2 * millimeter)
        {
            throw regenError("B3: the thrust washer and the ear bushing's flange leave no room for a wave washer");
        }
        b3TubeX(context, id + "thrust", y, z, lipIn, waveX0, THRUST_OD, AXLE_DIA);
        const thrust = b3Solids(id + "thrust");
        b3FinishPtfe(context, thrust, "Thrust washer");
        b3Mc(context, id + "mcThrust", thrust, "Thrust washer", "Axle", axle);

        b3TubeX(context, id + "wave", y, z, waveX0, waveX1, WAVE_OD, WAVE_ID);
        const wave = b3Solids(id + "wave");
        b3FinishSteel(context, wave, "Wave washer");
        b3Mc(context, id + "mcWave", wave, "Wave washer", "Axle", axle);

        // Neoprene washer on the step's cabinet-side pin, between the back lip's
        // bushing flange and the keyhole plate.
        const pin = b3CabAxleJoint(HOOK_STEP);
        const neoX1 = b3GoingX(HOOK_STEP) + TRAY_INSET - BUSHING_FLANGE_T;
        b3TubeX(context, id + "neoprene", pin.origin[1], pin.origin[2], neoX1 - NEO_T, neoX1, NEO_OD, NEO_ID);
        const neoprene = b3Solids(id + "neoprene");
        b3FinishRubber(context, neoprene, "Neoprene washer");
        b3Mc(context, id + "mcNeoprene", neoprene, "Neoprene washer", "Tread", pin);

        // Rubber pad under the flat of the rail's foot flange, down to the floor;
        // two of them, either side of the travel lock's hole.
        const railPad = b3RailPadJoint(1);
        const rp = railPad.origin;
        b3Box(context, id + "railPad", vector(rp[0], rp[1], rp[2] - RAIL_PAD_T), vector(rp[0] + RAIL_PAD_L, rp[1] + RAIL_PAD_W, rp[2]));
        b3FinishRubber(context, b3Solids(id + "railPad"), "Rail foot pad");
        b3Mc(context, id + "mcRailPad", b3Solids(id + "railPad"), "Rail foot pad", "Rail", railPad);

        // Rubber pad under the cabinet-side step leg's foot flange.
        const stepPad = b3StepPadJoint();
        const sp = stepPad.origin;
        b3Box(context, id + "stepPad", vector(sp[0] - STEP_PAD_L / 2, STEP_LEG_Y0, sp[2] - STEP_PAD_T),
            vector(sp[0] + STEP_PAD_L / 2, STEP_LEG_Y0 + STEP_LEG_FLANGE, sp[2]));
        b3FinishRubber(context, b3Solids(id + "stepPad"), "Step leg pad");
        b3Mc(context, id + "mcStepPad", b3Solids(id + "stepPad"), "Step leg pad", "Leg", stepPad);

        // Rubber strip on the step ledge's shelf, from past the back wall's bend.
        const shelf = b3Rounded(b3Rect(0 * inch, LEDGE_STRIP_Y0, LEDGE_LEN, LEDGE_SHELF), [0, 0, 0, LEDGE_SHELF_R]);
        b3PrismXY(context, id + "ledgeStrip", shelf, [], LEDGE_TOP_Z, LEDGE_TOP_Z + LEDGE_STRIP_T);
        b3FinishRubber(context, b3Solids(id + "ledgeStrip"), "Ledge strip");
        b3Mc(context, id + "mcLedgeStrip", b3Solids(id + "ledgeStrip"), "Ledge strip", "Ledge", b3LedgeStripJoint());

        // Rubber bumper on the panel behind the folded top tread, at its free height.
        const bumper = b3BumperJoint(TREAD_TOP);
        const bp = bumper.origin;
        fCylinder(context, id + "bumper", { "bottomCenter" : b3V(bp), "topCenter" : b3V(bp + vector(0 * inch, BUMPER_H, 0 * inch)),
                    "radius" : BUMPER_DIA / 2 });
        b3FinishRubber(context, b3Solids(id + "bumper"), "Panel bumper");
        b3Mc(context, id + "mcBumper", b3Solids(id + "bumper"), "Panel bumper", "Panel", bumper);
    });

// A flanged bushing on an axle along x: the flange sits against the face at
// seat, on its -x side, and the sleeve runs on into the hole. len is the
// bushing's overall length, flange included.
function b3Bushing(context is Context, id is Id, y is ValueWithUnits, z is ValueWithUnits, seat is ValueWithUnits, len is ValueWithUnits)
{
    const x0 = seat - BUSHING_FLANGE_T;
    b3CylX(context, id + "flange", y, z, x0, seat, BUSHING_FLANGE_DIA);
    b3CylX(context, id + "sleeve", y, z, x0, x0 + len, HOLE_DIA);
    const body = qUnion([b3Solids(id + "flange"), b3Solids(id + "sleeve")]);
    b3Union(context, id + "join", body);
    b3CylX(context, id + "bore", y, z, x0 - 1 * millimeter, x0 + len + 1 * millimeter, AXLE_DIA);
    b3Subtract(context, id + "cut", body, b3Solids(id + "bore"));
}
