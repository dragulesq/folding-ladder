"""Offline model of the B3 stair: where every part and mate connector sits, open and stowed.

It costs no Onshape API calls. It reads the numbers from
onshape/B3_Parameters.fs and repeats the joints of onshape/B3_Lib.fs and the
mate connectors each feature makes, so tools/onshape_assembly.py can plan an
assembly before the Part Studios exist, tools/b3_check.py can check
clearances, and tools/stow_view.py can draw the stowed stair.
tools/onshape_check.py compares these connectors with the ones Onshape
reports, so a joint changed in the FeatureScript and not here shows up.

Lengths are in inches and positions in the B3 page's coordinates (x along the
cabinet, y out from the panel, z up), except where a name says Onshape.
"""
from pathlib import Path
import math
import re

ROOT = Path(__file__).resolve().parent.parent
PARAMS_FILE = ROOT / "onshape" / "B3_Parameters.fs"
HOOK_STEP, TREAD_MIDDLE, TREAD_TOP = 0, 1, 2
INCH = 0.0254  # meters


def load_params(text=None):
    """Every `export const NAME = value;` in B3_Parameters.fs, lengths in inches."""
    text = PARAMS_FILE.read_text() if text is None else text
    env = {"inch": 1.0, "millimeter": 1 / 25.4, "centimeter": 1 / 2.54, "gram": 1.0, "__builtins__": {}}
    for name, expr in re.findall(r"^export const (\w+) = (.*?);", text, re.M | re.S):
        env[name] = eval(expr.replace("^", "**"), env)  # the file is ours: numbers, units and earlier names
    return {k: v for k, v in env.items() if k.isupper()}


