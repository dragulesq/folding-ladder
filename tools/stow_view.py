#!/usr/bin/env python3
"""Draw the stowed B3 stair from the offline model. No Onshape API calls.

Two views, to scale, as SVG:
  end   looking along the cabinet from its front edge: the panel on the left,
        the room to the right. Shows how far the folded stair and the folded
        hook-on step stand off the panel, with the open stair as a dashed ghost.
  face  looking at the panel from the room: where the folded parts sit on it.

Parts are drawn as their bounding boxes (the rail's plate as its outline), so
bends, radii and hardware are left out; the sizes and positions are the model's.

  python3 tools/stow_view.py            # writes drawings/stowed_end.svg and drawings/stowed_face.svg
  python3 tools/stow_view.py --dir DIR  # somewhere else
"""
import argparse
from pathlib import Path

from b3_model import HOOK_STEP, ROOT, TREAD_MIDDLE, TREAD_TOP, Model, apply

S = 14.0  # pixels per inch

STYLE = """
.bg{fill:#fff}.panel{fill:#d9d4c7;stroke:#8a8473}.floor{stroke:#555;stroke-width:1.5}
.tread{fill:#c9d6e8;stroke:#34506f}.rail{fill:#e3e7ec;stroke:#3d4650}.step{fill:#f2d9a6;stroke:#7a5a12}
.fixed{fill:#b9bec6;stroke:#3d4650}.rubber{fill:#444;stroke:#111}.handle{fill:#e9b7ae;stroke:#8f2f1e}
.ghost{fill:none;stroke:#8a8f98;stroke-dasharray:4 3}.dim{stroke:#b3261e;fill:none;stroke-width:1}
.dimt{fill:#b3261e;font:600 11px system-ui,sans-serif}.lbl{fill:#333;font:11px system-ui,sans-serif}
.ttl{fill:#111;font:600 13px system-ui,sans-serif}rect,polygon,circle{stroke-width:1}
"""


def frac(v):
    """Inches to the nearest 1/16 as a workshop fraction."""
    n = round(v * 16)
    whole, r = divmod(abs(n), 16)
    sign = "-" if n < 0 else ""
    if r == 0:
        return f'{sign}{whole}"'
    d = 16
    while r % 2 == 0:
        r, d = r // 2, d // 2
    return f'{sign}{whole} {r}/{d}"' if whole else f'{sign}{r}/{d}"'


