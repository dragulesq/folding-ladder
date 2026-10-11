FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

/*
 * B3 folding bed stair: global parameters.
 *
 * Every dimension the part features use lives here. Change a number, and
 * every Part Studio that uses the B3 features rebuilds to match.
 *
 * Coordinate system (all parts are built in their in-use position):
 *   X  out from the cabinet's side panel face into the room (panel face at X = 0)
 *   Y  along the stair's depth, from the bed-end wall (Y = 0) toward the
 *      cabinet's front post
 *   Z  up from the floor (Z = 0)
 *
 * Source: the "Bed Stair B3" design page from the project's Ladder step
 * spacing thread. Values marked "assumed" were not on that page and are
 * best guesses to check.
 */

// ---------------------------------------------------------------------------
// Room and cabinet
// ---------------------------------------------------------------------------
export const CABINET_TOP_Z = 48 * inch;            // cabinet top, including the 1/4" Plyboo top panel
export const BED_PLATFORM_Z = 47.75 * inch;        // bed platform height
export const MATTRESS_THICK = 4 * inch;            // mattress, uncompressed
export const CABINET_DEPTH = 12.5 * inch;          // along Y, bed-end wall to front post
export const CABINET_WIDTH = 13 * inch;            // along X, behind the side panel
export const PANEL_THICK = 0.5 * inch;             // side panel (Plyboo)
export const HANDLE_X = 19 * inch;                 // closest point of the cab door handle, from the panel face
export const HANDLE_Z = 18 * inch;                 // door handle height
export const HANDLE_PROJECTION = 2.5 * inch;       // how far the handle sticks out from the wall

// ---------------------------------------------------------------------------
// Stair layout
// ---------------------------------------------------------------------------
export const RISE = 10.25 * inch;                  // every rise; tread tops at 1x, 2x, 3x RISE
export const TREAD_GOING = 6 * inch;               // depth of each tread along Y
export const STAIR_Y0 = 0.5 * inch;                // gap from the bed-end wall to the back of the top tread
export const TREAD_WIDTH = 18 * inch;              // along X
export const PANEL_GAP = (3 / 8) * inch;           // tread's cabinet-side edge to the panel face

// ---------------------------------------------------------------------------
// Material
// ---------------------------------------------------------------------------
export const SHEET_T = (1 / 8) * inch;             // treads, hook-on step, front angle, ledge
export const PLATE_T = (3 / 16) * inch;            // outer rail and cabinet brackets
export const ALU_DENSITY = 2.68 * gram / (centimeter ^ 3);   // 5052-H32
export const STEEL_DENSITY = 8.0 * gram / (centimeter ^ 3);  // 304 stainless

// ---------------------------------------------------------------------------
// Treads
// ---------------------------------------------------------------------------
export const TREAD_LIP = 2 * inch;                 // front and back lips, bent down

// ---------------------------------------------------------------------------
// Pivots (axles run along Y, front to back)
// ---------------------------------------------------------------------------
export const CAB_AXLE_OUT = 0.75 * inch;           // cabinet axle, out from the bracket web's outer face
export const CAB_AXLE_DROP = (5 / 8) * inch;       // cabinet axle, below the tread top
export const OUT_AXLE_IN = 0.75 * inch;            // outer axle, in from the rail's inner face
export const OUT_AXLE_DROP = 1.5 * inch;           // outer axle, below the tread top
export const AXLE_DIA = (5 / 16) * inch;
export const AXLE_LEN = 6.25 * inch;
export const BUSHING_OD = (7 / 16) * inch;         // hole size for the flanged bushings
export const EAR_INSET = (7 / 16) * inch;          // tread lip's outer face to the ear's outer face
export const EAR_HOOK_R = 0.4 * inch;              // radius of the hook around each axle

// ---------------------------------------------------------------------------
// Cabinet brackets (one per tread)
// ---------------------------------------------------------------------------
export const BRACKET_DROP = (13 / 8) * inch;       // web's top edge below the tread top
export const BRACKET_H = 2 * inch;                 // web height
export const SCREW_DROP = (17 / 8) * inch;         // screw row below the tread top
export const SCREW_PITCH = 1 * inch;
export const SCREW_COUNT = 4;
export const SCREW_HOLE_DIA = 6.6 * millimeter;    // M6 clearance
export const STOP_PIN_DIA = (3 / 16) * inch;
export const STOP_PIN_OUT = 0.1 * inch;            // stop pin, outboard of the cabinet axle
export const STOP_PIN_DROP = 2.1 * inch;           // stop pin center, below the tread top

