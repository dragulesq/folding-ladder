FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

/*
 * B3 folding bed stair: global parameters.
 *
 * Every dimension the part features use lives here. Change a number, and
 * every Part Studio that uses the B3 features rebuilds to match. Names and
 * values follow the "Part definitions" section of the Bed Stair B3 page.
 *
 * Datum (same as the B3 page). Origin on the floor, at the side panel's face,
 * at the stair's bed end (the end the top tread is on).
 *   x  along the cabinet toward its front edge; the stair spans x = 0 to 11 1/2
 *   y  out from the panel face into the room (Onshape's -Y)
 *   z  up from the floor
 * Each part is built once, where its first use sits when the stair is open;
 * the assembly places the rest.
 *
 * Sizes are to the outside of bends. Every bent part is an Onshape
 * sheet-metal part with SendCutSend's bend radius and K-factor for 5052 in
 * its thickness, so its flat pattern is ready to export for them.
 */

// ---------------------------------------------------------------------------
// Room and cabinet (reference only)
// ---------------------------------------------------------------------------
export const CABINET_TOP_Z = 47 * inch;            // measured, top of the 8020 frame; folded stair tops out at 46 9/16
export const BED_PLATFORM_Z = 47 * inch;           // measured, cabover floor; the mattress lies on it
export const MATTRESS_THICK = 4 * inch;
export const WALL_GAP = 0.5 * inch;                // bed-end wall to the stair's bed end
export const CABINET_DEPTH = 12 * inch;            // measured, along x, wall to the front of the 8020 frame
export const CABINET_WIDTH = 29.25 * inch;         // measured, the 8020 frame behind the side panel, out to the camper's side wall
export const PANEL_T = 0.25 * inch;                 // Plyboo side panel over the 8020 frame
export const HANDLE_Y = (157 / 8) * inch - PANEL_T; // cab door handle's closest point to the panel: measured 19 5/8 from the 8020, so 19 3/8
export const HANDLE_Z = 22 * inch;                 // measured, taken as the center
export const HANDLE_W = 4.25 * inch;               // measured, along y
export const HANDLE_H = 0.75 * inch;               // measured, along z
// The room around the stair, for mock-up. The entrance aisle runs from the
// camper door, past the cabinet's front, to the middle aisle; the dinette is
// across it from the cabinet. The middle aisle runs along the camper between
// the dinette and the shower. The side wall is taken to be right behind the
// cabinet's frame.
export const ENTRY_AISLE_W = (221 / 8) * inch;     // measured 27 5/8, cabinet front to the dinette, along x
export const DINETTE_DEPTH = 35.25 * inch;         // measured, side wall to the middle aisle, along y
export const DINETTE_H = 47 * inch;                // measured, dinette seat's height at the entrance aisle
export const DINETTE_T = 4.5 * inch;               // measured, its thickness along x
export const HANDLE_TO_SHOWER = 8.75 * inch;       // measured, handle's far end to the shower door across the middle aisle
export const SIDE_WALL_Y = -(PANEL_T + CABINET_WIDTH);                // -29 1/2
export const DINETTE_Y1 = SIDE_WALL_Y + DINETTE_DEPTH;                // 5 3/4: the middle aisle's near edge
export const SHOWER_Y = HANDLE_Y + HANDLE_W + HANDLE_TO_SHOWER;       // 32 3/8: the middle aisle's far edge
export const DINETTE_X0 = CABINET_DEPTH - WALL_GAP + ENTRY_AISLE_W;   // 39 1/8
export const HANDLE_PROJECTION = 2 * inch;         // out from the bed-end wall, to x 1 1/2

// ---------------------------------------------------------------------------
// Layout and material
// ---------------------------------------------------------------------------
export const RISE = 10.25 * inch;                  // tread tops T1, T2, T3 = 1x, 2x, 3x RISE
export const GOING = 5.75 * inch;                  // top tread x 0 to 5 3/4, middle to 11 1/2, step to 17 1/4; two goings and WALL_GAP make CABINET_DEPTH
export const SHEET_T = (1 / 8) * inch;             // treads, front angle, step clip, ledge, step legs
export const PLATE_T = (3 / 16) * inch;            // outer rail, brackets