def parts(m):
    """[(css class, body, (x0, x1, y0, y1, z0, z1))] in the open stair, page coordinates."""
    out = []
    for n, body, cls in ((TREAD_TOP, "top tread", "tread"), (TREAD_MIDDLE, "middle tread", "tread"), (HOOK_STEP, "step", "step")):
        x0 = m.going_x(n) + m.TRAY_INSET
        out.append((cls, body, (x0, x0 + m.TRAY_D, m.TRAY_Y0, m.TRAY_Y0 + m.TRAY_W, m.T(n) - m.LIP, m.T(n))))
    for n in (TREAD_TOP, TREAD_MIDDLE):
        f = m.ear_faces(n)
        z0 = m.T(n) - m.BRACKET_TOP_DROP - m.BRACKET_H
        out.append(("fixed", "cabinet", (f[0], f[1], 0, m.PLATE_T, z0, z0 + m.BRACKET_H)))
        for x in (f[0], f[1] - m.PLATE_T):
            out.append(("fixed", "cabinet", (x, x + m.PLATE_T, 0, m.EAR_W, z0, m.T(n) - m.CAB_AXLE_DROP + m.EAR_HOOK_R)))
        b = m.bumper(n)["origin"]
        r = m.BUMPER_DIA / 2
        out.append(("rubber", "cabinet", (b[0] - r, b[0] + r, 0, m.BUMPER_H, b[2] - r, b[2] + r)))
    # Hook-on step legs: flanges and ears out from each web's outer face.
    xs = m.going_x(HOOK_STEP)
    mirror = m.OUT_AXLE_Y + m.CAB_AXLE_Y
    for body, flip in (("cabinet-side leg", False), ("outer leg", True)):
        for reach, z0 in ((m.STEP_LEG_FLANGE, m.STEP_LEG_BOTTOM_Z), (m.STEP_LEG_EAR, m.STEP_LEG_EAR_Z)):
            ya, yb = m.STEP_LEG_Y0, m.STEP_LEG_Y0 + reach
            if flip:
                ya, yb = mirror - yb, mirror - ya
            out.append(("step", body, (xs + m.TRAY_INSET + m.SHEET_T, xs + m.TRAY_INSET + m.TRAY_D - m.SHEET_T, ya, yb, z0, m.STEP_LEG_TOP_Z)))
    # Rail: flanges' outline and the plate, the step clip and the foot pad ride with it.
    top = m.T(TREAD_TOP) - m.RAIL_STEP_DROP
    y1 = m.RAIL_Y + m.PLATE_T
    out.append(("rail", "rail", (0, 2 * m.GOING, m.RAIL_Y, y1 + m.RAIL_FLANGE, m.RAIL_FOOT_Z, top)))
    out.append(("fixed", "rail", (m.CLIP_X0, 2 * m.GOING, m.CLIP_KEY_LEG_Y0, m.RAIL_Y, m.CLIP_Z0, m.CLIP_Z0 + m.CLIP_H)))
    out.append(("rubber", "rail", (m.RAIL_FOOT_X, 2 * m.GOING, y1 + m.RAIL_FLANGE - m.RAIL_PAD_W, y1 + m.RAIL_FLANGE,
                                   m.RAIL_FOOT_Z - m.RAIL_PAD_T, m.RAIL_FOOT_Z)))
    # Fixed to the cabinet: the step's ledge with its rubber strip, and the front angle.
    out.append(("fixed", "cabinet", (0, m.LEDGE_LEN, 0, m.LEDGE_SHELF, m.LEDGE_TOP_Z - m.SHEET_T, m.LEDGE_TOP_Z)))
    out.append(("fixed", "cabinet", (0, m.LEDGE_LEN, 0, m.SHEET_T, m.LEDGE_TOP_Z - m.LEDGE_LEG_H, m.LEDGE_TOP_Z)))
    out.append(("rubber", "cabinet", (0, m.LEDGE_LEN, 0, m.LEDGE_SHELF, m.LEDGE_TOP_Z, m.LEDGE_TOP_Z + m.LEDGE_STRIP_T)))
    out.append(("fixed", "cabinet", (m.FA_PANEL_LEG_X0, m.FA_X1, 0, m.SHEET_T, m.FA_BOTTOM_Z, m.FA_PANEL_LEG_TOP)))
    out.append(("fixed", "cabinet", (m.FA_X1 - m.SHEET_T, m.FA_X1, 0, m.FA_OUT_LEG, m.FA_BOTTOM_Z, m.FA_TOP_Z)))
    return out


def rail_outline(m):
    """The rail plate's corners in (x, z), open."""
    top, mid = m.T(TREAD_TOP) - m.RAIL_STEP_DROP, m.T(TREAD_MIDDLE) - m.RAIL_STEP_DROP
    return [(0, top), (m.GOING, top), (m.GOING, mid), (2 * m.GOING, mid), (2 * m.GOING, m.RAIL_FOOT_Z),
            (m.RAIL_FOOT_X, m.RAIL_FOOT_Z), (0, m.RAIL_BACK_Z)]


def moved(box, motion):
    """A box's extents after a motion (every motion here is a quarter turn or a slide)."""
    x0, x1, y0, y1, z0, z1 = box
    pts = [apply(motion, (x, y, z)) for x in (x0, x1) for y in (y0, y1) for z in (z0, z1)]
    return [f(p[i] for p in pts) for i in range(3) for f in (min, max)]


