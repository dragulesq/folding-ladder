#!/usr/bin/env python3
"""Build the B3 assembly in the Onshape test document from the parts tools/onshape_check.py made.

Run tools/onshape_check.py first: it saves the part ids and mate connectors
this script uses. This script then creates the assembly "B3 test Assembly",
inserts every part as many times as the stair uses it (the reference blocks
fixed), and puts them together:
- mates for the joints between the main parts: fastened where they are
  screwed or riveted together, revolute at the pivots, so the stair folds
  when you drag a tread and the hook-on step's legs fold;
- a group for each part and the hardware fixed to it (axles, stop pins,
  washers, screws, nuts), which costs one API call per group instead of one
  per piece of hardware. If Onshape refuses the group, that hardware gets
  fastened mates instead.

Each instance is inserted where its mates will put it, worked out from the
mate connectors, so the mates find everything already in place.

--stowed builds a second assembly, "B3 test Stowed", in two calls: every
part fixed where it sits when the stair is folded against the cabinet and
the folded hook-on step stands on its ledge. It has no mates, so it is a
picture of the stowed stair to look at and measure in, not a mechanism.

The Onshape plan allows a limited number of API calls a year. Building the
assembly costs about 25 calls, so the script does it once and records what
it made in the state file; a rerun picks up where it stopped and costs
nothing once the assembly is complete. The assembly follows the Part Studios
by itself, so rerun onshape_check.py after changing the FeatureScript, not
this. When the parts themselves are replaced (their ids change, as when they
became sheet metal), --rebuild deletes the old assembly and builds a new one.

Usage:
  python3 tools/onshape_assembly.py [--state FILE] [--dry-run [--offline]] [--rebuild] [--stowed]

--dry-run prints the instances, mates and groups without calling the API.
With --offline it plans from tools/b3_model.py instead of the state file, so
it works before the Part Studios have been rebuilt, and needs no Onshape keys.
"""
from urllib.parse import quote
import argparse
import sys

from b3_model import Model, mul, to_onshape

NAME = "B3 test Assembly"
STOWED_NAME = "B3 test Stowed"
REF_PARTS = ["REF floor", "REF cabinet", "REF bed-end wall", "REF mattress", "REF door handle",
             "REF side wall", "REF dinette seat", "REF shower wall"]

# Instances: key -> part name. The reference blocks are added separately, one
# fixed instance of each part in the Surroundings Part Studio.
AXLES = {  # axle instance -> part
    "Tread axle 1": "Tread axle", "Tread axle 2": "Tread axle",
    "Tread axle 3": "Tread axle", "Tread axle 4": "Tread axle",
    "Step axle 1": "Step axle", "Step axle 2": "Step axle",
}
INSTANCES = {
    # Brackets 1 and 2 on the cabinet (top and middle tread), 3 and 4 on the rail.
    **{f"Bracket {i}": "Bracket" for i in range(1, 5)},
    # Treads 1 and 2 are the top and middle treads; tread 3 is the hook-on step's tray.
    "Tread 1": "Tread", "Tread 2": "Tread", "Tread 3": "Tread",
    "Outer rail": "Outer rail", "Step clip": "Step clip",
    "Front angle": "Front angle", "Step ledge": "Step ledge",
    # Leg 1 on the cabinet side, leg 2 the same part turned to face it.
    "Hook-on step leg 1": "Hook-on step leg", "Hook-on step leg 2": "Hook-on step leg",
    **AXLES,
    **{f"Stop pin {i}": "Stop pin" for i in range(1, 5)},
    **{f"M6 fender washer {i}": "M6 fender washer" for i in range(1, 13)},
    **{f"M6 button-head screw {i}": "M6 button-head screw" for i in range(1, 15)},
    **{f"M6 flat-head screw {i}": "M6 flat-head screw" for i in range(1, 9)},
    # The cabinet brackets' screws into the 8020, through the side panel: the same part, swapped
    # for the longer McMaster screw below.
    **{f"M6 x 25 flat-head screw {i}": "M6 flat-head screw" for i in range(1, 9)},
    **{f"M6 nyloc nut {i}": "M6 nyloc nut" for i in range(1, 11)},
    # Pivot stacks: lip bushings 1 to 12 in the three trays' lips, 13 to 16 in the step legs' ears.
    **{f"Lip bushing {i}": "Lip bushing" for i in range(1, 17)},
    **{f"Ear bushing {i}": "Ear bushing" for i in range(1, 9)},
    **{f"Thrust washer {i}": "Thrust washer" for i in range(1, 13)},
    **{f"Wave washer {i}": "Wave washer" for i in range(1, 7)},
    "Neoprene washer 1": "Neoprene washer", "Neoprene washer 2": "Neoprene washer",
    # Rubber.
    "Rail foot pad 1": "Rail foot pad", "Rail foot pad 2": "Rail foot pad", "Step leg pad 1": "Step leg pad", "Step leg pad 2": "Step leg pad",
    "Ledge strip": "Ledge strip", "Panel bumper 1": "Panel bumper", "Panel bumper 2": "Panel bumper",
    # Travel locks on the panel: the ledge and tapped block the plunger stands in, under the folded
    # rail's foot, and the hook plates and turn tab that hold the stowed hook-on step.
    "Rail lock ledge": "Rail lock ledge", "Plunger jam nut": "Plunger jam nut",
    "Stow hook, upper": "Stow hook, upper", "Stow hook, lower": "Stow hook, lower", "Stow tab": "Stow tab",
}