// Bends: SendCutSend's bending specs for 5052. They set one bend radius and
// K-factor per thickness and don't do custom radii, so the tread lips bend at
// the 1/8" sheet's radius too. Their bend deduction at 90 degrees
// (0.216" and 0.356") checks against these.
export const BEND_R_SHEET = 0.125 * inch;          // inside bend radius on .125" sheet
export const BEND_R_PLATE = 0.250 * inch;          // inside bend radius on .187" sheet
export const K_SHEET = 0.44;                       // K-factor on .125" sheet
export const K_PLATE = 0.43;                       // K-factor on .187" sheet
export const DIE_W_SHEET = 0.630 * inch;           // their die width for .125"
export const DIE_W_PLATE = 0.984 * inch;           // their die width for .187"
// Keep cut features at least half the die width from a bend line (0.315" on
// .125", 0.492" on .187"), measured on the flat pattern.
// Bend reliefs, where a bend stops short of an edge: SendCutSend wants them at
// least half the thickness wide and the bend radius plus the thickness plus
// 0.020" deep, measured on the flat pattern from the bend line (0.270" on
// .125", 0.457" on .187"). These are cut a full thickness wide. Past a relief,
// the bare edge sits RELIEF_CLEAR inside the bend line so no material lies on
// it. Where two flanges meet, the round corner relief has the same radius.
export const RELIEF_W_SCALE = 1;                   // relief width / thickness (their minimum is 0.5)
export const RELIEF_EXTRA = 0.020 * inch;          // relief depth beyond radius + thickness
export const RELIEF_CLEAR = (1 / 16) * inch;       // bare edge past a relief, inside the bend line

// Corner radii (outside corners keep hands and shins from catching).
export const R_OUTSIDE_LARGE = 0.5 * inch;         // rail and its flanges, trays' lips, ledge shelf, front angle's top outer corner
export const R_OUTSIDE_SMALL = 0.25 * inch;        // brackets, front angle, step clip, legs, ledge back leg
export const R_INSIDE_MIN = (1 / 8) * inch;        // smallest inside corner
export const ALU_DENSITY = 2.68 * gram / (centimeter ^ 3);   // 5052-H32
export const STEEL_DENSITY = 8.0 * gram / (centimeter ^ 3);  // 304 stainless
export const TUBE_DENSITY = 2.70 * gram / (centimeter ^ 3);  // 6061 axle tube
export const BUSHING_DENSITY = 1.46 * gram / (centimeter ^ 3);   // iglide G
export const PTFE_DENSITY = 2.2 * gram / (centimeter ^ 3);
export const RUBBER_DENSITY = 1.3 * gram / (centimeter ^ 3);     // neoprene

// ---------------------------------------------------------------------------
// Treads (three: top, middle, and the hook-on step's tray)
// ---------------------------------------------------------------------------
// 17" wide keeps the open stair inside the cab door handle: the rail's flange
// tips reach TRAY_W + 1 7/8 out from the panel (18 7/8), 1/2" short of
// HANDLE_Y, so the rail needs no notch around the handle. Everything across
// the stair (the outer axle line, the rail, the step clip) follows TRAY_W.
export const TRAY_W = 17 * inch;                   // along y
export const TRAY_Y0 = (3 / 8) * inch;             // tray's cabinet-side edge
export const TRAY_D = GOING - 2 * (3 / 16) * inch;  // 5 3/8 along x
export const TRAY_INSET = (3 / 16) * inch;         // tray starts this far into its going
export const LIP = 2 * inch;                       // lips bent down at the x ends
export const LIP_CORNER_R = R_OUTSIDE_LARGE;       // both bottom corners of each lip