class Model:
    def __init__(self, params=None):
        self.p = params or load_params()
        for k, v in self.p.items():
            setattr(self, k, v)

    # ---- derived positions (B3_Lib.fs) ----
    def T(self, n):
        return (n + 1) * self.RISE

    def lock_plunger(self):
        """The travel lock's pin line: on top of the ledge's block, z up (b3LockPlungerJoint)."""
        return self.joint((self.LOCK_X, self.LOCK_FOLD_Y, self.LOCK_BLOCK_TOP_Z), (0, 0, 1), (1, 0, 0))

    def going_x(self, n):
        return (2 - n) * self.GOING

    def ear_faces(self, n):
        xg = self.going_x(n)
        return [xg + self.EAR_X0, xg + self.GOING - self.EAR_X0]

    # ---- joints: (origin, z axis, x axis), as in B3_Lib.fs ----
    @staticmethod
    def joint(origin, z, x):
        return {"origin": tuple(origin), "z": tuple(z), "x": tuple(x)}

    def axle_joint(self, x, y, z):
        return self.joint((x, y, z), (-1, 0, 0), (0, 1, 0))

    def axle_turned_joint(self, x, y, z):
        return self.joint((x, y, z), (1, 0, 0), (0, -1, 0))

    def panel_joint(self, x, z):
        return self.joint((x, 0, z), (0, 1, 0), (1, 0, 0))

    def cab_axle(self, n):
        return self.axle_joint(self.going_x(n) + self.TREAD_AXLE_X0, self.CAB_AXLE_Y, self.T(n) - self.CAB_AXLE_DROP)

    def out_axle(self, n):
        return self.axle_joint(self.going_x(n) + self.TREAD_AXLE_X0, self.OUT_AXLE_Y, self.T(n) - self.OUT_AXLE_DROP)

    def cab_axle_turned(self, n):
        return self.axle_turned_joint(self.going_x(n) + self.GOING - self.TREAD_AXLE_X0, self.CAB_AXLE_Y,
                                      self.T(n) - self.CAB_AXLE_DROP)

    def out_axle_turned(self, n):
        return self.axle_turned_joint(self.going_x(n) + self.GOING - self.TREAD_AXLE_X0, self.OUT_AXLE_Y,
                                      self.T(n) - self.OUT_AXLE_DROP)

    def bracket(self, n):
        return self.panel_joint(self.ear_faces(n)[0], self.T(n) - self.BRACKET_TOP_DROP - self.BRACKET_H)

    def turn_in_going(self, n, j, dz):
        xg = self.going_x(n)
        p, z, x = j["origin"], j["z"], j["x"]
        return self.joint((2 * xg + self.GOING - p[0], self.OUT_AXLE_Y + self.CAB_AXLE_Y - p[1], p[2] + dz),
                          (-z[0], -z[1], z[2]), (-x[0], -x[1], x[2]))

    def on_rail(self, n, j):
        return self.turn_in_going(n, j, self.CAB_AXLE_DROP - self.OUT_AXLE_DROP)

    def bracket_screw(self, n, k):
        return self.joint((self.going_x(n) + self.SCREW_X[k], self.PLATE_T, self.T(n) - self.SCREW_DROP), (0, 1, 0), (1, 0, 0))

    def rail_nut(self, n, k):
        hole = self.on_rail(n, self.bracket_screw(n, k))["origin"]
        return self.joint((hole[0], self.RAIL_Y + self.RAIL_T, hole[2]), (0, 1, 0), (1, 0, 0))

    def stop_pin(self, n, front):
        f = self.ear_faces(n)
        x = f[1] - self.PLATE_T if front else f[0] + self.PLATE_T
        return self.joint((x, self.STOP_PIN_Y, self.T(n) - self.STOP_PIN_DROP), (1 if front else -1, 0, 0), (0, 1, 0))

    def clip_mount(self):
        """The cabinet's step clip, where the part is built: the middle of its bend on the panel."""
        return self.panel_joint(2 * self.GOING, self.CLIP_CAB_Z)

    def step_clip(self):
        """The rail's step clip: the same point on the rail's inner face, which turns the clip upside down."""
        return self.joint((2 * self.GOING, self.RAIL_Y, self.CLIP_RAIL_Z), (0, -1, 0), (1, 0, 0))

    def clip_screw(self, k):
        return self.joint((self.CLIP_HOLE_X - (0 if k < 2 else 1) * self.CLIP_HOLE_BACK, self.SHEET_T,
                           self.CLIP_CAB_Z + (-1 if k % 2 == 0 else 1) * self.CLIP_HOLE_DZ), (0, 1, 0), (1, 0, 0))

    def clip_nut(self, k):
        return self.joint((self.CLIP_HOLE_X - self.CLIP_HOLE_BACK, self.RAIL_Y + self.RAIL_T, self.CLIP_RAIL_Z + (1 if k == 0 else -1) * self.CLIP_HOLE_DZ), (0, 1, 0), (1, 0, 0))

    def rail_pad(self):
        return self.joint((self.RAIL_PAD_X0[1], self.RAIL_Y + self.RAIL_T + self.RAIL_FLANGE - self.RAIL_PAD_W, self.RAIL_FOOT_Z),
                          (0, 0, -1), (1, 0, 0))

    def step_pad(self):
        return self.joint((self.going_x(HOOK_STEP) + self.GOING / 2, self.STEP_LEG_Y0 + self.STEP_LEG_FLANGE / 2,
                           self.STEP_LEG_BOTTOM_Z), (0, 0, -1), (1, 0, 0))

    def leg_lock(self):
        """The cabinet-side leg's plunger: thread end on the front ear's outer face, z along the nose."""
        return self.joint((self.going_x(HOOK_STEP) + self.GOING - self.EAR_X0, self.CAB_AXLE_Y + self.LEG_LOCK_DY, self.T(HOOK_STEP) - self.LEG_LOCK_DROP),
                          (1, 0, 0), (0, 0, -1))

    def leg_lock_turned(self):
        """The outer leg's plunger, on the leg as built at the cabinet-side leg's place."""
        return self.joint((self.going_x(HOOK_STEP) + self.EAR_X0, self.CAB_AXLE_Y + self.LEG_LOCK_DY, self.T(HOOK_STEP) - self.LEG_LOCK_DROP),
                          (-1, 0, 0), (0, 0, 1))

    def bumper(self, n):
        return self.panel_joint(self.going_x(n) + self.GOING / 2, self.T(n) - self.CAB_AXLE_DROP + self.BUMPER_UP)

    # ---- the mate connectors each feature makes: {(part, name): joint} ----
    def connectors(self):
        top, mid, step = TREAD_TOP, TREAD_MIDDLE, HOOK_STEP
        axle = self.cab_axle(top)
        p = axle["origin"]
        under = self.axle_joint(p[0] - self.END_WASHER_T, p[1], p[2])
        step_axle = self.axle_joint(self.STEP_AXLE_X0, self.CAB_AXLE_Y, self.T(step) - self.CAB_AXLE_DROP)
        c = {
            ("REF cabinet", "Bracket, top tread"): self.bracket(top),
            ("REF cabinet", "Bracket, middle tread"): self.bracket(mid),
            ("REF cabinet", "Step clip"): self.clip_mount(),
            ("REF cabinet", "Step ledge"): self.panel_joint(0, self.LEDGE_TOP_Z - self.SHEET_T),
            ("REF cabinet", "Bumper, top tread"): self.bumper(top),
            ("REF cabinet", "Bumper, middle tread"): self.bumper(mid),
            ("Tread", "Cabinet axle"): axle,
            ("Tread", "Outer axle"): self.out_axle(top),
            ("Tread", "Cabinet axle, turned"): self.cab_axle_turned(top),
            ("Tread", "Outer axle, turned"): self.out_axle_turned(top),
            ("Bracket", "Mounting face"): self.bracket(top),
            ("Bracket", "Axle"): axle,
            ("Bracket", "Axle, turned"): self.cab_axle_turned(top),
            ("Bracket", "Stop pin, back"): self.stop_pin(top, False),
            ("Bracket", "Stop pin, front"): self.stop_pin(top, True),
            ("Outer rail", "Top tread bracket"): self.on_rail(top, self.bracket(top)),
            ("Outer rail", "Middle tread bracket"): self.on_rail(mid, self.bracket(mid)),
            ("Outer rail", "Step clip"): self.step_clip(),
            ("Outer rail", "Foot pad"): self.rail_pad(),
            ("Step clip", "Mount"): self.clip_mount(),
            ("Step clip", "Hook-on step"): self.cab_axle(step),
            ("REF cabinet", "Stow bracket"): self.panel_joint(self.STOW_HOOK_X0, self.STOW_BRACKET_Z0),
            ("Stow bracket", "Cabinet"): self.panel_joint(self.STOW_HOOK_X0, self.STOW_BRACKET_Z0),
            ("REF cabinet", "Plunger"): self.lock_plunger(),
            ("Plunger jam nut", "Ledge"): self.lock_plunger(),
            ("Step ledge", "Pin"): self.joint((self.STOW_PIN_X, self.STOW_PIN_LO_Y, self.STOW_PIN_LO_Z), (1, 0, 0), (0, 0, 1)),
            ("Outer rail", "Foot pad, back"): self.joint((self.RAIL_PAD_X0[0], self.RAIL_Y + self.RAIL_T + self.RAIL_FLANGE - self.RAIL_PAD_W, self.RAIL_FOOT_Z), (0, 0, -1), (1, 0, 0)),
            ("Outer rail", "Step bumper"): self.joint((self.RAIL_BUMPER_X, self.RAIL_Y, self.RAIL_BUMPER_Z), (0, -1, 0), (1, 0, 0)),
            ("Tread axle", "Back end"): axle,
            ("Tread axle", "Front end"): self.joint((p[0] + self.TREAD_AXLE_LEN, p[1], p[2]), (1, 0, 0), (0, 1, 0)),
            ("Step axle", "Back end"): step_axle,
            ("Step axle", "Front end"): self.joint((self.STEP_AXLE_X1, p[1], step_axle["origin"][2]), (1, 0, 0), (0, 1, 0)),
            ("Step axle", "Tread"): self.cab_axle(step),
            ("M6 fender washer", "Axle"): axle,
            ("M6 fender washer", "Screw"): under,
            ("M6 button-head screw", "Washer"): under,
            ("Stop pin", "Bracket"): self.stop_pin(top, False),
            ("M6 nyloc nut", "Rail"): self.rail_nut(top, 0),
            ("Step ledge", "Cabinet"): self.panel_joint(0, self.LEDGE_TOP_Z - self.SHEET_T),
            ("Step ledge", "Rubber strip"): self.joint((0, 0, self.LEDGE_TOP_Z), (0, 0, 1), (1, 0, 0)),
            ("Hook-on step leg", "Axle"): self.cab_axle(step),
            ("Hook-on step leg", "Axle, turned"): self.turn_in_going(step, self.out_axle(step), 0),
            ("Hook-on step leg", "Axle, front"): self.cab_axle_turned(step),
            ("Hook-on step leg", "Lower axle"): self.axle_joint(self.going_x(step) + self.TREAD_AXLE_X0, self.CAB_AXLE_Y,
                                                               self.T(step) - self.OUT_AXLE_DROP),
            ("Hook-on step leg", "Foot pad"): self.step_pad(),
            ("Hook-on step leg", "Lock"): self.leg_lock(),
            ("Hook-on step leg", "Lock, turned"): self.leg_lock_turned(),
            ("M6 jam nut", "Ear"): self.leg_lock(),
            ("Lip bushing", "Axle"): axle,
            ("Lip bushing", "Leg ear"): self.axle_joint(p[0] - (self.EAR_X0 - self.TRAY_INSET), p[1], p[2]),
            ("Ear bushing", "Axle"): axle,
            ("Thrust washer", "Axle"): axle,
            ("Wave washer", "Axle"): axle,
            ("Neoprene washer", "Tread"): self.cab_axle(step),
            ("Rail foot pad", "Rail"): self.rail_pad(),
            ("Step leg pad", "Leg"): self.step_pad(),
            ("Ledge strip", "Ledge"): self.joint((0, 0, self.LEDGE_TOP_Z), (0, 0, 1), (1, 0, 0)),
            ("Panel bumper", "Panel"): self.bumper(top),
        }
        for k in range(len(self.SCREW_X)):
            c[("Bracket", f"Screw {k + 1}")] = self.bracket_screw(top, k)
            c[("Outer rail", f"Screw, top {k + 1}")] = self.rail_nut(top, k)
            c[("Outer rail", f"Screw, middle {k + 1}")] = self.rail_nut(mid, k)
        for k in range(2):
            c[("Outer rail", f"Nut, clip {k + 1}")] = self.clip_nut(k)
        for k in range(4):
            c[("Step clip", f"Screw {k + 1}")] = self.clip_screw(k)
        return c

    def onshape_connectors(self):
        """The connectors as tools/onshape_check.py saves them: meters, Onshape's axes (its Y is -y)."""
        def flip(v, scale=1.0):
            return [v[0] * scale + 0.0, -v[1] * scale + 0.0, v[2] * scale + 0.0]
        return [{"part": part, "name": name, "origin": flip(j["origin"], INCH), "x": flip(j["x"]), "z": flip(j["z"])}
                for (part, name), j in self.connectors().items()]

    # ---- the fold: where each rigid body goes from open to stowed, as 4x4 matrices on page coordinates ----
    def fold_delta(self):
        """How far the outer rail moves, (dy, dz): its axles ride the treads' outer pivots through a quarter turn."""
        ry, rz = self.OUT_AXLE_Y - self.CAB_AXLE_Y, self.CAB_AXLE_DROP - self.OUT_AXLE_DROP
        return (-rz - ry, ry - rz)

    def stow_top_y(self):
        """The folded step's tread face, out from the panel, when it stands on the ledge."""
        return self.STOW_PANEL_GAP + self.OUT_AXLE_DROP + self.CAB_AXLE_Y - self.STEP_LEG_Y0

    def body_motions(self, angle=90.0):
        """Body name -> motion from the open stair to the stair folded by angle degrees (90 is stowed).

        The hook-on step only has a stowed place, so its three bodies are at
        the stowed pose for any angle above zero.
        """
        a = math.radians(angle)

        def about_x(cy, cz, t):
            c, s = math.cos(t), math.sin(t)
            return [[1, 0, 0, 0], [0, c, -s, cy - c * cy + s * cz], [0, s, c, cz - s * cy - c * cz], [0, 0, 0, 1]]

        ident = about_x(0, 0, 0)
        m = {"cabinet": ident}
        for name, n in (("top tread", TREAD_TOP), ("middle tread", TREAD_MIDDLE)):
            m[name] = about_x(self.CAB_AXLE_Y, self.T(n) - self.CAB_AXLE_DROP, a)
        out = apply(m["top tread"], self.out_axle(TREAD_TOP)["origin"])
        o = self.out_axle(TREAD_TOP)["origin"]
        m["rail"] = [[1, 0, 0, 0], [0, 1, 0, out[1] - o[1]], [0, 0, 1, out[2] - o[2]], [0, 0, 0, 1]]
        if angle <= 0:
            m.update({"step": ident, "cabinet-side leg": ident, "outer leg": ident})
            return m
        # The folded step stands on end on the ledge's rubber strip, cabinet-side end down,
        # tread face to the room, in the top tread's going: x -> 3 GOING - x, y -> up, z -> out.
        t0 = self.T(HOOK_STEP)
        stand = [[-1, 0, 0, 3 * self.GOING],
                 [0, 0, 1, self.stow_top_y() - t0],
                 [0, 1, 0, self.LEDGE_TOP_Z + self.LEDGE_STRIP_T - self.TRAY_Y0],
                 [0, 0, 0, 1]]
        m["step"] = stand
        m["cabinet-side leg"] = mul(stand, about_x(self.CAB_AXLE_Y, t0 - self.CAB_AXLE_DROP, math.pi / 2))
        m["outer leg"] = mul(stand, about_x(self.OUT_AXLE_Y, t0 - self.OUT_AXLE_DROP, -math.pi / 2))
        return m


