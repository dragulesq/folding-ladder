#!/usr/bin/env python3
"""Write each sheet-metal part's flat pattern, as Onshape unfolds it, to a DXF for SendCutSend.

Reads the flat patterns in "B3 test Part Studio" through the parts API (one
call for the list, one per sheet-metal part): each flat's outline and holes,
and its bend centerlines. Each part gets one DXF in
inches, full size, with nothing in it but the cut geometry (layer CUT) and the
bend lines (layer BEND), plus one SVG sheet of all of them to look at.

Usage:
  python3 tools/flat_patterns.py [--out DIR] [--state FILE]

Run tools/onshape_check.py first, so the Part Studio holds the current parts.
"""
import argparse
import json
import math
import pathlib
import urllib.parse

from onshape_api import ROOT, State

M = 0.0254


def flat_from_body(body):
    """One flat pattern body from the body details: its top face's edges, and its bend centerlines.

    The flat's top is split into walls and bend strips. An edge on one top face is cut
    geometry; an edge between two is a bend's tangent line. A bend strip is a top face with
    two such lines, parallel, a bend allowance apart; its centerline is the bend line.
    """
    xyz = lambda v: (v["x"] / M, v["y"] / M, v["z"] / M)
    edges = {e["id"]: e for e in body["edges"]}
    top = max(xyz(e["geometry"][k])[2] for e in edges.values() for k in ("startPoint", "endPoint"))
    low = min(xyz(e["geometry"][k])[2] for e in edges.values() for k in ("startPoint", "endPoint"))
    on_top = {i for i, e in edges.items() if all(abs(xyz(e["geometry"][k])[2] - top) < 1e-5 for k in ("startPoint", "midPoint", "endPoint"))}
    uses, faces = {}, []
    for f in body["faces"]:
        ids = [c["edgeId"] for loop in f["loops"] for c in loop["coedges"]]
        if f["surface"].get("type") == "PLANE" and ids and all(i in on_top for i in ids):
            faces.append(ids)
            for i in ids:
                uses[i] = uses.get(i, 0) + 1
    out = []
    for i in sorted(on_top):
        if uses.get(i) != 1:
            continue
        g = edges[i]["geometry"]
        pts = [xyz(g[k])[:2] for k in ("startPoint", "midPoint", "endPoint")]
        out.append(["l" if edges[i]["curve"]["type"] == "LINE" else "c", pts])
        if edges[i]["curve"]["type"] not in ("LINE", "CIRCLE"):
            raise SystemExit(f"edge {i} is a {edges[i]['curve']['type']}: not handled")
    bends = []
    for ids in faces:
        inner = [i for i in ids if uses[i] == 2]
        if len(inner) != 2 or len(ids) != 4:
            continue
        (a0, a1), (b0, b1) = [[xyz(edges[i]["geometry"][k])[:2] for k in ("startPoint", "endPoint")] for i in inner]
        if math.dist(a0, b0) > math.dist(a0, b1):
            b0, b1 = b1, b0
        t = top - low
        r, k = (0.25, 0.43) if t > 0.15 else (0.125, 0.44)   # SendCutSend's 3/16 and 1/8 5052, as in B3_Parameters.fs
        if abs(math.dist(a0, b0) - (math.pi / 2) * (r + k * t)) > 0.02 or abs(math.dist(a1, b1) - math.dist(a0, b0)) > 1e-4:
            continue
        bends.append([((a0[0] + b0[0]) / 2, (a0[1] + b0[1]) / 2), ((a1[0] + b1[0]) / 2, (a1[1] + b1[1]) / 2)])
    return {"edges": out, "bends": bends, "thickness": top - low}


def arc(p0, pm, p1):
    """Circle through three points: (center, radius, start angle, end angle) in degrees, counterclockwise."""
    (ax, ay), (bx, by), (cx, cy) = p0, pm, p1
    d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by))
    ux = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / d
    uy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / d
    r = math.hypot(ax - ux, ay - uy)
    a0, a1 = (math.degrees(math.atan2(p[1] - uy, p[0] - ux)) for p in (p0, p1))
    ccw = (bx - ax) * (cy - ay) - (by - ay) * (cx - ax) > 0
    return (ux, uy), r, (a0, a1) if ccw else (a1, a0)


def entities(flat):
    """[(layer, kind, data)] for one flat, shifted so its lower left corner is at the origin."""
    out = []
    for kind, pts in flat["edges"]:
        if kind == "l":
            out.append(("CUT", "line", (pts[0], pts[2])))
        elif kind == "c":
            if math.dist(pts[0], pts[2]) < 1e-6:
                c = ((pts[0][0] + pts[1][0]) / 2, (pts[0][1] + pts[1][1]) / 2)
                out.append(("CUT", "circle", (c, math.dist(pts[0], pts[1]) / 2)))
            else:
                out.append(("CUT", "arc", arc(*pts)))
        else:
            out += [("CUT", "line", (a, b)) for a, b in zip(pts, pts[1:])]
    for a, b in flat["bends"]:
        out.append(("BEND", "line", (a, b)))
    xs, ys = [], []
    for _, kind, d in out:
        if kind == "line":
            xs += [d[0][0], d[1][0]]
            ys += [d[0][1], d[1][1]]
        else:
            xs += [d[0][0] - d[1], d[0][0] + d[1]]
            ys += [d[0][1] - d[1], d[0][1] + d[1]]
    x0, y0 = min(xs), min(ys)

    def sh(p):
        return (p[0] - x0, p[1] - y0)

    moved = []
    for layer, kind, d in out:
        if kind == "line":
            moved.append((layer, kind, (sh(d[0]), sh(d[1]))))
        else:
            moved.append((layer, kind, (sh(d[0]),) + tuple(d[1:])))
    return moved, (max(xs) - x0, max(ys) - y0)