// ---------------------------------------------------------------------------
// Pivots
// ---------------------------------------------------------------------------
// The tube and its bushings. Change AXLE_DIA to the tube's measured diameter
// and BUSHING_WALL to the bushing that fits it (1 mm for igus 12 x 14, 1/16"
// for a 1/2" x 5/8" inch bushing): the bores, bushings, thrust and wave
// washers and keyhole slots all follow.
export const AXLE_DIA = 12 * millimeter;           // aluminum tube on all six axles
export const AXLE_BORE = 5 * millimeter;           // tapped M6 at both ends
export const BUSHING_WALL = 1 * millimeter;        // bushing OD = tube + 2 walls
export const HOLE_DIA = AXLE_DIA + 2 * BUSHING_WALL;   // 14 mm bushing bore, reamed H7 (cut 3/8 pilots, bend, then open up)
export const CAB_AXLE_Y = (15 / 16) * inch;
export const CAB_AXLE_DROP = (5 / 8) * inch;       // below T
export const OUT_AXLE_INSET = (5 / 8) * inch;      // outer axle, in from the tray's outer edge
export const OUT_AXLE_Y = TRAY_Y0 + TRAY_W - OUT_AXLE_INSET;   // 16 3/4, the rail brackets' axle line
export const OUT_AXLE_DROP = 1.5 * inch;           // below T
export const EAR_X0 = (7 / 16) * inch;             // ear outer faces at EAR_X0 and GOING - EAR_X0 in each going
export const EAR_HOOK_R = (7 / 16) * inch;         // round top of every hook, on its axle hole
export const EAR_W = (19 / 16) * inch;             // bracket ear reach, u 0 to 1 3/16
export const HOOK_U0 = (5 / 16) * inch;            // hook part of each ear starts at u = 5/16
export const TREAD_AXLE_LEN = 138.85 * millimeter; // 5.47
export const TREAD_AXLE_X0 = (GOING - TREAD_AXLE_LEN) / 2;   // centered: x 0.14 to 5.61 in each going
// Each axle end: an M6 fender washer and an M6 button-head screw (ISO 7380).
export const END_WASHER_DIA = 18 * millimeter;
export const END_WASHER_T = 1.6 * millimeter;
export const END_HEAD_DIA = 10.5 * millimeter;
export const END_HEAD_H = 3.3 * millimeter;

// ---------------------------------------------------------------------------
// Pivot stack: bushings and washers on every axle. Sizes are the catalog
// parts' (igus iglide flanged bushings, a DIN 7603 size PTFE ring).
// ---------------------------------------------------------------------------
export const BUSHING_FLANGE_DIA = AXLE_DIA + 8 * millimeter;   // 20 mm flange (McMaster 2705T142 has 18 mm)
export const BUSHING_FLANGE_T = 1 * millimeter;
export const LIP_BUSHING_LEN = 4 * millimeter;     // overall, flange included: igus JFM-1214-04 (iglide J), in the 1/8" lips and step leg ears
export const EAR_BUSHING_LEN = 6 * millimeter;     // overall: GFM-1214-06, in the brackets' 3/16" ears
export const THRUST_OD = AXLE_DIA + 6 * millimeter;    // 18 mm PTFE washer; the most that sits flat inside a lip at a cabinet-side hole
export const THRUST_T = 1.5 * millimeter;
// Wave washer: McMaster 1775N12, split wave disc spring, one per axle at the
// back. It pushes the tread toward the front until the front lip's stack is
// solid, so it sits in both lip-to-ear gaps' worth of room. Drawn as a flat
// ring squeezed to the back gap with the tread centered.
export const WAVE_OD = 0.625 * inch;
export const WAVE_ID = 0.48 * inch;
export const WAVE_T = 0.01 * inch;                 // spring steel, doubled at the split's overlap
export const WAVE_FREE_H = 0.095 * inch;
export const WAVE_WORK_H = 0.05 * inch;            // at its rated 10 lb
// Step pins: a neoprene washer behind the back lip's bushing flange.
export const NEO_ID = 0.5 * inch;
export const NEO_OD = (7 / 8) * inch;
export const NEO_T = (1 / 8) * inch;

