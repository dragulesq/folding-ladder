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

The Onshape plan allows a limited number of API calls a year. Building the
assembly costs about 25 calls, so the script does it once and records what
it made in the state file; a rerun picks up where it stopped and costs
nothing once the assembly is complete. The assembly follows the Part Studios
by itself, so rerun onshape_check.py after changing the FeatureScript, not
this. When the parts themselves are replaced (their ids change, as when they
became sheet metal), --rebuild deletes the old assembly and builds a new one.

Usage:
  python3 tools/onshape_assembly.py [--state FILE] [--dry-run] [--rebuild]

--dry-run prints the instances, mates and groups without calling the API.
"""
from urllib.parse import quote
import argparse
import sys

from onshape_api import State

NAME = "B3 test Assembly"

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
    **{f"M6 nyloc nut {i}": "M6 nyloc nut" for i in range(1, 11)},
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
# Two button-head screws through the step clip, with a nyloc nut on the rail's outer face.
for k in range(1, 3):
    HARDWARE += [
        ((f"M6 button-head screw {12 + k}", "Washer"), ("Step clip", f"Screw {k}")),
        ((f"M6 nyloc nut {8 + k}", "Rail"), ("Outer rail", f"Nut, clip {k}")),
    ]


def groups():
    """The hardware's groups: [(name, [instances])], each led by the part the rest is fixed to."""
    root = {}

    def find(i):
        while root.get(i, i) != i:
            i = root[i]
        return i

    for (child, _), (parent, _) in HARDWARE:
        root[find(child)] = find(parent)
    members = {}
    for i in [i for (c, _), (p, _) in HARDWARE for i in (p, c)]:
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


def plan(state):
    """Every instance's part, Part Studio and transform, and the mate connectors by (part, name)."""
    studios = state["part_studios"]
    for key in ("parts", "surroundings"):
        if "parts" not in studios.get(key, {}):
            sys.exit("Run tools/onshape_check.py first; the state file has no parts for the assembly.")
    mcs = {(m["part"], m["name"]): m for key in ("parts", "surroundings") for m in studios[key]["mate_connectors"]}
    part_of = dict(INSTANCES)
    instances = {}
    for name in studios["surroundings"]["parts"]:
        part_of[name] = name
        instances[name] = {"part": name, "studio": "surroundings", "fixed": True, "transform": IDENTITY}
    links = [ends for _, *ends in MATES] + HARDWARE
    missing = sorted({p for p in INSTANCES.values() if p not in studios["parts"]["parts"]}
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
    return instances, mcs


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--state", help="state file (default: the project's shared folder if present)")
    ap.add_argument("--dry-run", action="store_true", help="print the plan without calling the API")
    ap.add_argument("--rebuild", action="store_true", help="delete the assembly in the state file and build a new one")
    ap.add_argument("--no-groups", action="store_true", help="fasten the hardware with mates instead of groups")
    args = ap.parse_args()

    st = State(args.state)
    instances, mcs = plan(st.data)
    if args.dry_run:
        for name, inst in instances.items():
            t = inst["transform"]
            print(f"{name:34} {inst['part']:32} at ({t[0][3] / 0.0254:7.3f}, {t[1][3] / 0.0254:7.3f}, {t[2][3] / 0.0254:7.3f}) in"
                  + ("  fixed" if inst["fixed"] else ""))
        for kind, (a, an), (b, bn) in MATES:
            print(f"{kind:9} {a} ({an}) to {b} ({bn})")
        for name, members in groups():
            print(f"group     {name}: {', '.join(members[1:])}")
        print(f"{len(instances)} instances, {len(MATES)} mates, {len(groups())} groups")
        return
    try:
        if args.no_groups:
            st.data.setdefault("assembly", {})["no_groups"] = True
        build(st, instances, mcs, args.rebuild)
    finally:
        st.close()


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
            n = int(old.rsplit(" ", 1)[1]) + 1 if old.rsplit(" ", 1)[-1].isdigit() else 2
            keep["name"] = f"{NAME} {n}"
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
            transform_groups = []
            for name, inst in instances.items():
                ps = state["part_studios"][inst["studio"]]
                params = {"documentId": api.did, "elementId": ps["id"], "partId": ps["parts"][inst["part"]],
                          "isAssembly": False, "isWholePartStudio": False}
                if inst["fixed"]:
                    params["isFixed"] = True
                transform_groups.append({"instances": [params], "transform": flat(inst["transform"])})
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
        asm["instances"] = match(api("GET", path), instances, state)
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
    d = api("GET", path)
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
            if (inst.get("elementId") == ps["id"] and inst.get("partId") == ps["parts"][plan_inst["part"]]
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
