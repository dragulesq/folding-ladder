FeatureScript 2641;
import(path : "onshape/std/common.fs", version : "2641.0");

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

export function b3CabAxle(n is number) returns Vector
{
    return vector(b3GoingX(n), CAB_AXLE_Y, b3T(n) - CAB_AXLE_DROP);
}

// ---------------------------------------------------------------------------
// Fold transforms (FOLD_ANGLE: 0 = open, 90 degrees = folded)
// ---------------------------------------------------------------------------
// Each tread turns about its cabinet axle; the outer edge swings up toward the panel.
export function b3TreadFold(n is number) returns Transform
{
    return rotationAround(line(b3CabAxle(n), vector(1, 0, 0)), FOLD_ANGLE);
}

// The treads and rail form a parallelogram, so the rail, its clip and the
// outer axles translate without turning.
export function b3RailFold() returns Transform
{
    const vy = OUT_AXLE_Y - CAB_AXLE_Y;
    const vz = CAB_AXLE_DROP - OUT_AXLE_DROP;
    const c = cos(FOLD_ANGLE);
    const s = sin(FOLD_ANGLE);
    return transform(vector(0 * inch, vy * c - vz * s - vy, vz * c + vy * s - vz));
}

// ---------------------------------------------------------------------------
// Sketch planes and prisms
// ---------------------------------------------------------------------------
// Sketch (u, v) = (y, z), extruded along +x.
export function b3PlaneYZ(x is ValueWithUnits) returns Plane
{
    return plane(vector(x, 0 * inch, 0 * inch), vector(1, 0, 0), vector(0, 1, 0));
}

// Sketch (u, v) = (x, z), extruded along -y.
export function b3PlaneXZ(y is ValueWithUnits) returns Plane
{
    return plane(vector(0 * inch, y, 0 * inch), vector(0, -1, 0), vector(1, 0, 0));
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
    b3Prism(context, id, b3PlaneYZ(x0), path, holes, x1 - x0);
}

// Outline in (x, z), from y0 to y1.
export function b3PrismXZ(context is Context, id is Id, path is array, holes is array, y0 is ValueWithUnits, y1 is ValueWithUnits)
{
    b3Prism(context, id, b3PlaneXZ(y1), path, holes, y1 - y0);
}

export function b3Box(context is Context, id is Id, lo is Vector, hi is Vector)
{
    fCuboid(context, id, { "corner1" : lo, "corner2" : hi });
}

// Cylinder along x.
export function b3CylX(context is Context, id is Id, y is ValueWithUnits, z is ValueWithUnits,
    x0 is ValueWithUnits, x1 is ValueWithUnits, dia is ValueWithUnits)
{
    fCylinder(context, id, { "bottomCenter" : vector(x0, y, z), "topCenter" : vector(x1, y, z), "radius" : dia / 2 });
}

// ---------------------------------------------------------------------------
// 2D shapes
// ---------------------------------------------------------------------------
export function b3Rect(u0 is ValueWithUnits, v0 is ValueWithUnits, u1 is ValueWithUnits, v1 is ValueWithUnits) returns array
{
    return [vector(u0, v0), vector(u1, v0), vector(u1, v1), vector(u0, v1)];
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

// Hook-shaped ear in (y, z). The ear runs from the root face yRoot to yFar,
// over z zLo to zHi; above that the hook (yHookIn to yFar) rises to a round
// top of radius EAR_HOOK_R on the axle (axY, axZ). Works either way along y.
export function b3EarOutline(yRoot is ValueWithUnits, yFar is ValueWithUnits, yHookIn is ValueWithUnits,
    zLo is ValueWithUnits, zHi is ValueWithUnits, axY is ValueWithUnits, axZ is ValueWithUnits) returns array
{
    return [
            vector(yRoot, zLo),
            vector(yFar, zLo),
            vector(yFar, axZ),
            { "mid" : vector(axY, axZ + EAR_HOOK_R), "end" : vector(yHookIn, axZ) },
            vector(yHookIn, zHi),
            vector(yRoot, zHi)
        ];
}

// ---------------------------------------------------------------------------
// Tray (treads and hook-on step): skin plus two lips at the x ends.
// Holes are in (y, z) and go through both lips. cornerR > 0 rounds each
// lip's bottom outer corner.
// ---------------------------------------------------------------------------
export function b3Tray(context is Context, id is Id, x0 is ValueWithUnits, top is ValueWithUnits, holes is array, cornerR is ValueWithUnits)
{
    const y0 = TRAY_Y0;
    const y1 = TRAY_Y0 + TRAY_W;
    const x1 = x0 + TRAY_D;
    const zb = top - LIP;
    var lip = [vector(y0, top), vector(y1, top), vector(y1, zb), vector(y0, zb)];
    if (cornerR > 0 * inch)
    {
        const k = 1 - sqrt(0.5);
        lip = [
                vector(y0, top),
                vector(y1, top),
                vector(y1, zb + cornerR),
                { "mid" : vector(y1 - cornerR * k, zb + cornerR * k), "end" : vector(y1 - cornerR, zb) },
                vector(y0, zb)
            ];
    }
    b3Box(context, id + "skin", vector(x0, y0, top - SHEET_T), vector(x1, y1, top));
    b3PrismYZ(context, id + "lipBack", lip, holes, x0, x0 + SHEET_T);
    b3PrismYZ(context, id + "lipFront", lip, holes, x1 - SHEET_T, x1);
    b3Union(context, id + "join", b3Solids(id));
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
    setProperty(context, { "entities" : bodies, "propertyType" : PropertyType.NAME, "value" : partName });
    setProperty(context, { "entities" : bodies, "propertyType" : PropertyType.MATERIAL,
                "value" : material("Aluminum 5052-H32", ALU_DENSITY) });
    setProperty(context, { "entities" : bodies, "propertyType" : PropertyType.APPEARANCE,
                "value" : color(rgb[0], rgb[1], rgb[2]) });
}

export function b3FinishSteel(context is Context, bodies is Query, partName is string)
{
    setProperty(context, { "entities" : bodies, "propertyType" : PropertyType.NAME, "value" : partName });
    setProperty(context, { "entities" : bodies, "propertyType" : PropertyType.MATERIAL,
                "value" : material("Stainless 304", STEEL_DENSITY) });
    setProperty(context, { "entities" : bodies, "propertyType" : PropertyType.APPEARANCE,
                "value" : color(RGB_STEEL[0], RGB_STEEL[1], RGB_STEEL[2]) });
}
