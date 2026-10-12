FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");
import(path : "onshape/std/sheetMetalStart.fs", version : "2641.0");

// <B3-IMPORT> In Onshape: delete the next line, click Import in the toolbar,
// pick "B3 Parameters", then type "export " in front of the line it inserts.
// export import(path : "B3 Parameters", version : "");
// </B3-IMPORT>

/*
 * B3 folding bed stair: shared helpers and derived positions.
 * Nothing to adjust here; the numbers live in B3 Parameters.
 */

// Which tread: n = 0 is the hook-on step, 1 the middle tread, 2 the top tread.
export const HOOK_STEP = 0;
export const TREAD_MIDDLE = 1;
export const TREAD_TOP = 2;

export const RGB_ALU = [0.78, 0.80, 0.83];
export const RGB_ALU_DARK = [0.55, 0.60, 0.66];
export const RGB_STEP = [0.62, 0.56, 0.77];
export const RGB_STEEL = [0.36, 0.41, 0.44];
export const RGB_TUBE = [0.70, 0.72, 0.76];
export const RGB_REF = [0.85, 0.75, 0.57];

// ---------------------------------------------------------------------------
// Derived positions
// ---------------------------------------------------------------------------
// Tread top T.
export function b3T(n is number) returns ValueWithUnits
{
    return (n + 1) * RISE;
}

// Start of the tread's going along x: top 0, middle 6, step 12.
export function b3GoingX(n is number) returns ValueWithUnits
{
    return (2 - n) * GOING;
}

// Outer faces of the two ears in a going (back, front).
export function b3EarFaces(n is number) returns array
{
    const xg = b3GoingX(n);
    return [xg + EAR_X0, xg + GOING - EAR_X0];
}

// The two ears' x ranges, each PLATE_T thick inboard of its outer face.
export function b3EarRanges(n is number) returns array
{
    const f = b3EarFaces(n);
    return [[f[0], f[0] + PLATE_T], [f[1] - PLATE_T, f[1]]];
}

// ---------------------------------------------------------------------------
// Page coordinates to Onshape
// ---------------------------------------------------------------------------
// The page's x, y, z are a left-handed set as installed (facing the bed-end
// wall, the cabinet is on the right), so the page's y runs along Onshape's -Y.
// Every helper below takes page coordinates.
export function b3V(p is Vector) returns Vector
{
    return vector(p[0], -p[1], p[2]);
}

// ---------------------------------------------------------------------------
// Mate connectors
// ---------------------------------------------------------------------------
// Joints between parts, in page coordinates: each is the frame where two
// parts' mate connectors meet when the stair is open. Pivot and axle frames
// have z along -x (toward the bed end) and x along y.
export function b3Joint(origin is Vector, zAxis is Vector, xAxis is Vector) returns map
{
    return { "origin" : origin, "zAxis" : zAxis, "xAxis" : xAxis };
}

export function b3AxleJoint(x is ValueWithUnits, y is ValueWithUnits, z is ValueWithUnits) returns map
{
    return b3Joint(vector(x, y, z), vector(-1, 0, 0), vector(0, 1, 0));
}

// On the panel face (z out into the room, x along the cabinet).
export function b3PanelJoint(x is ValueWithUnits, z is ValueWithUnits) returns map
{
    return b3Joint(vector(x, 0 * inch, z), vector(0, 1, 0), vector(1, 0, 0));
}

// Tread n's cabinet and outer axles, at the axle's bed-end tip.
export function b3CabAxleJoint(n is number) returns map
{
    return b3AxleJoint(b3GoingX(n) + TREAD_AXLE_X0, CAB_AXLE_Y, b3T(n) - CAB_AXLE_DROP);
}

export function b3OutAxleJoint(n is number) returns map
{
    return b3AxleJoint(b3GoingX(n) + TREAD_AXLE_X0, OUT_AXLE_Y, b3T(n) - OUT_AXLE_DROP);
}

// Bracket n on the panel: the web's bottom corner at the bed end.
export function b3BracketJoint(n is number) returns map
{
    return b3PanelJoint(b3EarFaces(n)[0], b3T(n) - BRACKET_TOP_DROP - BRACKET_H);
}