// ---------------------------------------------------------------------------
// Brackets: one part, used four times. Two on the cabinet; two turned
// around to face the cabinet and bolted inside the outer rail.
// ---------------------------------------------------------------------------
export const BRACKET_TOP_DROP = (13 / 8) * inch;   // web top at T - 1 5/8
export const BRACKET_H = 2 * inch;                 // web bottom at T - 3 5/8
export const SCREW_X = [GOING / 2 - 1.5 * inch, GOING / 2 - 0.5 * inch, GOING / 2 + 0.5 * inch, GOING / 2 + 1.5 * inch];   // within the going, 1" apart about its middle
export const SCREW_DROP = (17 / 8) * inch;         // screw row at T - 2 1/8
export const SCREW_HOLE_DIA = 6.6 * millimeter;    // M6 clearance
export const SCREW_CSK_DIA = 12.4 * millimeter;    // 90 degree countersink, ear side (not modeled)
export const STOP_PIN_Y = (17 / 16) * inch;
export const STOP_PIN_DROP = (67 / 32) * inch;     // T - 2 3/32
export const STOP_PIN_DIA = (3 / 16) * inch;
export const STOP_PIN_OUT = (5 / 16) * inch;       // sticks out under the lip; flush on the ear's inside
export const EAR_CORNER_R = R_OUTSIDE_SMALL;       // each ear's bottom outer corner
// The hook starts 1/16" past the end of the ear's 1/4" bend, so the notch's
// inside corner can only take a 1/32" radius and still stay clear of the bend.
export const EAR_NOTCH_R = (1 / 32) * inch;        // the hook notch
// Rail bracket screws: M6 x 20 flat head (ISO 10642) from the ear side, DIN 985 nyloc nut outside the rail.
export const FH_DIA = 6 * millimeter;
export const FH_LEN = 20 * millimeter;             // overall, head included; comes through the nut by about 4 mm
export const FH_HEAD_DIA = 12 * millimeter;        // 90 degree head
export const NUT_AF = 10 * millimeter;
export const NUT_H = 6 * millimeter;

// ---------------------------------------------------------------------------
// Outer rail
// ---------------------------------------------------------------------------
// The rail bracket is a cabinet bracket turned around: its web, against the
// rail's inner face, fills y 17 1/2 to 17 11/16, and its axle hole lands on
// the outer axle line.
export const RAIL_Y = OUT_AXLE_Y + CAB_AXLE_Y;     // 17 11/16, inner face (outer face 17 7/8)
export const RAIL_STEP_DROP = 2 * inch;            // step edges at T - 2 (28 3/4 and 18 1/2)
export const RAIL_BACK_Z = 23.5 * inch;            // bottom of the bed-side edge; the long lower edge runs from here straight to the foot
export const RAIL_FOOT_X = 2 * GOING - 3.75 * inch; // foot runs x 7 3/4 to 11 1/2
export const RAIL_FOOT_Z = 0.5 * inch;
export const RAIL_FLANGE = 1 * inch;               // outward, beyond the plate (out to y 18 7/8)
export const RAIL_FLANGE_TIP_R = R_OUTSIDE_LARGE;  // every flange's free corners
export const HAND_SLOT_X = 1.5 * GOING;            // centered under the middle tread's bracket, near the travel lock
export const HAND_SLOT_Z = 14.5 * inch;
export const HAND_SLOT_L = 4 * inch;               // along x
export const HAND_SLOT_H = 1.25 * inch;
// Step clip screws, M6 (SCREW_HOLE_DIA) through the clip and the rail. 11 1/8 is
// as close to the front flange as half a die width from its bend allows.
export const CLIP_SCREW_X = [2 * GOING - (13 / 8) * inch, 2 * GOING - (7 / 8) * inch];   // 9 7/8 and 10 5/8
export const CLIP_SCREW_Z = 9 * inch;
// Foot pad: rubber, glued under the foot flange's flat, standing on the floor.
export const RAIL_PAD_W = 0.75 * inch;             // the flange's flat width past its bend
export const RAIL_PAD_T = RAIL_FOOT_Z;             // 1/2; shim it so it is squeezed about 1/16

// ---------------------------------------------------------------------------
// Keyholes (front angle and step clip)
// ---------------------------------------------------------------------------
export const KEYHOLE_DIA = END_WASHER_DIA + 2 * millimeter;    // 20 mm, clears the pin's fender washer and button head
export const KEYHOLE_SLOT_W = AXLE_DIA + 1 * millimeter;      // 13 mm, clears the tube