# Mates between the main parts: (type, (instance, its mate connector), (instance it goes on, that one's
# mate connector)). In order: each instance is placed by the first mate that names it on the left.
# The treads, the rail brackets and the rail make a four-bar linkage with the cabinet brackets.
MATES = [
    ("FASTENED", ("Bracket 1", "Mounting face"), ("REF cabinet", "Bracket, top tread")),
    ("FASTENED", ("Bracket 2", "Mounting face"), ("REF cabinet", "Bracket, middle tread")),
    ("FASTENED", ("Front angle", "Cabinet"), ("REF cabinet", "Front angle")),
    ("FASTENED", ("Step ledge", "Cabinet"), ("REF cabinet", "Step ledge")),
    ("REVOLUTE", ("Tread 1", "Cabinet axle"), ("Bracket 1", "Axle")),
    ("REVOLUTE", ("Tread 2", "Cabinet axle"), ("Bracket 2", "Axle")),
    ("REVOLUTE", ("Bracket 3", "Axle, turned"), ("Tread 1", "Outer axle")),
    ("FASTENED", ("Outer rail", "Top tread bracket"), ("Bracket 3", "Mounting face")),
    ("FASTENED", ("Bracket 4", "Mounting face"), ("Outer rail", "Middle tread bracket")),
    ("REVOLUTE", ("Tread 2", "Outer axle"), ("Bracket 4", "Axle, turned")),
    ("FASTENED", ("Step clip", "Outer rail"), ("Outer rail", "Step clip")),
    ("FASTENED", ("Tread 3", "Cabinet axle"), ("Front angle", "Hook-on step")),
    ("REVOLUTE", ("Hook-on step leg 1", "Axle"), ("Tread 3", "Cabinet axle")),
    ("REVOLUTE", ("Hook-on step leg 2", "Axle, turned"), ("Tread 3", "Outer axle")),
]

# Hardware fixed to a part: ((instance, mate connector), (part instance, mate connector)), placed the
# same way. Each part and everything fixed to it, directly or through an axle, become one group.
HARDWARE = [
    (("Stop pin 1", "Bracket"), ("Bracket 1", "Stop pin, back")),
    (("Stop pin 2", "Bracket"), ("Bracket 1", "Stop pin, front")),
    (("Stop pin 3", "Bracket"), ("Bracket 2", "Stop pin, back")),
    (("Stop pin 4", "Bracket"), ("Bracket 2", "Stop pin, front")),
    (("Tread axle 1", "Back end"), ("Bracket 1", "Axle")),
    (("Tread axle 2", "Back end"), ("Bracket 2", "Axle")),
    (("Tread axle 3", "Back end"), ("Bracket 3", "Axle, turned")),
    (("Tread axle 4", "Back end"), ("Bracket 4", "Axle, turned")),
    (("Step axle 1", "Tread"), ("Tread 3", "Cabinet axle")),
    (("Step axle 2", "Tread"), ("Tread 3", "Outer axle")),
]
# A fender washer and button-head screw on each axle end.
for i, (axle, end) in enumerate((a, e) for a in AXLES for e in ("Back end", "Front end")):
    HARDWARE += [
        ((f"M6 fender washer {i + 1}", "Axle"), (axle, end)),
        ((f"M6 button-head screw {i + 1}", "Washer"), (f"M6 fender washer {i + 1}", "Screw")),
    ]