// Tread n's rail bracket is the same part as its cabinet bracket, turned
// half a turn about a vertical axis and lowered so its axle hole lands on the
// outer axle line. This moves a joint on the cabinet bracket to the same spot
// on the rail bracket.
export function b3OnRail(n is number, joint is map) returns map
{
    const xg = b3GoingX(n);
    const p = joint.origin;
    const z = joint.zAxis;
    const x = joint.xAxis;
    return b3Joint(vector(2 * xg + GOING - p[0], OUT_AXLE_Y + CAB_AXLE_Y - p[1], p[2] + CAB_AXLE_DROP - OUT_AXLE_DROP),
        vector(-z[0], -z[1], z[2]), vector(-x[0], -x[1], x[2]));
}

// Bracket n's web on the rail's inner face.
export function b3RailBracketJoint(n is number) returns map
{
    return b3OnRail(n, b3BracketJoint(n));
}

// The bracket's axle seen from its other end, so that on the rail it matches
// the tread's outer axle joint: b3OnRail(n, b3BracketAxleTurnedJoint(n)) is
// b3OutAxleJoint(n).
export function b3BracketAxleTurnedJoint(n is number) returns map
{
    return b3Joint(vector(b3GoingX(n) + GOING - TREAD_AXLE_X0, CAB_AXLE_Y, b3T(n) - CAB_AXLE_DROP),
        vector(1, 0, 0), vector(0, -1, 0));
}

// Screw hole k in bracket n's web, on the ear side, z along the screw.
export function b3BracketScrewJoint(n is number, k is number) returns map
{
    return b3Joint(vector(b3GoingX(n) + SCREW_X[k], PLATE_T, b3T(n) - SCREW_DROP), vector(0, -1, 0), vector(1, 0, 0));
}

// The nut on the rail's outer face for screw k of tread n's rail bracket.
export function b3RailNutJoint(n is number, k is number) returns map
{
    const hole = b3OnRail(n, b3BracketScrewJoint(n, k)).origin;
    return b3Joint(vector(hole[0], RAIL_Y + PLATE_T, hole[2]), vector(0, 1, 0), vector(1, 0, 0));
}

// Front angle on the panel: its bottom front corner.
export function b3FrontAngleJoint() returns map
{
    return b3PanelJoint(FA_X1, FA_BOTTOM_Z);
}

// Step ledge on the panel: the leg's bottom corner at the bed end.
export function b3StepLedgeJoint() returns map
{
    return b3PanelJoint(0 * inch, LEDGE_TOP_Z - LEDGE_LEG_H);
}

// Tread n's stop pins, on the bracket ear's inside face, z along the pin.
export function b3StopPinJoint(n is number, front is boolean) returns map
{
    const r = b3EarRanges(n);
    const p = vector(front ? r[1][0] : r[0][1], STOP_PIN_Y, b3T(n) - STOP_PIN_DROP);
    return b3Joint(p, vector(front ? 1 : -1, 0, 0), vector(0, 1, 0));
}

// Step clip on the rail's inner face, at the clip's bottom corner on the bed-end side.
export function b3StepClipJoint() returns map
{
    return b3Joint(vector(CLIP_X0, RAIL_Y, CLIP_Z0), vector(0, -1, 0), vector(1, 0, 0));
}

export function b3StepCabAxleJoint() returns map
{
    return b3AxleJoint(STEP_AXLE_X0, STEP_CAB_AXLE_Y, STEP_CAB_AXLE_Z);
}

export function b3StepOutAxleJoint() returns map
{
    return b3AxleJoint(STEP_AXLE_X0, STEP_OUT_AXLE_Y, STEP_OUT_AXLE_Z);
}