def dxf(ents):
    o = ["0", "SECTION", "2", "HEADER", "9", "$ACADVER", "1", "AC1009", "9", "$INSUNITS", "70", "1", "0", "ENDSEC",
         "0", "SECTION", "2", "TABLES", "0", "TABLE", "2", "LAYER", "70", "2",
         "0", "LAYER", "2", "CUT", "70", "0", "62", "7", "6", "CONTINUOUS",
         "0", "LAYER", "2", "BEND", "70", "0", "62", "1", "6", "CONTINUOUS",
         "0", "ENDTAB", "0", "ENDSEC", "0", "SECTION", "2", "ENTITIES"]
    f = lambda v: f"{v:.6f}"
    for layer, kind, d in ents:
        if kind == "line":
            o += ["0", "LINE", "8", layer, "10", f(d[0][0]), "20", f(d[0][1]), "11", f(d[1][0]), "21", f(d[1][1])]
        elif kind == "circle":
            o += ["0", "CIRCLE", "8", layer, "10", f(d[0][0]), "20", f(d[0][1]), "40", f(d[1])]
        else:
            o += ["0", "ARC", "8", layer, "10", f(d[0][0]), "20", f(d[0][1]), "40", f(d[1]), "50", f(d[2][0]), "51", f(d[2][1])]
    return "\n".join(o + ["0", "ENDSEC", "0", "EOF"]) + "\n"


def svg_path(ents, ox, oy, h, s):
    out = []
    for layer, kind, d in ents:
        style = 'stroke="#d33" stroke-dasharray="6 4"' if layer == "BEND" else 'stroke="#222"'
        X = lambda v: ox + v * s
        Y = lambda v: oy + (h - v) * s
        if kind == "line":
            out.append(f'<line x1="{X(d[0][0]):.2f}" y1="{Y(d[0][1]):.2f}" x2="{X(d[1][0]):.2f}" y2="{Y(d[1][1]):.2f}" {style} fill="none"/>')
        elif kind == "circle":
            out.append(f'<circle cx="{X(d[0][0]):.2f}" cy="{Y(d[0][1]):.2f}" r="{d[1] * s:.2f}" {style} fill="none"/>')
        else:
            (cx, cy), r, (a0, a1) = d
            p0 = (cx + r * math.cos(math.radians(a0)), cy + r * math.sin(math.radians(a0)))
            p1 = (cx + r * math.cos(math.radians(a1)), cy + r * math.sin(math.radians(a1)))
            large = 1 if (a1 - a0) % 360 > 180 else 0
            out.append(f'<path d="M{X(p0[0]):.2f} {Y(p0[1]):.2f} A{r * s:.2f} {r * s:.2f} 0 {large} 0 {X(p1[0]):.2f} {Y(p1[1]):.2f}" {style} fill="none"/>')
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=str(ROOT / "flat"))
    ap.add_argument("--state")
    ap.add_argument("--cache", help="reuse a saved evaluate result instead of calling Onshape")
    args = ap.parse_args()
    out = pathlib.Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    if args.cache and pathlib.Path(args.cache).exists():
        res = json.load(open(args.cache))
    else:
        st = State(args.state)
        try:
            api, ps = st.api, st.data["part_studios"]["parts"]
            parts = api("GET", f"/parts{api.dw}/e/{ps['id']}?includeFlatParts=true")
            res = {}
            for p in parts:
                if p.get("isFlattenedBody"):
                    pid = urllib.parse.quote(p["partId"], safe="")
                    res[p["name"]] = api("GET", f"/parts{api.dw}/e/{ps['id']}/partid/{pid}/bodydetails")["bodies"][0]
            if args.cache:
                json.dump(res, open(args.cache, "w"))
        finally:
            st.close()
    flats = {name: flat_from_body(body) for name, body in res.items()}
    sheet, y, scale = [], 20.0, 30.0
    for name in sorted(flats):
        ents, (w, h) = entities(flats[name])
        slug = name.lower().replace(",", "").replace(" ", "-")
        (out / f"{slug}.dxf").write_text(dxf(ents))
        t = flats[name]["thickness"]
        n_bend = sum(1 for e in ents if e[0] == "BEND")
        print(f"{name}: {w:.3f} x {h:.3f} in, {sum(1 for e in ents if e[1] == 'circle')} round holes, {n_bend} bend lines, {t:.4f} thick -> {slug}.dxf")
        sheet.append(f'<text x="20" y="{y + 14:.0f}" font-family="sans-serif" font-size="14">{name}: {w:.3f} x {h:.3f} in</text>')
        sheet += svg_path(ents, 20, y + 24, h, scale)
        y += 24 + h * scale + 30
    width = 40 + scale * max(entities(f)[1][0] for f in flats.values())
    (out / "flat-patterns.svg").write_text(
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{width:.0f}" height="{y:.0f}" viewBox="0 0 {width:.0f} {y:.0f}">'
        f'<rect width="100%" height="100%" fill="#fff"/>' + "".join(sheet) + "</svg>\n")
    print(f"wrote {len(flats)} DXF files and flat-patterns.svg to {out}")


if __name__ == "__main__":
    main()