# Four flat-head screws through each rail bracket, with a nyloc nut on the rail's outer face.
for b, row in (("Bracket 3", "top"), ("Bracket 4", "middle")):
    for k in range(1, 5):
        i = k + (4 if row == "middle" else 0)
        HARDWARE += [
            ((f"M6 flat-head screw {i}", "Head"), (b, f"Screw {k}")),
            ((f"M6 nyloc nut {i}", "Rail"), ("Outer rail", f"Nut, {row} {k}")),
        ]
# Four flat-head screws through each cabinet bracket into the 8020.
for b in (1, 2):
    for k in range(1, 5):
        HARDWARE.append(((f"M6 x 25 flat-head screw {4 * (b - 1) + k}", "Head"), (f"Bracket {b}", f"Screw {k}")))
# Two button-head screws through the step clip, with a nyloc nut on the rail's outer face.
for k in range(1, 3):
    HARDWARE += [
        ((f"M6 button-head screw {12 + k}", "Washer"), ("Step clip", f"Screw {k}")),
        ((f"M6 nyloc nut {8 + k}", "Rail"), ("Outer rail", f"Nut, clip {k}")),
    ]

# Pivot stacks. At both ends of both axles, each tray has a bushing in its lip and a thrust washer
# inside the lip. One wave washer per axle, in the back gap.
for t in (1, 2, 3):
    for e, end in enumerate(("Cabinet axle", "Cabinet axle, turned", "Outer axle", "Outer axle, turned")):
        i = 4 * (t - 1) + e + 1
        HARDWARE += [
            ((f"Lip bushing {i}", "Axle"), (f"Tread {t}", end)),
            ((f"Thrust washer {i}", "Axle"), (f"Tread {t}", end)),
        ]
    for e, end in enumerate(("Cabinet axle", "Outer axle")):
        HARDWARE.append(((f"Wave washer {2 * (t - 1) + e + 1}", "Axle"), (f"Tread {t}", end)))
# A bushing in each bracket ear.
for b in range(1, 5):
    for e, end in enumerate(("Axle", "Axle, turned")):
        HARDWARE.append(((f"Ear bushing {2 * (b - 1) + e + 1}", "Axle"), (f"Bracket {b}", end)))
# The step legs' ears take lip bushings: the cabinet-side leg in its upper holes, the outer leg in its lower ones.
for i, (leg, end) in enumerate((("Hook-on step leg 1", "Axle"), ("Hook-on step leg 1", "Axle, front"),
                                ("Hook-on step leg 2", "Lower axle"), ("Hook-on step leg 2", "Axle, turned"))):
    HARDWARE.append(((f"Lip bushing {13 + i}", "Leg ear"), (leg, end)))
HARDWARE += [
    (("Neoprene washer 1", "Tread"), ("Tread 3", "Cabinet axle")),
    (("Neoprene washer 2", "Tread"), ("Tread 3", "Outer axle")),
    (("Rail foot pad 1", "Rail"), ("Outer rail", "Foot pad, back")),
    (("Rail foot pad 2", "Rail"), ("Outer rail", "Foot pad")),
    (("Step leg pad 1", "Leg"), ("Hook-on step leg 1", "Foot pad")),
    (("Step leg pad 2", "Leg"), ("Hook-on step leg 2", "Foot pad")),
    (("Ledge strip", "Ledge"), ("Step ledge", "Rubber strip")),
    (("Panel bumper 1", "Panel"), ("REF cabinet", "Bumper, top tread")),
    (("Panel bumper 2", "Panel"), ("REF cabinet", "Bumper, middle tread")),
    (("Rail lock ledge", "Cabinet"), ("REF cabinet", "Rail lock ledge")),
    (("Plunger jam nut", "Ledge"), ("REF cabinet", "Plunger")),
    (("Stow hook, upper", "Cabinet"), ("REF cabinet", "Stow hook, upper")),
    (("Stow hook, lower", "Cabinet"), ("REF cabinet", "Stow hook, lower")),
    (("Stow tab", "Hook"), ("REF cabinet", "Stow tab")),
]