// A mate connector named mcName on the part partName (the bodies in owner),
// at a joint. The assembly script finds it by its "b3Mc" attribute; an
// assembly mate names it by its id, joined with dots.
export function b3Mc(context is Context, id is Id, owner is Query, partName is string, mcName is string, joint is map)
{
    opMateConnector(context, id, {
                "coordSystem" : coordSystem(b3V(joint.origin), b3V(joint.xAxis), b3V(joint.zAxis)),
                "owner" : owner
            });
    var fullId = id[0];
    for (var i = 1; i < size(id); i += 1)
    {
        fullId = fullId ~ "." ~ id[i];
    }
    setAttribute(context, { "entities" : qCreatedBy(id, EntityType.BODY), "name" : "b3Mc",
                "attribute" : { "part" : partName, "name" : mcName, "featureId" : fullId } });
}

// ---------------------------------------------------------------------------
// Sketch planes and prisms
// ---------------------------------------------------------------------------
// Sketch (u, v) = (y, z), extruded along -x.
export function b3PlaneYZ(x is ValueWithUnits) returns Plane
{
    return plane(b3V(vector(x, 0 * inch, 0 * inch)), vector(-1, 0, 0), b3V(vector(0, 1, 0)));
}

// Sketch (u, v) = (x, z), extruded along +y.
export function b3PlaneXZ(y is ValueWithUnits) returns Plane
{
    return plane(b3V(vector(0 * inch, y, 0 * inch)), b3V(vector(0, 1, 0)), vector(1, 0, 0));
}

// Sketch (u, v) = (x, y), extruded along -z.
export function b3PlaneXY(z is ValueWithUnits) returns Plane
{
    return plane(vector(0 * inch, 0 * inch, z), vector(0, 0, -1), vector(1, 0, 0));
}

// Closed loop through 2D points. An entry {"mid" : p, "end" : q} draws an arc
// from the previous point through p to q. The loop closes with a line.
export function b3SketchLoop(sketch is Sketch, prefix is string, path is array)
{
    const first = path[0];
    var cur = first;
    for (var i = 1; i <= size(path); i += 1)
    {
        const item = i < size(path) ? path[i] : first;
        if (item is map)
        {
            skArc(sketch, prefix ~ "a" ~ toString(i), { "start" : cur, "mid" : item.mid, "end" : item.end });
            cur = item.end;
        }
        else
        {
            skLineSegment(sketch, prefix ~ "l" ~ toString(i), { "start" : cur, "end" : item });
            cur = item;
        }
    }
}

// Extrudes a 2D outline by depth. Each hole is {"center" : p, "dia" : d}
// or a closed path like the outline.
export function b3Prism(context is Context, id is Id, frame is Plane, path is array, holes is array, depth is ValueWithUnits)
{
    const sketchId = id + "sketch";
    const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : frame });
    b3SketchLoop(sketch, "outline", path);
    for (var i = 0; i < size(holes); i += 1)
    {
        const h = holes[i];
        if (h is map)
        {
            skCircle(sketch, "hole" ~ toString(i), { "center" : h.center, "radius" : h.dia / 2 });
        }
        else
        {
            b3SketchLoop(sketch, "hole" ~ toString(i), h);
        }
    }
    skSolve(sketch);
    opExtrude(context, id + "extrude", {
                "entities" : qSketchRegion(sketchId, true),
                "direction" : frame.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : depth
            });
    opDeleteBodies(context, id + "cleanup", { "entities" : qCreatedBy(sketchId, EntityType.BODY) });
}

// Outline in (y, z), from x0 to x1.
export function b3PrismYZ(context is Context, id is Id, path is array, holes is array, x0 is ValueWithUnits, x1 is ValueWithUnits)
{
    b3Prism(context, id, b3PlaneYZ(x1), path, holes, x1 - x0);
}

// Outline in (x, z), from y0 to y1.
export function b3PrismXZ(context is Context, id is Id, path is array, holes is array, y0 is ValueWithUnits, y1 is ValueWithUnits)
{
    b3Prism(context, id, b3PlaneXZ(y0), path, holes, y1 - y0);
}

// Outline in (x, y), from z0 to z1.
export function b3PrismXY(context is Context, id is Id, path is array, holes is array, z0 is ValueWithUnits, z1 is ValueWithUnits)
{
    b3Prism(context, id, b3PlaneXY(z1), path, holes, z1 - z0);
}