// ---------------------------------------------------------------------------
// Outer rail
// ---------------------------------------------------------------------------
export const RAIL_CLEAR = (1 / 8) * inch;          // tread's outer edge to the rail
export const RAIL_STEP_DROP = 2 * inch;            // rail's step edges below each tread top
export const RAIL_BACK_DROP = 5.25 * inch;         // bed-side edge drops this far from the top step
export const RAIL_CUTBACK = 3 * inch;              // then angles this far toward the front (door handle cut-back)
export const RAIL_CUTBACK_Z = 20 * inch;           // reaching this height off the floor, then on to the foot
export const RAIL_FOOT_LEN = 3.75 * inch;          // foot length along Y, at the front
export const RAIL_FOOT_Z = 0.5 * inch;             // rail bottom above the floor (rubber pad)
export const RAIL_FLANGE = 0.75 * inch;            // outward flange, beyond the plate
export const RAIL_FOOT_FLANGE = 1 * inch;          // wider flange along the foot
export const RAIL_EAR_BOTTOM_DROP = 3.1 * inch;    // rail ear's bottom edge below the tread top
export const RAIL_NOTCH_DROP = 2.1 * inch;         // bottom of the hook notch below the tread top
export const HAND_SLOT_W = 4 * inch;               // along Y
export const HAND_SLOT_H = 1.25 * inch;
export const HAND_SLOT_Z = 25 * inch;              // slot center height
export const HAND_SLOT_IN = 3 * inch;              // slot center, in from the bed-side edge

// ---------------------------------------------------------------------------
// Front angle (on the cabinet's front post)
// ---------------------------------------------------------------------------
export const FA_PANEL_LEG = 1.5 * inch;            // leg flat on the panel
export const FA_OUT_LEG = 1.25 * inch;             // leg sticking out, flush with the front edge
export const FA_BOTTOM_Z = 0.5 * inch;             // assumed
export const FA_PANEL_LEG_TOP = 16.5 * inch;       // panel leg stops here to clear the middle bracket
export const FA_TOP_Z = 17.75 * inch;              // outward leg runs up past the keeper (assumed)
export const FA_SCREW_Z = [2.5 * inch, 8.5 * inch, 14.5 * inch];   // assumed
export const FA_SCREW_INSET = 0.6 * inch;          // screw line, back from the front edge (assumed)
export const KEEPER_Z = 17.25 * inch;              // travel-latch keeper height

// ---------------------------------------------------------------------------
// Hook-on step (provisional: B3 doesn't detail it yet)
// ---------------------------------------------------------------------------
export const STEP_WIDTH = 18 * inch;               // match TREAD_WIDTH
export const STEP_LIP = 2 * inch;
export const STUD_Z1 = 2 * inch;                   // keyhole studs, height off the floor
export const STUD_Z2 = 7 * inch;
export const STUD_HOLE_DIA = 0.257 * inch;         // 1/4" studs
export const CAB_STUD_X = 0.75 * inch;             // studs on the front angle, out from the panel
export const STEP_LEG_BOTTOM_Z = (3 / 8) * inch;   // leg bottom above the floor (rubber boot)
export const STEP_LEG_PIVOT_DROP = 1 * inch;       // leg axle below the step top (assumed)

// ---------------------------------------------------------------------------
// Step ledge (holds the folded hook-on step)
// ---------------------------------------------------------------------------
export const LEDGE_LEN = 6 * inch;                 // along Y, starting at STAIR_Y0
export const LEDGE_SHELF = 2.5 * inch;             // shelf depth, out from the panel
export const LEDGE_Z = (5 / 8) * inch;             // shelf bottom above the floor
export const LEDGE_LEG_H = (11 / 8) * inch;        // leg height on the panel

// ---------------------------------------------------------------------------
// Fold check
// ---------------------------------------------------------------------------
// 0 degrees = open and in use, 90 degrees = folded flat against the cabinet.
// The treads, outer rail and outer axles move; everything else stays put.
export const FOLD_ANGLE = 0 * degree;
