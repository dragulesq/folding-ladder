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
    # The travel lock: a spring plunger standing in a ledge on the panel, its nose up through
    # the rail's foot flange when the stair is folded.
    mo = m.body_motions()
    hole = apply(mo["rail"], (m.LOCK_X, m.LOCK_Y, m.RAIL_FOOT_Z))
    pin = m.lock_plunger()["origin"]
    out.append(("Folded, the foot flange's hole on the plunger's line", -math.hypot(hole[0] - pin[0], hole[1] - pin[1]), -0.002 / MM,
                f"hole at x {hole[0]:.4f} y {hole[1]:.4f}, plunger at x {pin[0]:.4f} y {pin[1]:.4f}"))
    gap = hole[2] - pin[2]
    out.append(("Plunger nose through the foot flange", m.PLUNGER_NOSE - gap - m.PLATE_T, 2 / MM,
                f"{m.PLUNGER_NOSE * MM:.0f} mm travel, {gap * MM:.2f} mm gap, {m.PLATE_T * MM:.2f} mm flange; this much stands above it"))
    flat = m.RAIL_PAD_W
    out.append(("Flange metal beside the lock hole", (flat - m.LOCK_HOLE) / 2, 0.125, "hole centered on the flange's flat"))
    thread_in = m.LOCK_BLOCK_T + m.PLATE_T
    out.append(("Plunger thread in the block and shelf", thread_in, 8 / MM, f"{thread_in * MM:.1f} mm"))
    out.append(("Plunger thread left under the shelf for a jam nut", m.PLUNGER_THREAD - thread_in, 8 / MM,
                f"handle hangs to about z {m.LOCK_SHELF_TOP_Z - m.PLATE_T - (m.PLUNGER_THREAD - thread_in) - 18 / MM - 0.2:.2f}"))
    # The rail's first move out of the fold is square to the pin.
    r90, r85 = m.body_motions(90)["rail"], m.body_motions(85)["rail"]
    dy, dz = r85[1][3] - r90[1][3], r85[2][3] - r90[2][3]
    out.append(("Rail's first 5 degrees of unfolding, out from the panel against down", dy - 5 * abs(dz), 0,
                f"moves {dy:.2f} out and {dz:.2f} up or down: the pin is square to it"))
    # Coming in, the foot (flange underside and plate bottom, both at RAIL_FOOT_Z) passes over the block and shelf.
    worst = 9.0
    for a10 in range(600, 901):
        r = m.body_motions(a10 / 10)["rail"]
        y_in, y_out, z = m.RAIL_Y + r[1][3], m.RAIL_Y + m.PLATE_T + m.RAIL_FLANGE + r[1][3], m.RAIL_FOOT_Z + r[2][3]
        if y_in < pin[1] + m.LOCK_NUT_AF / 2 and y_out > pin[1] - m.LOCK_NUT_AF / 2:
            worst = min(worst, z - m.LOCK_BLOCK_TOP_Z)
        if y_in < m.LOCK_LEDGE_REACH:
            worst = min(worst, z - m.LOCK_SHELF_TOP_Z)
    out.append(("Folding, the rail's foot over the lock's block and shelf", worst, 1 / 16, "closest over the last 30 degrees"))
    web = m.T(TREAD_MIDDLE) - m.BRACKET_TOP_DROP - m.BRACKET_H
    out.append(("Lock ledge under the middle tread's cabinet bracket", web - m.LOCK_SHELF_TOP_Z, 1 / 32, ""))
    out.append(("Lock ledge clear of the front angle's panel leg", m.FA_PANEL_LEG_X0 - (m.LOCK_LEDGE_X0 + m.LOCK_LEDGE_W), 0.125, ""))
    out.append(("Rail foot pads clear of the lock ledge, either side",
                min(m.LOCK_LEDGE_X0 - (m.RAIL_PAD_X0[0] + m.RAIL_PAD_L), m.RAIL_PAD_X0[1] - (m.LOCK_LEDGE_X0 + m.LOCK_LEDGE_W)), 0.125,
                f"pads {m.RAIL_PAD_L:.3f} long at x {m.RAIL_PAD_X0[0]:.3f} and {m.RAIL_PAD_X0[1]:.3f}; hole at x {m.LOCK_X:.3f}"))
    out.append(("Lock hole centered on the foot", -abs(m.LOCK_X - (m.RAIL_FOOT_X + 2 * m.GOING) / 2), -0.001, ""))
    # The hand slot under the middle tread's bracket: clear of the bracket's screws, the clip and the plate's lower edge.
    sl, sh = m.HAND_SLOT_L / 2, m.HAND_SLOT_H / 2
    out.append(("Hand slot under the middle bracket's screw row", m.T(TREAD_MIDDLE) - m.SCREW_DROP - m.SCREW_HOLE_DIA / 2 - (m.HAND_SLOT_Z + sh), 0.5, ""))
    out.append(("Hand slot above the step clip", m.HAND_SLOT_Z - sh - (m.CLIP_Z0 + m.CLIP_H), 0.5, ""))
    x0 = m.HAND_SLOT_X - sl
    edge = m.RAIL_BACK_Z + (m.RAIL_FOOT_Z - m.RAIL_BACK_Z) * min(x0, m.RAIL_FOOT_X) / m.RAIL_FOOT_X
    out.append(("Hand slot above the rail's lower edge", m.HAND_SLOT_Z - sh - edge, 0.75, f"edge at z {edge:.2f} under the slot's back end"))
    out.append(("Hand slot to the lock's handle, folded (one-hand reach)", (m.HAND_SLOT_Z + m.FOLD_DZ) - (m.LOCK_SHELF_TOP_Z - 2.0), -99,
                "height between the folded slot and the plunger's handle"))
    # The stowed step's pins in the hook plates' slots.
    for name, y_open, drop, py, pz in (("upper", m.OUT_AXLE_Y, m.OUT_AXLE_DROP, m.STOW_PIN_UP_Y, m.STOW_PIN_UP_Z),
                                       ("lower", m.CAB_AXLE_Y, m.CAB_AXLE_DROP, m.STOW_PIN_LO_Y, m.STOW_PIN_LO_Z)):
        q = apply(mo["step"], (m.STEP_AXLE_X0, y_open, m.T(HOOK_STEP) - drop))
        out.append((f"Stowed step's {name} pin on its hook slot", -math.hypot(q[1] - py, q[2] - pz), -0.002 / MM,
                    f"pin at y {q[1]:.4f} z {q[2]:.4f}, tube end at x {q[0]:.3f}"))
    tube_end = apply(mo["step"], (m.STEP_AXLE_X0, 0, 0))[0]
    out.append(("Stowed pin's tube through the hook plate", tube_end - (m.STOW_HOOK_X0 + m.SHEET_T), 0, "the washer and head sit beyond the plate"))
    out.append(("Hook plate to the middle bracket's ear, past the pin's head", m.ear_faces(TREAD_MIDDLE)[0] - (m.STOW_HOOK_X0 + m.SHEET_T), 0.25, ""))
    lip_low = m.T(TREAD_MIDDLE) - m.CAB_AXLE_DROP - math.hypot(m.CAB_AXLE_Y - m.TRAY_Y0, m.LIP - m.CAB_AXLE_DROP)
    out.append(("Upper hook's top under the middle tread's back lip as it folds", lip_low - m.STOW_UP_Z1, 0.06, f"the lip's corner dips to {lip_low:.3f}"))
    out.append(("Stow bracket's leg under the cabinet bracket's web", web - m.LOCK_SHELF_TOP_Z, 0.03, "one leg from the hook plate to the shelf"))
    out.append(("Stow bracket's bottom under the upper pin's slot", m.STOW_PIN_UP_Z - m.KEYHOLE_SLOT_W / 2 - m.STOW_BRACKET_Z0, 0.5, ""))
    out.append(("Plunger's regular nut between the shelf's flanges", m.LOCK_LEDGE_W - 2 * m.SHEET_T - 19.6 / MM, 0.03, "19.6 mm across its corners"))
    xs = m.STOW_BRACKET_SCREW_X
    out.append(("Stow bracket's screws clear of the hook plate and shelf flanges",
                min(xs[0] - (m.STOW_HOOK_X0 + m.SHEET_T), xs[2] - (m.LOCK_LEDGE_X0 + m.SHEET_T), (m.LOCK_LEDGE_X0 + m.LOCK_LEDGE_W - m.SHEET_T) - xs[2]),
                0.2, "screw centers to the bent-out plates; a #8 pan head is 0.27 across"))
    # The step keeper, bolted inside the rail: folded, in front of the stowed step's top.
    rmo = mo["rail"]
    k_bot = m.KEEP_Z0 + rmo[2][3]
    k_y = m.RAIL_Y - m.KEEP_T + rmo[1][3]
    step_top = apply(mo["outer leg"], (m.going_x(HOOK_STEP), m.OUT_AXLE_Y + m.CAB_AXLE_Y - m.STEP_LEG_Y0, m.STEP_LEG_TOP_Z))[2]
    out.append(("Folded, the keeper reaches below the stowed step's top", step_top - k_bot, 0.5, f"keeper bottom at z {k_bot:.2f}, step top {step_top:.2f}"))
    gap = k_y - m.stow_top_y()
    out.append(("Keeper to the stowed step's face", gap, 0.125, ""))
    slot = m.STOW_UP_D - m.STOW_PIN_UP_Y
    pin_move = gap * (m.STOW_PIN_UP_Z - m.STOW_PIN_LO_Z) / (k_bot - m.STOW_PIN_LO_Z)
    out.append(("Upper pin's slot left when the step tips out onto the keeper", slot - pin_move, 0.5,
                f"the pin can move {pin_move:.2f} of the slot's {slot:.2f}"))
    tread_end = 3 * m.GOING - (m.going_x(HOOK_STEP) + m.TRAY_INSET)
    out.append(("Keeper inside the stowed step's front end", tread_end - m.KEEP_X0, 0.5, f"step's tread ends at x {tread_end:.2f}"))
    out.append(("Keeper clear of the upper hook plate", m.STOW_HOOK_X0 - (m.KEEP_X0 + m.KEEP_W), 0.06, ""))
    # Perpendicular distance from the keeper's lower screw hole to the plate's lower edge, back to foot.
    ex, ez = m.RAIL_FOOT_X, m.RAIL_FOOT_Z - m.RAIL_BACK_Z
    px, pz = m.KEEP_X0 + m.KEEP_W / 2, m.KEEP_SCREW_Z[0] - m.RAIL_BACK_Z
    dist = abs(ex * pz - ez * px) / math.hypot(ex, ez) - m.KEEP_SCREW_DIA / 2
    out.append(("Keeper's lower screw hole clear of the lower edge's bend", dist - (m.BEND_R_PLATE + m.PLATE_T), 0.1,
                "hole edge to the flange's bend zone, square to the edge"))
    out.append(("Open keeper above the floor", m.KEEP_Z0, 1.0, ""))
    worst = 9.0
    kx0, kx1 = m.KEEP_X0, m.KEEP_X0 + m.KEEP_W
    for n, body in ((TREAD_TOP, "top tread"), (TREAD_MIDDLE, "middle tread")):
        xg = m.going_x(n)
        if xg + m.TRAY_INSET > kx1 or xg + m.TRAY_INSET + m.TRAY_D < kx0:
            continue
        pts = [(y, z) for y in [m.TRAY_Y0 + m.TRAY_W * i / 60 for i in range(61)] for z in (m.T(n), m.T(n) - m.LIP)]
        for a in range(0, 91):
            mt, mr = m.body_motions(a)[body], m.body_motions(a)["rail"]
            for y, z in pts:
                q = apply(mt, (kx0, y, z))
                ry, rz = q[1] - mr[1][3], q[2] - mr[2][3]
                if m.KEEP_Z0 - 0.05 <= rz <= m.KEEP_Z1 + 0.05:
                    worst = min(worst, (m.RAIL_Y - m.KEEP_T) - ry)
    out.append(("Folding, the treads past the keeper", worst, 0.06, "closest a tread comes to the keeper's inner face"))
    stop_z = m.T(TREAD_MIDDLE) - m.STOP_PIN_DROP - m.STOP_PIN_DIA / 2
    out.append(("Upper hook's top under the cabinet bracket's stop pin", stop_z - m.STOW_UP_Z1, 0.03, "the pin sticks out to the plate's face"))
    out.append(("Upper pin's washer under that stop pin as it slides in", stop_z - (m.STOW_PIN_UP_Z + m.END_WASHER_DIA / 2), 0.125, ""))
    head = m.T(TREAD_MIDDLE) - m.CAB_AXLE_DROP - m.END_WASHER_DIA / 2
    out.append(("Front angle top under the middle tread's axle washer", head - m.FA_TOP_Z, 0.125, ""))
    # The stack between a lip and its ear.
    gap = m.EAR_X0 - m.TRAY_INSET - m.SHEET_T
    wave = gap - m.THRUST_T - m.BUSHING_FLANGE_T
    out.append(("Wave washer's room in a lip-to-ear gap", wave, 0.4 / MM,
                f"{gap * MM:.2f} mm gap less {m.THRUST_T * MM:.1f} thrust washer and {m.BUSHING_FLANGE_T * MM:.1f} flange"))
    work = 2 * wave
    out.append(("Wave washer's working room, front gap closed", work - 2 * m.WAVE_T, 0,
                f"sits at {work * MM:.2f} mm ({work:.3f} in); rated 10 lb at {m.WAVE_WORK_H:.3f}, free {m.WAVE_FREE_H:.3f}, overlap {2 * m.WAVE_T:.3f}"))
    out.append(("Wave washer's deflection left before its rated limit", (m.WAVE_FREE_H - m.WAVE_WORK_H) - (m.WAVE_FREE_H - work), 0,
                f"deflects {(m.WAVE_FREE_H - work):.3f} of {(m.WAVE_FREE_H - m.WAVE_WORK_H):.3f} rated, about {10 * (m.WAVE_FREE_H - work) / (m.WAVE_FREE_H - m.WAVE_WORK_H):.0f} lb"))
    out.append(("Wave washer's ID over the unsanded tube (12.16 mm)", m.WAVE_ID - 12.16 / MM, 0, ""))
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