def mul(a, b):
    return [[sum(a[i][k] * b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


def apply(m, p):
    return tuple(m[i][0] * p[0] + m[i][1] * p[1] + m[i][2] * p[2] + m[i][3] for i in range(3))


def to_onshape(m):
    """A motion on page coordinates (inches) as one on Onshape's (meters, Y flipped)."""
    f = [1, -1, 1]
    return [[m[i][j] * f[i] * f[j] for j in range(3)] + [m[i][3] * f[i] * INCH] for i in range(3)] + [[0, 0, 0, 1]]


def compare_connectors(predicted, actual, tol=1e-6):
    """Differences between this model's connectors and Onshape's (both as saved in the state file)."""
    have = {(m["part"], m["name"]): m for m in actual}
    out = []
    for m in predicted:
        a = have.get((m["part"], m["name"]))
        if a is None:
            out.append(f"{m['part']} / {m['name']}: not in the Part Studios")
            continue
        off = max(abs(u - v) for key in ("origin", "x", "z") for u, v in zip(m[key], a[key]))
        if off > tol:
            out.append(f"{m['part']} / {m['name']}: off by {off * 1000:.3f} mm (or a turned axis)")
    extra = sorted(set(have) - {(m["part"], m["name"]) for m in predicted})
    return out + [f"{p} / {n}: in the Part Studios, not in tools/b3_model.py" for p, n in extra]