# Bought parts imported into the document (McMaster's models), used in place of the modeled ones:
# instance name prefix -> import key in the state file's "imports". Each import there gives its
# Part Studio, its part ids and a frame in its own coordinates that matches the mate connector the
# modeled part is placed by, so the bought part lands where the modeled one would.
SWAPS = [
    ("M6 flat-head screw ", "92125A240"),
    ("M6 x 25 flat-head screw ", "92125A242"),
    *((f"M6 button-head screw {i}", "92095A226") for i in range(1, 13)),
    *((f"M6 button-head screw {i}", "92095A238") for i in (13, 14)),
    ("M6 fender washer ", "94316A560"),
    ("M6 nyloc nut ", "93625A250"),
    ("Wave washer ", "1775N12"),
    ("Lip bushing ", "JFM-1214-04"),
    ("Ear bushing ", "GFM-1214-06"),
]
# The connector each swapped part is placed by, in the modeled part's Part Studio.
SWAP_CONNECTOR = {"M6 flat-head screw": "Head", "M6 button-head screw": "Washer", "M6 fender washer": "Axle",
                  "M6 nyloc nut": "Rail", "Wave washer": "Axle", "Lip bushing": "Axle", "Ear bushing": "Axle"}
# A second part of an import (the nut's nylon insert) -> the instance it goes with.
EXTRA = {}
# Bought parts with no modeled stand-in: (instance, import key, part instance, its mate connector).
# Each goes where the import's frame meets that connector, and moves with that part.
ADDS = [
    ("Spring plunger", "8501A58", "REF cabinet", "Plunger"),
]


def swap_key(name):
    for prefix, key in SWAPS:
        if name == prefix or (prefix.endswith(" ") and name.startswith(prefix) and name[len(prefix):].isdigit()):
            return key
    return None


def swapped(instances, mcs, imports):
    """The instances with McMaster's parts in place of the modeled hardware, where the document has them."""
    out = {}
    for name, inst in instances.items():
        key = swap_key(name)
        if key not in imports:
            out[name] = inst
            continue
        imp = imports[key]
        t = mul(mul(inst["transform"], frame(mcs[(inst["part"], SWAP_CONNECTOR[inst["part"]])])), inverse(frame(imp["frame"])))
        for j, pid in enumerate(imp["parts"]):
            n = name if j == 0 else f"{name}, part {j + 1}"
            out[n] = {**inst, "part": imp["name"], "element": imp["element"], "partId": pid, "transform": t,
                      "composite": imp.get("composite", False)}
            if j:
                EXTRA[n] = name
    for name, key, parent, connector in ADDS:
        imp = imports.get(key)
        if imp is None:
            continue
        t = mul(mul(out[parent]["transform"], frame(mcs[(out[parent]["part"], connector)])), inverse(frame(imp["frame"])))
        for j, pid in enumerate(imp["parts"]):
            n = name if j == 0 else f"{name}, part {j + 1}"
            out[n] = {"part": imp["name"], "studio": "parts", "fixed": False, "element": imp["element"], "partId": pid,
                      "transform": t, "composite": imp.get("composite", False)}
            EXTRA[n] = parent
    return out


# Which rigid body of the fold each part belongs to (tools/b3_model.py moves the bodies). Hardware
# goes with the part it is fixed to; everything not listed stays on the cabinet.
BODIES = {
    "Tread 1": "top tread", "Tread 2": "middle tread",
    "Bracket 3": "rail", "Bracket 4": "rail", "Outer rail": "rail", "Step clip": "rail",
    "Tread 3": "step", "Hook-on step leg 1": "cabinet-side leg", "Hook-on step leg 2": "outer leg",
}


def lead_of():
    """Instance -> the part it is fixed to, directly or through other hardware."""
    root = {}

    def find(i):
        while root.get(i, i) != i:
            i = root[i]
        return i

    for (child, _), (parent, _) in HARDWARE:
        root[find(child)] = find(parent)
    for extra, with_ in EXTRA.items():
        root[find(extra)] = find(with_)
    return find


def groups():
    """The hardware's groups: [(name, [instances])], each led by the part the rest is fixed to."""
    find = lead_of()
    members = {}
    for i in [i for (c, _), (p, _) in HARDWARE for i in (p, c)] + list(EXTRA):
        members.setdefault(find(i), [])
        if i not in members[find(i)]:
            members[find(i)].append(i)
    return [(f"{lead} and its hardware", [lead] + [i for i in m if i != lead]) for lead, m in members.items()]


# 4x4 transforms as row-major nested lists, lengths in meters.
def frame(mc):
    x, z, o = mc["x"], mc["z"], mc["origin"]
    y = [z[1] * x[2] - z[2] * x[1], z[2] * x[0] - z[0] * x[2], z[0] * x[1] - z[1] * x[0]]
    return [[x[i], y[i], z[i], o[i]] for i in range(3)] + [[0, 0, 0, 1]]