export function b3Box(context is Context, id is Id, lo is Vector, hi is Vector)
{
    fCuboid(context, id, { "corner1" : b3V(lo), "corner2" : b3V(hi) });
}

// Cylinder along x.
export function b3CylX(context is Context, id is Id, y is ValueWithUnits, z is ValueWithUnits,
    x0 is ValueWithUnits, x1 is ValueWithUnits, dia is ValueWithUnits)
{
    fCylinder(context, id, { "bottomCenter" : b3V(vector(x0, y, z)), "topCenter" : b3V(vector(x1, y, z)), "radius" : dia / 2 });
}

// Tube along x, x0 < x1, with a bore.
export function b3TubeX(context is Context, id is Id, y is ValueWithUnits, z is ValueWithUnits,
    x0 is ValueWithUnits, x1 is ValueWithUnits, dia is ValueWithUnits, bore is ValueWithUnits)
{
    b3CylX(context, id + "outside", y, z, x0, x1, dia);
    b3CylX(context, id + "bore", y, z, x0 - 1 * millimeter, x1 + 1 * millimeter, bore);
    opBoolean(context, id + "cut", { "tools" : b3Solids(id + "bore"), "targets" : b3Solids(id + "outside"),
                "operationType" : BooleanOperationType.SUBTRACTION });
}

// ---------------------------------------------------------------------------
// 2D shapes
// ---------------------------------------------------------------------------
export function b3Rect(u0 is ValueWithUnits, v0 is ValueWithUnits, u1 is ValueWithUnits, v1 is ValueWithUnits) returns array
{
    return [vector(u0, v0), vector(u1, v0), vector(u1, v1), vector(u0, v1)];
}

// Rounds the corner p, between the edges to a and to b, with radius r: the
// path entries from the tangent point on pa, around the arc, to the tangent
// point on pb. Works for outside and inside corners alike.
export function b3Corner(a is Vector, p is Vector, b is Vector, r is ValueWithUnits) returns array
{
    const ua = (a - p) / norm(a - p);
    const ub = (b - p) / norm(b - p);
    const half = acos(dot(ua, ub)) / 2;
    const bisector = (ua + ub) / norm(ua + ub);
    const center = p + bisector * (r / sin(half));
    return [p + ua * (r / tan(half)), { "mid" : center - bisector * r, "end" : p + ub * (r / tan(half)) }];
}

// Closed polygon with corner i rounded to radii[i] (0 leaves it sharp). Each
// corner's tangent points must stay on its two edges.
export function b3Rounded(points is array, radii is array) returns array
{
    const n = size(points);
    var path = [];
    for (var i = 0; i < n; i += 1)
    {
        if (radii[i] is number || radii[i] <= 0 * inch)
        {
            path = append(path, points[i]);
        }
        else
        {
            path = concatenateArrays([path, b3Corner(points[(i + n - 1) % n], points[i], points[(i + 1) % n], radii[i])]);
        }
    }
    return path;
}

export function b3Hole(u is ValueWithUnits, v is ValueWithUnits, dia is ValueWithUnits) returns map
{
    return { "center" : vector(u, v), "dia" : dia };
}

// Strip along the edge p -> q, offset to the left of travel by t.
export function b3Strip(p is Vector, q is Vector, t is ValueWithUnits) returns array
{
    const d = (q - p) / norm(q - p);
    const left = vector(-d[1], d[0]);
    return [p, q, q + left * t, p + left * t];
}

// Obround slot centered on c, l long along u and h tall along v.
export function b3Slot(c is Vector, l is ValueWithUnits, h is ValueWithUnits) returns array
{
    const r = h / 2;
    const a = l / 2 - r;
    return [
            vector(c[0] - a, c[1] + r),
            { "mid" : vector(c[0] - a - r, c[1]), "end" : vector(c[0] - a, c[1] - r) },
            vector(c[0] + a, c[1] - r),
            { "mid" : vector(c[0] + a + r, c[1]), "end" : vector(c[0] + a, c[1] + r) }
        ];
}