class View:
    """One orthographic view: axis 0 is x (face view) or y (end view) across, z up."""

    def __init__(self, across, lo, hi, zmax, title):
        self.a, self.lo, self.hi, self.zmax = across, lo, hi, zmax
        self.pad_l, self.pad_t, self.pad_b = 14, 30, 36
        self.w = (hi - lo) * S + 2 * self.pad_l
        self.h = zmax * S + self.pad_t + self.pad_b
        self.out = [f'<rect class="bg" width="{self.w:.0f}" height="{self.h:.0f}"/>',
                    f'<text class="ttl" x="{self.pad_l}" y="18">{title}</text>']

    def X(self, v):
        return self.pad_l + (v - self.lo) * S

    def Z(self, z):
        return self.pad_t + (self.zmax - z) * S

    def rect(self, cls, a0, a1, z0, z1, title=""):
        tip = f"<title>{title}</title>" if title else ""
        self.out.append(f'<rect class="{cls}" x="{self.X(a0):.1f}" y="{self.Z(z1):.1f}" width="{(a1 - a0) * S:.1f}" '
                        f'height="{(z1 - z0) * S:.1f}">{tip}</rect>')

    def box(self, cls, ext, title=""):
        i = 2 * self.a
        self.rect(cls, ext[i], ext[i + 1], ext[4], ext[5], title)

    def poly(self, cls, pts):
        self.out.append(f'<polygon class="{cls}" points="' + " ".join(f"{self.X(a):.1f},{self.Z(z):.1f}" for a, z in pts) + '"/>')

    def line(self, cls, a0, z0, a1, z1):
        self.out.append(f'<line class="{cls}" x1="{self.X(a0):.1f}" y1="{self.Z(z0):.1f}" x2="{self.X(a1):.1f}" y2="{self.Z(z1):.1f}"/>')

    def text(self, cls, a, z, s, anchor="start", dx=0, dy=0):
        self.out.append(f'<text class="{cls}" x="{self.X(a) + dx:.1f}" y="{self.Z(z) + dy:.1f}" text-anchor="{anchor}">{s}</text>')

    def hdim(self, a0, a1, z, label, anchor="start"):
        self.line("dim", a0, z, a1, z)
        for a in (a0, a1):
            self.line("dim", a, z - 0.35, a, z + 0.35)
        self.text("dimt", a1 if anchor == "start" else a0, z, label, anchor, 5 if anchor == "start" else -5, 4)

    def vdim(self, a, z0, z1, label, side=1):
        self.line("dim", a, z0, a, z1)
        for z in (z0, z1):
            self.line("dim", a - 0.35, z, a + 0.35, z)
        self.text("dimt", a, (z0 + z1) / 2, label, "start" if side > 0 else "end", 6 * side, 4)

    def svg(self):
        return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {self.w:.0f} {self.h:.0f}" width="{self.w:.0f}" '
                f'height="{self.h:.0f}" role="img">' + "".join(self.out) + "</svg>")


NAMES = {"top tread": "Top tread, folded", "middle tread": "Middle tread, folded", "step": "Hook-on step, stowed on its ledge",
         "cabinet-side leg": "Hook-on step leg, folded under the step", "outer leg": "Hook-on step leg, folded under the step",
         "rail": "Outer rail, lifted against the folded treads", "cabinet": "Fixed to the cabinet"}


def numbers(m):
    """The stowed sizes the drawings dimension, in inches."""
    mo = m.body_motions()
    ext = {}
    for cls, body, box in parts(m):
        e = moved(box, mo[body])
        g = "step" if body in ("step", "cabinet-side leg", "outer leg") else body
        o = ext.setdefault(g, list(e))
        for i in range(0, 6, 2):
            o[i], o[i + 1] = min(o[i], e[i]), max(o[i + 1], e[i + 1])
    dy, dz = m.fold_delta()
    return {"depth": ext["rail"][3], "plate": m.RAIL_Y + dy, "treads": max(ext["top tread"][3], ext["middle tread"][3]),
            "top": ext["top tread"][5], "step": ext["step"], "rail": ext["rail"], "rise": dz,
            "top_tread": ext["top tread"], "middle_tread": ext["middle tread"]}


