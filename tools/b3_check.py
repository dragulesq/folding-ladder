#!/usr/bin/env python3
"""Offline fit checks on the B3 stair, from onshape/B3_Parameters.fs. No Onshape API calls.

Prints each clearance the hardware and the stowed stair depend on, and what
it has to stay above. Run it after changing a number, before spending calls:

  python3 tools/b3_check.py

Exits 1 if a check fails.
"""
import math
import sys

from b3_model import HOOK_STEP, TREAD_MIDDLE, TREAD_TOP, Model, apply

MM = 25.4


def checks(m):
    """[(what, value in inches, the least it may be, note)]."""
    out = []
    # The door handle: the rail's flange tips, at their farthest out during the swing.
    ry, rz = m.OUT_AXLE_Y - m.CAB_AXLE_Y, m.OUT_AXLE_DROP - m.CAB_AXLE_DROP
    tip = m.RAIL_Y + m.PLATE_T + m.RAIL_FLANGE + math.hypot(ry, rz) - ry
    out.append(("Door handle to the rail's flange tips", m.HANDLE_Y - tip, 0.25,
                f"tips reach {tip:.3f} out, handle starts at {m.HANDLE_Y:.3f}; 1/2 clear needs TRAY_W {m.TRAY_W - 0.5 + m.HANDLE_Y - tip:.3f}"))
    top = m.T(TREAD_TOP) - m.CAB_AXLE_DROP + m.TRAY_Y0 + m.TRAY_W - m.CAB_AXLE_Y
    out.append(("Cabinet top to the folded top tread", m.CABINET_TOP_Z - top, 0.25, f"folded stair tops out at {top:.3f}"))

    over = m.CABINET_DEPTH - m.WALL_GAP - 2 * m.GOING
    out.append(("Cabinet front edge past the stair's front end", over, 0,
                f"cabinet is {m.CABINET_DEPTH:.3f} deep, the stair starts {m.WALL_GAP:.3f} off the wall and runs {2 * m.GOING:.3f}"))
    # The room: the open hook-on step in the entrance aisle, the open rail in the middle aisle.
    step_end = 3 * m.GOING + m.END_WASHER_T + m.END_HEAD_H
    out.append(("Entrance aisle left beside the open hook-on step", m.DINETTE_X0 - step_end, 18,
                f"aisle is {m.ENTRY_AISLE_W:.3f}, the step reaches {step_end - 2 * m.GOING:.3f} into it"))
    out.append(("Open rail to the shower wall, across the middle aisle", m.SHOWER_Y - tip, 12,
                f"middle aisle runs y {m.DINETTE_Y1:.3f} to {m.SHOWER_Y:.3f}"))
    # The stack between a lip and its ear.
    gap = m.EAR_X0 - m.TRAY_INSET - m.SHEET_T
    wave = gap - m.THRUST_T - m.BUSHING_FLANGE_T
    out.append(("Wave washer's room in a lip-to-ear gap", wave, 0.4 / MM,
                f"{gap * MM:.2f} mm gap less {m.THRUST_T * MM:.1f} thrust washer and {m.BUSHING_FLANGE_T * MM:.1f} flange"))
    out.append(("Lip bushing's sleeve short of the lip's inside face", m.SHEET_T - (m.LIP_BUSHING_LEN - m.BUSHING_FLANGE_T), 0,
                "a longer sleeve holds the thrust washer off the lip and eats the wave washer's room"))
    out.append(("Ear bushing's sleeve in the 3/16 ear", m.EAR_BUSHING_LEN - m.BUSHING_FLANGE_T, 0.6 * m.PLATE_T,
                f"ear is {m.PLATE_T * MM:.2f} mm; the sleeve may stand proud inboard"))
    flat = 2 * (m.CAB_AXLE_DROP - m.BEND_R_SHEET - m.SHEET_T)
    out.append(("Thrust washer inside the lip's bend, cabinet-side hole", flat - m.THRUST_OD, 0,
                f"up to {flat * MM:.2f} mm OD sits flat"))
    out.append(("Metal left over the hole at the top of an ear's hook", m.EAR_HOOK_R - m.HOLE_DIA / 2, 0.125, ""))
    end = m.BUSHING_FLANGE_T + 0.15 / MM + m.END_WASHER_T + m.END_HEAD_H
    out.append(("Axle screw head to the next tray's lip, folded", m.GOING - m.TRAY_D - end, 0.06, f"{end * MM:.2f} mm stands out past each lip"))
    out.append(("Keyhole opening over the pin's fender washer", m.KEYHOLE_DIA - m.END_WASHER_DIA, 1 / MM, ""))
    pin_float = m.TRAY_INSET - m.BUSHING_FLANGE_T - m.NEO_T
    out.append(("Step pin's float behind the neoprene washer", pin_float, 0.2 / MM, "back lip to the keyhole plate"))

    # Stowed: the folded step on its ledge, under the folded stair.
    mo = m.body_motions()
    t0, xg = m.T(HOOK_STEP), m.going_x(HOOK_STEP)
    stub = apply(mo["outer leg"], (xg, m.OUT_AXLE_Y + m.CAB_AXLE_Y - m.STEP_LEG_Y0, m.STEP_LEG_TOP_Z))[2]
    out.append(("Folded step's top to the folded middle tread", apply(mo["middle tread"], (0, m.TRAY_Y0, m.T(TREAD_MIDDLE)))[2] - stub, 0.125,
                f"step stands {stub:.3f} tall on its ledge, cabinet-side end down"))
    head = m.END_WASHER_T + m.END_HEAD_H
    front = apply(mo["step"], (m.STEP_AXLE_X1 + head, 0, 0))[0]
    out.append(("Wall to the stowed step's front screw heads", front + m.WALL_GAP, 0.125, ""))
    pins = apply(mo["step"], (m.STEP_AXLE_X0 - head, 0, 0))[0]
    out.append(("Stowed step's pin heads to the middle bracket's ear", m.ear_faces(TREAD_MIDDLE)[0] - pins, 0.06,
                f"pins point toward the cabinet front and reach x {pins:.3f}"))
    out.append(("Ledge shelf under the stowed step's face", m.LEDGE_SHELF - m.stow_top_y(), 0,
                f"step is {m.stow_top_y() - m.STOW_PANEL_GAP:.3f} thick, {m.STOW_PANEL_GAP:.3f} off the panel"))
    dy, dz = m.fold_delta()
    out.append(("Folded rail's plate outside the stowed step", m.RAIL_Y + dy - m.stow_top_y(), 0.125,
                f"rail rises {dz:.3f} and lands {m.RAIL_Y + dy:.3f} off the panel"))
    out.append(("Bumper squeezed by the folded tread top", m.BUMPER_H - (m.CAB_AXLE_Y - m.CAB_AXLE_DROP), 0, "the squeeze, not a gap"))
    return out


def main():
    m = Model()
    bad = 0
    for what, value, least, note in checks(m):
        ok = value >= least - 1e-9
        bad += not ok
        print(f"{'ok  ' if ok else 'FAIL'} {what:54} {value:7.3f} in ({value * MM:6.2f} mm), at least {least:.3f}" + (f"   {note}" if note else ""))
    print("all clear" if not bad else f"{bad} check(s) failed")
    sys.exit(1 if bad else 0)


if __name__ == "__main__":
    main()