// Keyhole: KEYHOLE_DIA hole centered (u, vHole), with a KEYHOLE_SLOT_W slot
// running down to a round end centered at vEnd.
export function b3Keyhole(u is ValueWithUnits, vHole is ValueWithUnits, vEnd is ValueWithUnits) returns array
{
    const big = KEYHOLE_DIA / 2;
    const h = KEYHOLE_SLOT_W / 2;
    const meet = vHole - sqrt(big * big - h * h);
    return [
            vector(u - h, vEnd),
            { "mid" : vector(u, vEnd - h), "end" : vector(u + h, vEnd) },
            vector(u + h, meet),
            { "mid" : vector(u, vHole + big), "end" : vector(u - h, meet) }
        ];
}

// Hook-shaped bracket ear in (y, z). The ear runs from the root face yRoot
// to yFar, over z zLo to zHi; above that the hook (yHookIn to yFar) rises to
// a round top of radius EAR_HOOK_R on the axle (axY, axZ). The bottom outer
// corner and the hook notch are rounded.
export function b3EarOutline(yRoot is ValueWithUnits, yFar is ValueWithUnits, yHookIn is ValueWithUnits,
    zLo is ValueWithUnits, zHi is ValueWithUnits, axY is ValueWithUnits, axZ is ValueWithUnits) returns array
{
    const root = vector(yRoot, zLo);
    const notch = vector(yHookIn, zHi);
    return concatenateArrays([
                [root],
                b3Corner(root, vector(yFar, zLo), vector(yFar, axZ), EAR_CORNER_R),
                [vector(yFar, axZ), { "mid" : vector(axY, axZ + EAR_HOOK_R), "end" : vector(yHookIn, axZ) }],
                b3Corner(vector(yHookIn, axZ), notch, vector(yRoot, zHi), EAR_NOTCH_R),
                [vector(yRoot, zHi)]
            ]);
}

// ---------------------------------------------------------------------------
// Tray (treads and hook-on step): skin plus two lips bent down at the x ends,
// as a sheet-metal part. Holes are in (y, z) and go through both lips. Both
// bottom corners of each lip are rounded to LIP_CORNER_R.
// ---------------------------------------------------------------------------
export function b3Tray(context is Context, id is Id, x0 is ValueWithUnits, top is ValueWithUnits, holes is array)
{
    const y0 = TRAY_Y0;
    const y1 = TRAY_Y0 + TRAY_W;
    const x1 = x0 + TRAY_D;
    const zb = top - LIP;
    const lip = b3Rounded([vector(y0, top), vector(y1, top), vector(y1, zb), vector(y0, zb)],
        [0, 0, LIP_CORNER_R, LIP_CORNER_R]);
    b3Box(context, id + "skin", vector(x0, y0, top - SHEET_T), vector(x1, y1, top));
    b3PrismYZ(context, id + "lipBack", lip, holes, x0, x0 + SHEET_T);
    b3PrismYZ(context, id + "lipFront", lip, holes, x1 - SHEET_T, x1);
    b3Union(context, id + "join", b3Solids(id));
    const ym = (y0 + y1) / 2;
    b3SheetMetal(context, id + "sm", b3Solids(id), [
                vector((x0 + x1) / 2, ym, top),
                vector(x0, ym, top - LIP / 2),
                vector(x1, ym, top - LIP / 2)
            ], SHEET_T, BEND_R_LIP, K_SHEET);
}