def mul(a, b):
    return [[sum(a[i][k] * b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


def inverse(m):
    r = [[m[j][i] for j in range(3)] for i in range(3)]
    t = [-sum(r[i][k] * m[k][3] for k in range(3)) for i in range(3)]
    return [r[i] + [t[i]] for i in range(3)] + [[0, 0, 0, 1]]


IDENTITY = [[1 if i == j else 0 for j in range(4)] for i in range(4)]


def flat(m):
    return [round(v, 12) + 0.0 for row in m for v in row]


def plan(state=None):
    """Every instance's part, Part Studio and transform, and the mate connectors by (part, name).

    Without a state, the plan comes from tools/b3_model.py: the same placing,
    before the Part Studios have been rebuilt.
    """
    if state is None:
        mcs = {(m["part"], m["name"]): m for m in Model().onshape_connectors()}
        ref_parts, made = REF_PARTS, {part for part, _ in mcs}
    else:
        studios = state["part_studios"]
        for key in ("parts", "surroundings"):
            if "parts" not in studios.get(key, {}):
                sys.exit("Run tools/onshape_check.py first; the state file has no parts for the assembly.")
        mcs = {(m["part"], m["name"]): m for key in ("parts", "surroundings") for m in studios[key]["mate_connectors"]}
        ref_parts, made = studios["surroundings"]["parts"], studios["parts"]["parts"]
    part_of = dict(INSTANCES)
    instances = {}
    for name in ref_parts:
        part_of[name] = name
        instances[name] = {"part": name, "studio": "surroundings", "fixed": True, "transform": IDENTITY}
    links = [ends for _, *ends in MATES] + HARDWARE
    missing = sorted({p for p in INSTANCES.values() if p not in made}
                     | {f"{part_of.get(i, i)} / {n}" for ends in links for i, n in ends
                        if (part_of.get(i, i), n) not in mcs})
    if missing:
        sys.exit(f"Not in the Part Studios (rerun onshape_check.py?): {', '.join(missing)}")
    for (child, cmc), (parent, pmc) in links:
        if child in instances:
            continue
        if parent not in instances:
            sys.exit(f"Mate order: {parent} is used before it is placed")
        t = mul(mul(instances[parent]["transform"], frame(mcs[(part_of[parent], pmc)])),
                inverse(frame(mcs[(part_of[child], cmc)])))
        instances[child] = {"part": part_of[child], "studio": "parts", "fixed": False, "transform": t}
    unplaced = [i for i in INSTANCES if i not in instances]
    if unplaced:
        sys.exit(f"No mate places {', '.join(unplaced)}")
    if state is not None and state.get("imports"):
        instances = swapped(instances, mcs, state["imports"])
    return instances, mcs


STEP_BODIES = ("step", "cabinet-side leg", "outer leg")
# The stowed step hinges on its lower pin in the lower hook plate, so it can be tipped in and out.
STOW_HINGE = ("REVOLUTE", ("Tread 3", "Cabinet axle"), ("Stow hook, lower", "Pin"))


def stowed(instances):
    """The same instances where they sit with the stair folded and the step on its ledge. Everything is
    fixed except the hook-on step and its hardware, which hinge on the lower hook plate."""
    motions = {body: to_onshape(m) for body, m in Model().body_motions().items()}
    find = lead_of()
    out = {}
    for name, inst in instances.items():
        body = BODIES.get(find(name), "cabinet")
        out[name] = {**inst, "fixed": body not in STEP_BODIES, "stow_body": body,
                     "transform": mul(motions[body], inst["transform"])}
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--state", help="state file (default: the project's shared folder if present)")
    ap.add_argument("--dry-run", action="store_true", help="print the plan without calling the API")
    ap.add_argument("--offline", action="store_true", help="with --dry-run: plan from tools/b3_model.py, not the state file")
    ap.add_argument("--stowed", action="store_true", help="build B3 test Stowed: every part in its stowed place, the hook-on step hinged (5 calls)")
    ap.add_argument("--rebuild", action="store_true", help="delete the assembly in the state file and build a new one")
    ap.add_argument("--no-groups", action="store_true", help="fasten the hardware with mates instead of groups")
    args = ap.parse_args()

    if args.offline and not args.dry_run:
        sys.exit("--offline only plans; use it with --dry-run.")
    if args.offline:
        st = None
        instances, mcs = plan()
    else:
        from onshape_api import State
        st = State(args.state)
        instances, mcs = plan(st.data)
    if args.stowed:
        instances = stowed(instances)
    if args.dry_run:
        for name, inst in instances.items():
            t = inst["transform"]
            print(f"{name:34} {inst['part']:32} at ({t[0][3] / 0.0254:7.3f}, {t[1][3] / 0.0254:7.3f}, {t[2][3] / 0.0254:7.3f}) in"
                  + ("  fixed" if inst["fixed"] else ""))
        if args.stowed:
            print(f"{len(instances)} instances in {STOWED_NAME}; unfixed: {', '.join(n for n, i in instances.items() if not i['fixed'])}")
            return
        for kind, (a, an), (b, bn) in MATES:
            print(f"{kind:9} {a} ({an}) to {b} ({bn})")
        for name, members in groups():
            print(f"group     {name}: {', '.join(members[1:])}")
        print(f"{len(instances)} instances, {len(MATES)} mates, {len(groups())} groups")
        return
    try:
        if args.stowed:
            global MCS_FOR_STOWED
            MCS_FOR_STOWED = mcs
            build_stowed(st, instances, args.rebuild)
            return
        if args.no_groups:
            st.data.setdefault("assembly", {})["no_groups"] = True
        build(st, instances, mcs, args.rebuild)
    finally:
        st.close()


def next_name(old, base):
    """The name for an assembly that replaces one whose tab the API key could not delete."""
    last = old.rsplit(" ", 1)[-1]
    return f"{base} {int(last) + 1 if last.isdigit() else 2}"


def insert_groups(api, state, instances):
    """The body of one transformedinstances call: every instance in its own transform group."""
    out = []
    for inst in instances.values():
        ps = state["part_studios"][inst["studio"]]
        params = {"documentId": api.did, "elementId": inst.get("element", ps["id"]),
                  "partId": inst.get("partId") or ps["parts"][inst["part"]],
                  "isAssembly": False, "isWholePartStudio": False}
        if inst.get("composite"):
            # McMaster's models import as composite parts, which an insert skips unless asked for.
            params["includePartTypes"] = ["PARTS", "COMPOSITE_PARTS"]
        if inst["fixed"]:
            params["isFixed"] = True
        out.append({"instances": [params], "transform": flat(inst["transform"])})
    return out


MCS_FOR_STOWED = {}


def build_stowed(st, instances, rebuild):
    """Five calls: create the assembly, insert every instance where it sits stowed, read it back,
    group the hook-on step's parts and hinge them on the lower hook plate."""
    api, state = st.api, st.data
    asm = state.setdefault("stowed", {})
    if asm.get("inserted") and not rebuild:
        print(f"{asm['name']} is already built; --rebuild makes a new one after the FeatureScript's numbers change.")
        return
    if asm.get("inserted"):
        # As in build(): a key that may not delete tabs leaves the old one to delete by hand.
        code, _ = api("DELETE", f"/elements{api.dw}/e/{asm['id']}", check=False)
        old, old_id = asm["name"], asm["id"]
        asm.clear()
        if code < 300:
            print(f"deleted the old assembly, {old}")
            asm["name"] = old
        else:
            print(f"could not delete the old assembly ({code}); delete the tab \"{old}\" by hand")
            asm["name"] = next_name(old, STOWED_NAME)
            state.setdefault("abandoned_assemblies", []).append(old_id)
        st.save()
    if "id" not in asm:
        asm.setdefault("name", STOWED_NAME)
        asm["id"] = api("POST", f"/assemblies{api.dw}", {"name": asm["name"]})["id"]
        print(f"created the assembly {asm['name']}")
        st.save()
    path = f"/assemblies{api.dw}/e/{asm['id']}"
    if not asm.get("inserted"):
        api("POST", f"{path}/transformedinstances", {"transformGroups": insert_groups(api, state, instances)})
        asm["inserted"] = True
        st.save()
    # The hook-on step: one group of its parts, and a hinge on its lower pin (3 calls).
    step = [n for n, i in instances.items() if i.get("stow_body") in STEP_BODIES]
    if step and not asm.get("hinged"):
        ids = asm.get("instances") or match(api("GET", f"{path}?includeNonSolids=true"), instances, state)
        asm["instances"] = ids
        st.save()
        if not asm.get("step_group"):
            lead = STOW_HINGE[1][0]
            members = [lead] + [n for n in step if n != lead]
            r = api("POST", f"{path}/features", mate_group("Hook-on step, stowed", [ids[m] for m in members]))
            print(f"group     Hook-on step, stowed ({len(members)} parts): {(r.get('featureState') or {}).get('featureStatus')}")
            asm["step_group"] = r["feature"]["featureId"]
            st.save()
        kind, (a, an), (b, bn) = STOW_HINGE
        queries = [{"btType": "BTMPartStudioMateConnectorQuery-1324", "path": [ids[i]],
                    "featureId": MCS_FOR_STOWED[(instances[i]["part"], n)]["featureId"], "queryData": ""} for i, n in ((a, an), (b, bn))]
        r = api("POST", f"{path}/features", mate("Step hinge, stowed", kind, queries))
        status = (r.get("featureState") or {}).get("featureStatus")
        print(f"revolute  Step hinge, stowed: {status}")
        asm["hinged"] = status in ("OK", "INFO", "WARNING")
        st.save()
    print(f"{asm['name']}: {len(instances)} instances, fixed in their stowed places"
          + (f"; the hook-on step ({len(step)} parts) hinges on its lower pin" if step else ""))


def build(st, instances, mcs, rebuild):
    api, state = st.api, st.data
    asm = state.setdefault("assembly", {})
    if rebuild and "id" in asm:
        # API keys without delete permission get a 403; then the new assembly gets a new name
        # and the old tab is left for deleting by hand.
        code, _ = api("DELETE", f"/elements{api.dw}/e/{asm['id']}", check=False)
        old = asm.get("name", NAME)
        keep = {k: asm[k] for k in ("no_groups",) if k in asm}
        if code < 300:
            print(f"deleted the old assembly, {old}")
            keep["name"] = old
        else:
            print(f"could not delete the old assembly ({code}); delete the tab \"{old}\" by hand")
            keep["name"] = next_name(old, NAME)
            state.setdefault("abandoned_assemblies", []).append(asm["id"])
        asm.clear()
        asm.update(keep)
        st.save()
    if asm.get("instances") and set(asm["instances"]) != set(instances):
        sys.exit("The assembly in the state file has other instances; rerun with --rebuild to replace it.")
    if "id" not in asm:
        asm.setdefault("name", NAME)
        asm["id"] = api("POST", f"/assemblies{api.dw}", {"name": asm["name"]})["id"]
        print(f"created the assembly {asm['name']}")
        st.save()
    path = f"/assemblies{api.dw}/e/{asm['id']}"

    # All instances in one call, each in its own transform group.
    if "instances" not in asm:
        if not asm.get("inserted"):
            transform_groups = insert_groups(api, state, instances)
            code, r = api("POST", f"{path}/transformedinstances", {"transformGroups": transform_groups}, check=False)
            if code >= 400 and any(i["fixed"] for i in instances.values()):
                print(f"insert with isFixed refused ({code}: {str(r)[:500]}); inserting without it")
                for g in transform_groups:
                    g["instances"][0].pop("isFixed", None)
                api("POST", f"{path}/transformedinstances", {"transformGroups": transform_groups})
                asm["unfixed"] = True
            elif code >= 400:
                raise RuntimeError(f"insert failed {code}: {r}")
            asm["inserted"] = True
            st.save()
        # Match the new instances to the plan by part and position.
        asm["instances"] = match(api("GET", f"{path}?includeNonSolids=true"), instances, state)
        st.save()
    ids = asm["instances"]

    # Groups, one call each: a part and the hardware fixed to it move as one.
    made = 0
    done_groups = asm.setdefault("groups", {})
    for name, members in groups():
        if name in done_groups or asm.get("no_groups"):
            continue
        code, r = api("POST", f"{path}/features", mate_group(name, [ids[m] for m in members]), check=False)
        fid = (r.get("feature") or {}).get("featureId") if isinstance(r, dict) else None
        status = (r.get("featureState") or {}).get("featureStatus") if isinstance(r, dict) else None
        print(f"group     {name}: {status or code}")
        if code >= 400 or status not in ("OK", "INFO"):
            if fid:
                api("DELETE", f"{path}/features/featureid/{quote(fid, safe='')}")
            sys.exit(f"Onshape refused the group ({code}: {str(r)[:800]}). Rerun with --no-groups to fasten "
                     f"the hardware with mates instead, which costs about {len(HARDWARE)} more calls.")
        done_groups[name] = fid
        made += 1
        st.save()

    # Mates, one call each. A mate names a Part Studio mate connector by the instance
    # and the id of the operation that made it.
    done = asm.setdefault("mates", {})
    pending = asm.setdefault("pending", {})
    mates = MATES + ([("FASTENED", c, p) for c, p in HARDWARE] if asm.get("no_groups") else [])
    for kind, (a, an), (b, bn) in mates:
        label = f"{a} to {b}"
        if label in done:
            continue
        queries = [{"btType": "BTMPartStudioMateConnectorQuery-1324", "path": [ids[i]],
                    "featureId": mcs[(instances[i]["part"], n)]["featureId"], "queryData": ""} for i, n in ((a, an), (b, bn))]
        fid = pending.get(label)
        r = api("POST", f"{path}/features" + (f"/featureid/{quote(fid, safe='')}" if fid else ""), mate(label, kind, queries))
        fid = pending[label] = fid or r["feature"]["featureId"]
        st.save()
        status = (r.get("featureState") or {}).get("featureStatus")
        print(f"{kind.lower():9} {label}: {status}")
        if status not in ("OK", "INFO", "WARNING"):
            sys.exit(f"Mate {label} failed; stopped so no more calls are spent. Rerun to retry it.")
        done[label] = pending.pop(label)
        made += 1
        st.save()

    # One read to check that the mates left every instance where it was planned.
    if made:
        moved = moved_instances(api, path, instances, ids)
        print("every instance is where its mates put it" if not moved else f"moved by the mates: {', '.join(moved)}")
    print(f"assembly: {len(ids)} instances, {len(done)} mates, {len(done_groups)} groups"
          + ("; fix the REF parts by hand (right-click > Fix)" if asm.get("unfixed") else ""))


def moved_instances(api, path, instances, ids):
    """One call: the instances that are not where the plan put them."""
    d = api("GET", f"{path}?includeNonSolids=true")
    where = {tuple(o["path"]): o["transform"] for o in d["rootAssembly"].get("occurrences", [])}
    moved = []
    for name, inst in instances.items():
        t = where.get((ids[name],))
        off = max(abs(p - q) for p, q in zip(t, flat(inst["transform"]))) if t else None
        if off is None or off > 1e-6:
            moved.append(f"{name} ({'missing' if off is None else f'{off * 1000:.3f} mm off'})")
    return moved


def match(definition, instances, state):
    """Instance key -> instance id, matched by part id and transform."""
    root = definition["rootAssembly"]
    where = {tuple(o["path"]): o["transform"] for o in root.get("occurrences", [])}
    found = {}
    for inst in root.get("instances", []):
        t = where.get((inst["id"],))
        for name, plan_inst in instances.items():
            if name in found:
                continue
            ps = state["part_studios"][plan_inst["studio"]]
            if (inst.get("elementId") == plan_inst.get("element", ps["id"])
                    and inst.get("partId") == (plan_inst.get("partId") or ps["parts"][plan_inst["part"]])
                    and t and max(abs(p - q) for p, q in zip(t, flat(plan_inst["transform"]))) < 1e-6):
                found[name] = inst["id"]
                break
    missing = [n for n in instances if n not in found]
    if missing:
        raise RuntimeError(f"inserted instances not where planned: {', '.join(missing)}; "
                           f"occurrences: {str(root.get('occurrences'))[:2000]}")
    return found


def mate_group(name, ids):
    return {"feature": {
        "btType": "BTMMateGroup-65", "featureType": "mateGroup", "name": name, "namespace": "",
        "suppressed": False,
        "parameters": [
            {"btType": "BTMParameterQueryWithOccurrenceList-67", "parameterId": "occurrencesQuery",
             "queries": [{"btType": "BTMIndividualOccurrenceQuery-626", "path": [i]} for i in ids]},
        ]}}


def mate(name, kind, queries):
    return {"feature": {
        "btType": "BTMMate-64", "featureType": "mate", "name": name, "namespace": "", "version": 2,
        "returnAfterSubfeatures": False, "subFeatures": [], "suppressed": False,
        "parameters": [
            {"btType": "BTMParameterEnum-145", "namespace": "", "enumName": "Mate type", "value": kind,
             "parameterId": "mateType"},
            {"btType": "BTMParameterQueryWithOccurrenceList-67", "queries": queries,
             "parameterId": "mateConnectorsQuery"},
        ]}}


if __name__ == "__main__":
    main()