// ---------------------------------------------------------------------------
// Front angle
// ---------------------------------------------------------------------------
export const FA_X1 = 2 * GOING;                    // 11 1/2, flush with the cabinet's front edge
export const FA_PANEL_LEG_X0 = FA_X1 - (11 / 8) * inch;    // 10 1/8
export const FA_PANEL_LEG_TOP = 16.5 * inch;
export const FA_OUT_LEG = 2 * inch;                // along y
export const FA_BOTTOM_Z = 1 * inch;
export const FA_TOP_Z = 18 * inch;
export const FA_SCREW_X = FA_X1 - (19 / 32) * inch;  // 10 29/32, front post centerline (15 mm in from the frame's front)
export const FA_SCREW_Z = [4.5 * inch, 12.25 * inch, 15.5 * inch];
export const FA_SCREW_HOLE_DIA = (17 / 64) * inch;
export const FA_KEYHOLE_Y = CAB_AXLE_Y;            // 15/16, on the step's cabinet-side pivot line
export const FA_KEYHOLE_Z = (161 / 16) * inch;     // 10 1/16, hole center
export const FA_KEYHOLE_END_Z = 9.5 * inch;        // slot's round end, 1/8 below the pin at 9 5/8
export const FA_CORNER_R = R_OUTSIDE_SMALL;
export const FA_ENTRY_R = R_OUTSIDE_LARGE;         // outward leg's top outer corner (y 2, z 18), facing the entry

// ---------------------------------------------------------------------------
// Step clip (bolted inside the outer rail with M6 button heads and nyloc nuts)
// ---------------------------------------------------------------------------
export const CLIP_X0 = 2 * GOING - (17 / 8) * inch; // 9 3/8; outer face at x 11 1/2
export const CLIP_Z0 = (63 / 8) * inch;            // 7 7/8
export const CLIP_H = (17 / 8) * inch;             // up to z 10
export const CLIP_KEY_LEG = (37 / 16) * inch;      // keyhole leg's reach in from the rail's inner face
export const CLIP_KEY_LEG_Y0 = RAIL_Y - CLIP_KEY_LEG;   // 15 3/8
export const CLIP_KEYHOLE_Y = OUT_AXLE_Y;          // 16 3/4, on the step's outer pivot line
export const CLIP_KEYHOLE_Z = (147 / 16) * inch;   // 9 3/16, hole center
export const CLIP_KEYHOLE_END_Z = (69 / 8) * inch; // 8 5/8, slot's round end, 1/8 below the pin at 8 3/4
export const CLIP_CORNER_R = R_OUTSIDE_SMALL;      // the free corners of both legs

