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
 *   x  along the cabinet toward its front edge; the stair spans x = 0 to 12
 *   y  out from the panel face into the room
 *   z  up from the floor
 * All parts are built where they sit when the stair is open.
 *
 * Sizes are to the outside of bends. Bends are modeled sharp; the page's
 * inside radii are BEND_R_SHEET, BEND_R_PLATE and BEND_R_LIP for the
 * sheet-metal conversion.
 */

// ---------------------------------------------------------------------------
// Room and cabinet (reference only)
// ---------------------------------------------------------------------------
export const CABINET_TOP_Z = 48 * inch;            // folded stair tops out at 47 9/16
export const BED_PLATFORM_Z = 47.75 * inch;
export const MATTRESS_THICK = 4 * inch;
export const WALL_GAP = 0.5 * inch;                // bed-end wall to the stair's bed end
export const CABINET_DEPTH = 12.5 * inch;          // along x, wall to front post
export const CABINET_WIDTH = 13 * inch;            // behind the side panel
export const HANDLE_Y = 19 * inch;                 // cab door handle's closest point to the panel
export const HANDLE_Z = 18 * inch;
export const HANDLE_PROJECTION = 2.5 * inch;       // out from the bed-end wall

// ---------------------------------------------------------------------------
// Layout and material
// ---------------------------------------------------------------------------
export const RISE = 10.25 * inch;                  // tread tops T1, T2, T3 = 1x, 2x, 3x RISE
export const GOING = 6 * inch;                     // top tread x 0-6, middle 6-12, step 12-18
export const SHEET_T = (1 / 8) * inch;             // treads, step tray, front angle, step clip, ledge
export const PLATE_T = (3 / 16) * inch;            // outer rail, brackets, step legs
export const BEND_R_SHEET = (1 / 8) * inch;        // inside bend radius on 1/8" sheet
export const BEND_R_PLATE = (3 / 16) * inch;       // inside bend radius on 3/16" sheet
export const BEND_R_LIP = (3 / 16) * inch;         // tread and step lips, on 1/8" sheet
export const K_FACTOR = 0.42;
export const ALU_DENSITY = 2.68 * gram / (centimeter ^ 3);   // 5052-H32
export const STEEL_DENSITY = 8.0 * gram / (centimeter ^ 3);  // 304 stainless

// ---------------------------------------------------------------------------
// Treads and step tray
// ---------------------------------------------------------------------------
export const TRAY_W = 18 * inch;                   // along y
export const TRAY_Y0 = (3 / 8) * inch;             // tray's cabinet-side edge
export const TRAY_D = (45 / 8) * inch;             // 5 5/8 along x
export const TRAY_INSET = (3 / 16) * inch;         // tray starts this far into its going
export const LIP = 2 * inch;                       // lips bent down at the x ends
export const LIP_CORNER_R = 0.5 * inch;            // relief on each lip's bottom outer corner (treads only)

// ---------------------------------------------------------------------------
// Pivots
// ---------------------------------------------------------------------------
export const CAB_AXLE_Y = (15 / 16) * inch;
export const CAB_AXLE_DROP = (5 / 8) * inch;       // below T
export const OUT_AXLE_Y = 17.75 * inch;
export const OUT_AXLE_DROP = 1.5 * inch;           // below T
export const HOLE_DIA = (7 / 16) * inch;           // bushing bore, reamed (cut 3/8 pilots)
export const EAR_X0 = (7 / 16) * inch;             // ear outer faces at EAR_X0 and GOING - EAR_X0 in each going
export const EAR_HOOK_R = (7 / 16) * inch;         // round top of every hook, on its axle hole
export const EAR_W = (19 / 16) * inch;             // ear reach, u 0 to 1 3/16 (brackets and rail)
export const HOOK_U0 = (5 / 16) * inch;            // hook part of each ear starts at u = 5/16
export const AXLE_DIA = (5 / 16) * inch;
export const TREAD_AXLE_X0 = 0 * inch;             // tread axles run x 0 to 6 in each going
export const TREAD_AXLE_LEN = 6 * inch;
export const GROOVE_DIA = 0.250 * inch;            // E-clip grooves on every axle (not modeled)
export const GROOVE_W = 0.029 * inch;
export const TREAD_GROOVE_SPAN = 5.70 * inch;      // groove inner edges apart, 0.15 in from each end

// ---------------------------------------------------------------------------
// Cabinet brackets
// ---------------------------------------------------------------------------
export const BRACKET_TOP_DROP = (13 / 8) * inch;   // web top at T - 1 5/8
export const BRACKET_H = 2 * inch;                 // web bottom at T - 3 5/8
export const SCREW_X = [1.5 * inch, 2.5 * inch, 3.5 * inch, 4.5 * inch];   // within the going
export const SCREW_DROP = (17 / 8) * inch;         // screw row at T - 2 1/8
export const SCREW_HOLE_DIA = 6.6 * millimeter;    // M6 clearance
export const SCREW_CSK_DIA = 12.4 * millimeter;    // 90 degree countersink, room side (not modeled)
export const STOP_PIN_Y = (17 / 16) * inch;
export const STOP_PIN_DROP = (67 / 32) * inch;     // T - 2 3/32
export const STOP_PIN_DIA = (3 / 16) * inch;
export const STOP_PIN_OUT = (5 / 16) * inch;       // sticks out under the lip; flush on the ear's inside