def end_view(m):
    v = View(1, -2.0, 22.5, 50.0, "End view, stowed (open stair dashed)")
    mo, open_ = m.body_motions(), m.body_motions(0)
    n = numbers(m)
    v.rect("panel", -2.0, 0, 0, m.CABINET_TOP_Z, "Cabinet side panel")
    for cls, body, box in parts(m):
        if body != "cabinet":
            v.box("ghost", moved(box, open_[body]))
    v.rect("handle", m.HANDLE_Y, min(m.HANDLE_Y + m.HANDLE_W, 22.5), m.HANDLE_Z - m.HANDLE_H / 2, m.HANDLE_Z + m.HANDLE_H / 2, "Cab door handle")
    order = {"rail": 0, "top tread": 1, "middle tread": 1, "cabinet": 2, "cabinet-side leg": 3, "outer leg": 3, "step": 4}
    for cls, body, box in sorted(parts(m), key=lambda p: order[p[1]]):
        v.box(cls, moved(box, mo[body]), NAMES[body])
    v.line("floor", -2.0, 0, 22.5, 0)
    v.hdim(0, n["depth"], 48.9, f'{frac(n["depth"])} to the rail\'s flange tips')
    v.hdim(0, n["treads"], 38.0, f'{frac(n["treads"])} treads')
    v.hdim(0, n["plate"], 33.0, f'{frac(n["plate"])} to the rail plate')
    v.hdim(0, n["step"][3], 21.0, f'{frac(n["step"][3])} step')
    v.vdim(8.0, 0, n["top"], f'{frac(n["top"])} folded top', 1)
    v.vdim(5.6, 0, n["step"][5], f'{frac(n["step"][5])} step top', 1)
    v.text("lbl", m.HANDLE_Y + 0.75, m.HANDLE_Z + 0.5, "door handle", "middle", 0, -6)
    v.text("lbl", m.HANDLE_Y + 0.75, m.HANDLE_Z - 0.5, f'starts {frac(m.HANDLE_Y)} out', "middle", 0, 14)
    v.text("lbl", -1.0, 49.0, "panel", "middle")
    v.text("lbl", 11.0, -1.2, "floor, room side", "middle")
    return v.svg()


def face_view(m):
    v = View(0, -1.5, 25.0, 50.0, "Facing the panel, stowed (open stair dashed)")
    mo, open_ = m.body_motions(), m.body_motions(0)
    n = numbers(m)
    front = m.CABINET_DEPTH - m.WALL_GAP
    v.rect("panel", -m.WALL_GAP, front, 0, m.CABINET_TOP_Z, "Cabinet side panel")
    for cls, body, box in parts(m):
        if body in ("step", "top tread", "middle tread"):
            v.box("ghost", moved(box, open_[body]))
    v.poly("ghost", rail_outline(m))
    behind = [p for p in parts(m) if p[1] != "rail"]
    order = {"cabinet": 0, "top tread": 1, "middle tread": 1, "cabinet-side leg": 2, "outer leg": 2, "step": 3}
    for cls, body, box in sorted(behind, key=lambda p: order[p[1]]):
        v.box(cls, moved(box, mo[body]), NAMES[body])
    v.out.append('<g opacity="0.55">')
    v.poly("rail", [(x, z + n["rise"]) for x, z in rail_outline(m)])
    v.out.append("</g>")
    v.line("floor", -1.5, 0, 19.0, 0)
    v.line("floor", -m.WALL_GAP, 0, -m.WALL_GAP, 50.0)
    s = n["step"]
    v.vdim(13.2, 0, n["top"], f'{frac(n["top"])} folded top', 1)
    v.vdim(-0.9, s[4], s[5], "", 1)
    v.text("dimt", s[0], s[5], f'step {frac(s[5] - s[4])} tall, {frac(s[1] - s[0])} wide', "start", 2, -6)
    v.hdim(0, 12.0, -1.0, '12" along the cabinet')
    v.text("lbl", -m.WALL_GAP, 49.0, "bed-end wall", "start", 4)
    v.text("lbl", front, 48.6, "cabinet front edge", "end", -2)
    v.text("lbl", 15.0, 11.2, "step, open", "middle")
    return v.svg()


def with_style(svg):
    return svg.replace(">", f"><style>{STYLE}</style>", 1)


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--dir", default=str(ROOT / "drawings"))
    args = ap.parse_args()
    m = Model()
    out = Path(args.dir)
    out.mkdir(parents=True, exist_ok=True)
    for name, svg in (("stowed_end.svg", end_view(m)), ("stowed_face.svg", face_view(m))):
        (out / name).write_text(with_style(svg) + "\n")
        print("wrote", out / name)
    n = numbers(m)
    print(f'stowed: {n["depth"]:.3f} off the panel at the rail\'s flange tips, {n["plate"]:.3f} to its plate, '
          f'treads {n["treads"]:.3f}, step {n["step"][3]:.3f}; folded top {n["top"]:.3f}; '
          f'step z {n["step"][4]:.3f} to {n["step"][5]:.3f}, x {n["step"][0]:.3f} to {n["step"][1]:.3f}')


if __name__ == "__main__":
    main()