// ---------------------------------------------------------------------------
// Travel lock
// ---------------------------------------------------------------------------
// Travel lock: McMaster 8501A58 twist-lock spring plunger (M10, 6 mm nose,
// 14 mm travel), pointing up through a small ledge on the panel, under where
// the rail's foot flange lands when the stair is folded. The folded rail's
// first move on the way open is straight out from the panel, square to the
// pin, so the pin in the flange's hole locks the fold. The handle hangs under
// the ledge.
export const FOLD_DY = -(CAB_AXLE_DROP - OUT_AXLE_DROP) - (OUT_AXLE_Y - CAB_AXLE_Y);   // the rail's move when folded: -14 15/16
export const FOLD_DZ = (OUT_AXLE_Y - CAB_AXLE_Y) - (CAB_AXLE_DROP - OUT_AXLE_DROP);    // +16 11/16
export const LOCK_X = (RAIL_FOOT_X + 2 * GOING) / 2;   // pin line: mid-foot, 9 5/8 along the cabinet
export const LOCK_Y = RAIL_Y + PLATE_T + RAIL_FLANGE - RAIL_PAD_W / 2;   // on the open rail: mid-flat of the foot flange, 18 1/2
export const LOCK_FOLD_Y = LOCK_Y + FOLD_DY;       // folded: 3 9/16 off the panel
export const LOCK_HOLE = 6.5 * millimeter;         // in the rail's foot flange, for the nose
export const LOCK_SHELF_HOLE = 10.5 * millimeter;  // M10 clearance in the ledge's shelf
export const PLUNGER_TAP = 8.5 * millimeter;       // M10 x 1.5 tap drill, in the block
export const PLUNGER_NOSE = 14 * millimeter;       // nose travel past the plunger's thread
export const PLUNGER_THREAD = 33 * millimeter;     // threaded length
export const LOCK_GAP = (3 / 16) * inch;           // block top to the folded flange; the foot comes in about 0.09 low
export const LOCK_BLOCK_T = 5 * millimeter;        // M10 x 1.5 thin jam nut on the shelf (a regular nut under it clamps the plunger)
export const LOCK_NUT_AF = 17 * millimeter;        // across its flats
export const LOCK_BLOCK_TOP_Z = RAIL_FOOT_Z + FOLD_DZ - LOCK_GAP;   // 17
export const LOCK_SHELF_TOP_Z = LOCK_BLOCK_TOP_Z - LOCK_BLOCK_T;    // 16.80, just under the cabinet bracket's web (16 7/8)
export const LOCK_LEDGE_W = 0.75 * inch;           // along x, between the two foot pads
export const LOCK_LEDGE_X0 = LOCK_X - LOCK_LEDGE_W / 2;             // 9 1/4
export const LOCK_LEDGE_REACH = RAIL_Y + PLATE_T + RAIL_FLANGE + FOLD_DY;   // 3 15/16, as far out as the folded rail's flange
export const LOCK_LEDGE_LEG = 1.75 * inch;         // panel leg, down from the shelf's top
export const LOCK_SCREW_DROP = [(5 / 8) * inch, (11 / 8) * inch];   // two #8 screws, one over the other, below the shelf's top
export const RAIL_PAD_GAP = LOCK_LEDGE_W + (1 / 4) * inch;          // the foot pads stop 1/8 short of the ledge each side
export const RAIL_PAD_L = (2 * GOING - RAIL_FOOT_X - RAIL_PAD_GAP) / 2;   // 1 3/8 each
export const RAIL_PAD_X0 = [RAIL_FOOT_X, 2 * GOING - RAIL_PAD_L];  // back pad, front pad

// ---------------------------------------------------------------------------
// Step ledge
// ---------------------------------------------------------------------------
export const LEDGE_LEN = GOING;                    // x 0 to 5 3/4
export const LEDGE_SHELF = 2.5 * inch;             // along y
export const LEDGE_TOP_Z = (9 / 8) * inch;
export const LEDGE_LEG_H = 1 * inch;
export const LEDGE_SCREW_X = [GOING / 2 - 1.5 * inch, GOING / 2 + 1.5 * inch];
export const LEDGE_SCREW_Z = 0.5 * inch;           // 1/2 rather than the page's 5/8 keeps the countersinks half a die width from the bend
export const LEDGE_SCREW_DIA = 0.177 * inch;       // #8 clearance (countersink not modeled)
export const LEDGE_SHELF_R = R_OUTSIDE_LARGE;      // the shelf's front corners, at ankle height
export const LEDGE_LEG_R = R_OUTSIDE_SMALL;        // the back leg's bottom corners
export const LEDGE_STRIP_T = (1 / 8) * inch;       // rubber strip on the shelf; the folded step stands on it
export const STOW_PANEL_GAP = (1 / 8) * inch;      // the folded step's back stands this far off the panel (drawings and the stowed assembly)

// ---------------------------------------------------------------------------
// Bumpers: rubber, stuck to the panel behind each folded tread. The folded
// tread tops sit 5/16" off the panel, so a 3/8" bumper is squeezed 1/16".
// ---------------------------------------------------------------------------
export const BUMPER_DIA = 0.5 * inch;
export const BUMPER_H = (3 / 8) * inch;
export const BUMPER_UP = 12 * inch;                // above the tread's cabinet axle, mid-depth in its going