// ---------------------------------------------------------------------------
// Outer rail
// ---------------------------------------------------------------------------
export const RAIL_Y = 18.5 * inch;                 // inner face
export const RAIL_STEP_DROP = 2 * inch;            // step edges at T - 2 (28 3/4 and 18 1/2)
export const RAIL_BACK_Z = 23.5 * inch;            // bed-side edge, bottom of the straight part
export const RAIL_CUTBACK_X = 3 * inch;            // cut-back for the door handle reaches (3, 20)
export const RAIL_CUTBACK_Z = 20 * inch;
export const RAIL_FOOT_X = 8.25 * inch;            // foot runs x 8 1/4 to 12
export const RAIL_FOOT_Z = 0.5 * inch;
export const RAIL_FLANGE = 0.75 * inch;            // outward, beyond the plate (out to y 19 7/16)
export const RAIL_BACK_FLANGE_TOP = (217 / 8) * inch;    // 27 1/8: stops short of the top ears
export const RAIL_FRONT_FLANGE_TOP = 17 * inch;          // stops short of the middle ears
export const RAIL_EAR_ROOT_LO = (25 / 8) * inch;   // ear root (1" bend line) from T - 3 1/8
export const RAIL_EAR_ROOT_HI = (17 / 8) * inch;   //   to T - 2 1/8
export const HAND_SLOT_X = 3 * inch;
export const HAND_SLOT_Z = 25 * inch;
export const HAND_SLOT_L = 4 * inch;               // along x
export const HAND_SLOT_H = 1.25 * inch;
export const CLIP_RIVET_X = [(83 / 8) * inch, 11.25 * inch];   // 10 3/8 and 11 1/4
export const CLIP_RIVET_Z = 9.5 * inch;
export const RIVET_HOLE_DIA = 0.196 * inch;        // #9 drill

// ---------------------------------------------------------------------------
// Keyholes (front angle and step clip)
// ---------------------------------------------------------------------------
export const KEYHOLE_DIA = (11 / 16) * inch;
export const KEYHOLE_SLOT_W = (11 / 32) * inch;

// ---------------------------------------------------------------------------
// Front angle
// ---------------------------------------------------------------------------
export const FA_X1 = 12 * inch;                    // flush with the cabinet's front edge
export const FA_PANEL_LEG_X0 = (85 / 8) * inch;    // 10 5/8
export const FA_PANEL_LEG_TOP = 16.5 * inch;
export const FA_OUT_LEG = 2 * inch;                // along y
export const FA_BOTTOM_Z = 1 * inch;
export const FA_TOP_Z = 18 * inch;
export const FA_SCREW_X = (365 / 32) * inch;       // 11 13/32, front post centerline
export const FA_SCREW_Z = [4.5 * inch, 12.25 * inch, 15.5 * inch];
export const FA_SCREW_HOLE_DIA = (17 / 64) * inch;
export const FA_KEYHOLE_Y = (19 / 16) * inch;
export const FA_KEYHOLE_Z = 10 * inch;             // hole center
export const FA_KEYHOLE_END_Z = (151 / 16) * inch; // 9 7/16, slot's round end

// ---------------------------------------------------------------------------
// Step clip (riveted inside the outer rail)
// ---------------------------------------------------------------------------
export const CLIP_X0 = (79 / 8) * inch;            // 9 7/8; outer face at x 12
export const CLIP_Z0 = 8.5 * inch;
export const CLIP_H = 2 * inch;
export const CLIP_KEY_LEG_Y0 = (131 / 8) * inch;   // 16 3/8
export const CLIP_KEYHOLE_Y = (281 / 16) * inch;   // 17 9/16
export const CLIP_KEYHOLE_Z = (155 / 16) * inch;   // 9 11/16
export const CLIP_KEYHOLE_END_Z = (73 / 8) * inch; // 9 1/8

// ---------------------------------------------------------------------------
// Step ledge
// ---------------------------------------------------------------------------
export const LEDGE_LEN = 6 * inch;                 // x 0 to 6
export const LEDGE_SHELF = 2.5 * inch;             // along y
export const LEDGE_TOP_Z = (9 / 8) * inch;
export const LEDGE_LEG_H = 1 * inch;
export const LEDGE_SCREW_X = [1.5 * inch, 4.5 * inch];
export const LEDGE_SCREW_Z = (5 / 8) * inch;
export const LEDGE_SCREW_DIA = 0.177 * inch;       // #8 clearance (countersink not modeled)

// ---------------------------------------------------------------------------
// Hook-on step
// ---------------------------------------------------------------------------
export const STEP_CAB_AXLE_Y = (19 / 16) * inch;
export const STEP_CAB_AXLE_Z = (153 / 16) * inch;  // 9 9/16
export const STEP_OUT_AXLE_Y = (281 / 16) * inch;  // 17 9/16
export const STEP_OUT_AXLE_Z = 9.25 * inch;
export const STEP_LEG_CAB_Y0 = (3 / 8) * inch;     // cabinet leg web, outer face
export const STEP_LEG_OUT_Y1 = (147 / 8) * inch;   // 18 3/8, outer leg web, outer face
export const STEP_LEG_BOTTOM_Z = (3 / 8) * inch;
export const STEP_LEG_TOP_Z = (81 / 8) * inch;     // 10 1/8, stops against the skin's underside
export const STEP_EAR_W = (17 / 16) * inch;        // ear reach from the web's inner face
export const STEP_EAR_DROP = 1 * inch;             // ear's straight part, from axle z - 1 to axle z
export const STEP_AXLE_X0 = 11.72 * inch;          // back end is the hook pin
export const STEP_AXLE_X1 = 17.97 * inch;          // 6.25 long
export const STEP_GROOVE_SPAN = 6.01 * inch;       // groove inner edges apart, 0.12 in from each end

// ---------------------------------------------------------------------------
// Fold check
// ---------------------------------------------------------------------------
// 0 degrees = open and in use, 90 degrees = folded flat against the cabinet.
// The treads, outer rail, step clip and outer axles move; the rest stays put.
export const FOLD_ANGLE = 0 * degree;