// ---------------------------------------------------------------------------
// Sheet metal
// ---------------------------------------------------------------------------
// Turns body, a solid modeled to the outside of its bends with sharp bends,
// into an Onshape sheet-metal part. outside lists a point (page coordinates)
// inside each outside face: the base wall first, then each wall bent off it.
// The edges between the base and the others become bends of inside radius r;
// any edge between two of the others becomes a rip with a corner relief. The
// thickness t goes inward, so the outside faces stay put.
export function b3SheetMetal(context is Context, id is Id, body is Query, outside is array,
    t is ValueWithUnits, r is ValueWithUnits, k is number)
{
    const faces = qOwnedByBody(body, EntityType.FACE);
    var walls = [];
    for (var p in outside)
    {
        const wall = qContainsPoint(faces, b3V(p));
        if (size(evaluateQuery(context, wall)) != 1)
        {
            throw regenError("B3: expected one outside face at " ~ toString(p / inch) ~ " in");
        }
        walls = append(walls, wall);
    }
    const baseEdges = qAdjacent(walls[0], AdjacencyType.EDGE, EntityType.EDGE);
    var bends = [];
    for (var i = 1; i < size(walls); i += 1)
    {
        bends = append(bends, qIntersection([baseEdges, qAdjacent(walls[i], AdjacencyType.EDGE, EntityType.EDGE)]));
    }
    sheetMetalStart(context, id, {
                "process" : SMProcessType.CONVERT,
                "partToConvert" : body,
                "facesToExclude" : qSubtraction(faces, qUnion(walls)),
                "bends" : qUnion(bends),
                "thickness" : t,
                "radius" : r,
                "kFactor" : k,
                "oppositeDirection" : true,
                "defaultCornerStyle" : SMCornerStrategyType.ROUND,
                "defaultCornerReliefScale" : CORNER_RELIEF_SCALE,
                "defaultBendReliefStyle" : SMBendStrategyType.RECTANGLE,
                "defaultBendReliefDepthScale" : 1 + RELIEF_EXTRA / r,
                "defaultBendReliefScale" : RELIEF_W_SCALE
            });
}

// Bend relief where a bend stops at p, short of the edge it runs along: a
// notch in the wall that carries on, RELIEF_W_SCALE * t wide along the edge
// (direction along, away from the bend) and r + t + RELIEF_EXTRA deep
// (direction inward, into the wall). A 2D outline for b3Prism*.
export function b3ReliefPath(p is Vector, along is Vector, inward is Vector, t is ValueWithUnits, r is ValueWithUnits) returns array
{
    const a = along / norm(along);
    const n = inward / norm(inward);
    const w = RELIEF_W_SCALE * t;
    const d = r + t + RELIEF_EXTRA;
    const out = n * (-0.05 * inch);
    return [p + out, p + a * w + out, p + a * w + n * d, p + n * d];
}

export function b3Subtract(context is Context, id is Id, targets is Query, tools is Query)
{
    opBoolean(context, id, { "tools" : tools, "targets" : targets, "operationType" : BooleanOperationType.SUBTRACTION });
}

// ---------------------------------------------------------------------------
// Booleans and finishing
// ---------------------------------------------------------------------------
export function b3Solids(id is Id) returns Query
{
    return qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID);
}

export function b3Union(context is Context, id is Id, bodies is Query)
{
    if (size(evaluateQuery(context, bodies)) > 1)
    {
        opBoolean(context, id, { "tools" : bodies, "operationType" : BooleanOperationType.UNION });
    }
}

export function b3Finish(context is Context, bodies is Query, partName is string, rgb is array)
{
    b3Part(context, bodies, partName, material("Aluminum 5052-H32", ALU_DENSITY), rgb);
}

export function b3FinishSteel(context is Context, bodies is Query, partName is string)
{
    b3Part(context, bodies, partName, material("Stainless 304", STEEL_DENSITY), RGB_STEEL);
}

export function b3FinishTube(context is Context, bodies is Query, partName is string)
{
    b3Part(context, bodies, partName, material("Aluminum 6061", TUBE_DENSITY), RGB_TUBE);
}

function b3Part(context is Context, bodies is Query, partName is string, mat is Material, rgb is array)
{
    setProperty(context, { "entities" : bodies, "propertyType" : PropertyType.NAME, "value" : partName });
    setProperty(context, { "entities" : bodies, "propertyType" : PropertyType.MATERIAL, "value" : mat });
    setProperty(context, { "entities" : bodies, "propertyType" : PropertyType.APPEARANCE,
                "value" : color(rgb[0], rgb[1], rgb[2]) });
}