// ---------------------------------------------------------------------------
// Hook-on step: its tray is the tread part, at T1 in the step's going, so its
// pivots sit on the treads' axle lines, at y 15/16, z 9 5/8 and y 16 3/4,
// z 8 3/4. The two legs are one part. The cabinet-side leg is built; the outer
// leg is the same part turned half a turn about a vertical axis, which puts
// its web's outer face at y 17 3/8.
// ---------------------------------------------------------------------------
export const STEP_LEG_Y0 = (5 / 16) * inch;        // cabinet-side leg's web, outer face
export const STEP_LEG_BOTTOM_Z = (1 / 8) * inch;   // underside of the foot flange
export const STEP_LEG_TOP_Z = (81 / 8) * inch;     // 10 1/8, stops against the skin's underside
export const STEP_LEG_FLANGE = (5 / 8) * inch;     // side and foot flanges, out from the web's outer face
export const STEP_LEG_EAR = (17 / 16) * inch;      // ears, out from the web's outer face
export const STEP_LEG_EAR_Z = (133 / 16) * inch;   // 8 5/16, where each side flange widens into its ear
export const STEP_LEG_EAR_R = (3 / 8) * inch;      // the ear's top outer corner
export const STEP_AXLE_X0 = 2 * GOING - 0.13 * inch;  // 11.37; back end is the hook pin, through the keyhole
export const STEP_AXLE_X1 = 3 * GOING - 0.14 * inch;  // 17.11; 145.8 mm (5.74) long
export const STEP_PAD_L = GOING - (9 / 8) * inch;   // 4 5/8 rubber pad under each leg's foot flange, as wide as the flange
export const STEP_PAD_T = STEP_LEG_BOTTOM_Z;       // 1/8, down to the floor

// ---------------------------------------------------------------------------
// Stowed hook-on step: two hook plates on the panel catch its two pins
// ---------------------------------------------------------------------------
// The stowed step stands in the top tread's going with its pins pointing to
// the cabinet front, just past x = GOING, where plates like the front angle's
// catch them. The lower plate's slot opens upward: the step's bottom end is
// set down on the ledge and its lower pin drops in. The upper plate's slot
// opens toward the room: the step's top is then tipped in to the panel and
// the upper pin slides in, held up and down by the slot. A turn tab across
// the slot's mouth keeps it in.
export const STOW_TOP_Y = STOW_PANEL_GAP + OUT_AXLE_DROP + CAB_AXLE_Y - STEP_LEG_Y0;   // the stowed step's tread face: 2 1/4
export const STOW_Z0 = LEDGE_TOP_Z + LEDGE_STRIP_T - TRAY_Y0;      // stowed height of the step's y = 0: 7/8
export const STOW_PIN_UP_Y = STOW_TOP_Y - OUT_AXLE_DROP;           // upper pin: 3/4 off the panel
export const STOW_PIN_UP_Z = STOW_Z0 + OUT_AXLE_Y;                 // 17 5/8
export const STOW_PIN_LO_Y = STOW_TOP_Y - CAB_AXLE_DROP;           // lower pin: 1 5/8
export const STOW_PIN_LO_Z = STOW_Z0 + CAB_AXLE_Y;                 // 1 13/16
export const STOW_HOOK_X0 = GOING;                 // plates from here to GOING + SHEET_T, where the front angle sits when the step is in use
export const STOW_SLOT_DROP = (1 / 8) * inch;      // each slot's round end past the resting pin
export const STOW_HOOK_LEG = 1.5 * inch;           // panel leg, along x past the plate
export const STOW_UP_Z0 = 15.25 * inch;            // upper hook: plate bottom; its panel leg runs under the cabinet bracket
export const STOW_UP_LEG_Z1 = 16.75 * inch;        // panel leg top, 1/8 under the bracket's web
export const STOW_UP_Z1 = 18.25 * inch;            // plate top, under the middle tread's back lip as it folds (18.39)
export const STOW_UP_D = 2 * inch;                 // plate reach from the panel
export const STOW_LO_Z0 = 1 * inch;                // lower hook
export const STOW_LO_Z1 = (21 / 8) * inch;
export const STOW_LO_D = 2.25 * inch;
export const STOW_SCREW_DIA = 0.177 * inch;        // #8 clearance
export const STOW_TAB_Y0 = 1.2 * inch;             // turn tab, closed: upright across the slot's mouth, past the pin's neoprene washer
export const STOW_TAB_Y1 = 1.4 * inch;
export const STOW_TAB_HALF = 0.6 * inch;           // it reaches this far above and below the pin
export const STOW_TAB_PIVOT_UP = 0.5 * inch;       // its screw, above the slot
export const STOW_PIN_X = GOING - TREAD_AXLE_X0;   // the stowed step's lower pin's tube end, where it hinges in the lower hook
