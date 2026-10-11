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
export function b3TreadTopZ(n is number) returns ValueWithUnits
{
    return (n + 1) * RISE;
}

export function b3TreadBackY(n is number) returns ValueWithUnits
{
    return STAIR_Y0 + (2 - n) * TREAD_GOING;
}

export function b3TreadMidY(n is number) returns ValueWithUnits
{
    return b3TreadBackY(n) + TREAD_GOING / 2;
}

export function b3StairFrontY() returns ValueWithUnits
{
    return STAIR_Y0 + 2 * TREAD_GOING;
}

export function b3TreadX0() returns ValueWithUnits
{
    return PANEL_GAP;
}

export function b3TreadX1() returns ValueWithUnits
{
    return PANEL_GAP + TREAD_WIDTH;
}

// Inner face of the outer rail's plate.
export function b3RailX0() returns ValueWithUnits
{
    return b3TreadX1() + RAIL_CLEAR;
}

// Hook-on step studs on the outer rail's front flange.
export function b3OutStudX() returns ValueWithUnits
{
    return b3RailX0() + PLATE_T + RAIL_FLANGE / 2;
}

export function b3CabAxle(n is number) returns Vector
{
    return vector(PLATE_T + CAB_AXLE_OUT, b3TreadMidY(n), b3TreadTopZ(n) - CAB_AXLE_DROP);
}

export function b3OutAxle(n is number) returns Vector
{
    return vector(b3RailX0() - OUT_AXLE_IN, b3TreadMidY(n), b3TreadTopZ(n) - OUT_AXLE_DROP);
}

// ---------------------------------------------------------------------------
// Fold transforms (FOLD_ANGLE: 0 = open, 90 degrees = folded)
// ---------------------------------------------------------------------------
// Each tread turns about its cabinet axle; the outer edge swings up toward the panel.
export function b3TreadFold(n is number) returns Transform
{
    return rotationAround(line(b3CabAxle(n), vector(0, -1, 0)), FOLD_ANGLE);
}

// The treads and rail form a parallelogram, so the rail and the outer axles
// translate without turning.
export function b3RailFold() returns Transform
{
    const v = b3OutAxle(TREAD_TOP) - b3CabAxle(TREAD_TOP);
    const c = cos(FOLD_ANGLE);
    const s = sin(FOLD_ANGLE);
    const moved = vector(v[0] * c - v[2] * s, v[1], v[2] * c + v[0] * s);
    return transform(moved - v);
}

// ---------------------------------------------------------------------------
// Sketch planes. Each maps sketch (u, v) to two world axes and extrudes
// along the third.
// ---------------------------------------------------------------------------
// (u, v) = (Y, Z); extrudes toward +X from x.
export function b3FrameYZ(x is ValueWithUnits) returns Plane
{
    return plane(vector(x, 0 * inch, 0 * inch), vector(1, 0, 0), vector(0, 1, 0));
}

// (u, v) = (X, Z); extrudes toward -Y from y.
export function b3FrameXZ(y is ValueWithUnits) returns Plane
{
    return plane(vector(0 * inch, y, 0 * inch), vector(0, -1, 0), vector(1, 0, 0));
}

// ---------------------------------------------------------------------------
// Geometry helpers
// ---------------------------------------------------------------------------
// Closed loop through 2D points. An entry {"mid" : p, "end" : q} draws an arc
// from the previous point through p to q. The last entry must be a point.
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

// Extrudes a 2D outline by depth. Each hole is {"center" : p, "radius" : r}
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
            skCircle(sketch, "hole" ~ toString(i), { "center" : h.center, "radius" : h.radius });
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

export function b3Box(context is Context, id is Id, lo is Vector, hi is Vector)
{
    fCuboid(context, id, { "corner1" : lo, "corner2" : hi });
}

// Cylinder along Y.
export function b3CylY(context is Context, id is Id, x is ValueWithUnits, z is ValueWithUnits,
    y0 is ValueWithUnits, y1 is ValueWithUnits, r is ValueWithUnits)
{
    fCylinder(context, id, { "bottomCenter" : vector(x, y0, z), "topCenter" : vector(x, y1, z), "radius" : r });
}

// Cylinder along X.
export function b3CylX(context is Context, id is Id, y is ValueWithUnits, z is ValueWithUnits,
    x0 is ValueWithUnits, x1 is ValueWithUnits, r is ValueWithUnits)
{
    fCylinder(context, id, { "bottomCenter" : vector(x0, y, z), "topCenter" : vector(x1, y, z), "radius" : r });
}

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

export function b3Subtract(context is Context, id is Id, targets is Query, tools is Query)
{
    if (size(evaluateQuery(context, tools)) > 0)
    {
        opBoolean(context, id, { "targets" : targets, "tools" : tools, "operationType" : BooleanOperationType.SUBTRACTION });
    }
}

// Strip along the edge p -> q, offset to the left of travel by t.
export function b3Strip(p is Vector, q is Vector, t is ValueWithUnits) returns array
{
    const d = (q - p) / norm(q - p);
    const left = vector(-d[1], d[0]);
    return [p, q, q + left * t, p + left * t];
}

// Rounded slot centered on c, w long along u and h tall along v.
export function b3Slot(c is Vector, w is ValueWithUnits, h is ValueWithUnits) returns array
{
    const r = h / 2;
    const a = w / 2 - r;
    return [
            vector(c[0] - a, c[1] + r),
            { "mid" : vector(c[0] - a - r, c[1]), "end" : vector(c[0] - a, c[1] - r) },
            vector(c[0] + a, c[1] - r),
            { "mid" : vector(c[0] + a + r, c[1]), "end" : vector(c[0] + a, c[1] + r) }
        ];
}

// Tray with lips bent down at the back (yb) and front, in (Y, Z).
export function b3TrayOutline(yb is ValueWithUnits, top is ValueWithUnits, lip is ValueWithUnits) returns array
{
    const yf = yb + TREAD_GOING;
    const t = SHEET_T;
    return [
            vector(yb, top), vector(yf, top), vector(yf, top - lip), vector(yf - t, top - lip),
            vector(yf - t, top - t), vector(yb + t, top - t), vector(yb + t, top - lip), vector(yb, top - lip)
        ];
}

// ---------------------------------------------------------------------------
// Part names, materials and colors
// ---------------------------------------------------------------------------
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
