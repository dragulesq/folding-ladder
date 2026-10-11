#!/usr/bin/env python3
"""Build the B3 assembly in the Onshape test document from the parts tools/onshape_check.py made.

Run tools/onshape_check.py first: it saves the part ids and mate connectors
this script uses. This script then creates the assembly "B3 test Assembly",
inserts every part as many times as the stair uses it (the reference blocks
fixed), and mates them: fastened mates for everything screwed, riveted or
clamped together, and revolute mates for the pivots, so the stair can be
folded by dragging a tread and the hook-on step's legs can be folded.

Each instance is inserted where its mates will put it, worked out from the
mate connectors, so the mates find everything already in place.

The Onshape plan allows a limited number of API calls a year. Building the
assembly costs about 50 calls (one per mate), so the script does it once and
records what it made in the state file; a rerun picks up where it stopped
and costs nothing once the assembly is complete. The assembly follows the
Part Studios by itself, so rerun onshape_check.py after changing the
FeatureScript, not this.

Usage:
  python3 tools/onshape_assembly.py [--state FILE] [--dry-run]

--dry-run prints the instances and mates without calling the API.
"""
from urllib.parse import quote
import argparse
import sys

from onshape_api import State

NAME = "B3 test Assembly"

# Instances: key -> (part name, Part Studio key). The reference blocks are added
# separately, one fixed instance of each part in the Surroundings Part Studio.
AXLES = {  # axle instance -> part
    "Tread axle 1": "Tread axle", "Tread axle 2": "Tread axle",
    "Tread axle 3": "Tread axle", "Tread axle 4": "Tread axle",
    "Step axle 1": "Step axle", "Step axle 2": "Step axle",
}
INSTANCES = {
    "Cabinet bracket 1": "Cabinet bracket", "Cabinet bracket 2": "Cabinet bracket",
    "Tread 1": "Tread", "Tread 2": "Tread",
    "Outer rail": "Outer rail", "Step clip": "Step clip",
    "Front angle": "Front angle", "Step ledge": "Step ledge",
    "Hook-on step tray": "Hook-on step tray",
    "Hook-on step leg, cabinet side": "Hook-on step leg, cabinet side",
    "Hook-on step leg, outer": "Hook-on step leg, outer",
    **AXLES,
    **{f"Stop pin {i}": "Stop pin" for i in range(1, 5)},
    **{f"M6 fender washer {i}": "M6 fender washer" for i in range(1, 13)},
    **{f"M6 button-head screw {i}": "M6 button-head screw" for i in range(1, 13)},
}

# Mates: (type, (instance, its mate connector), (instance it goes on, that one's mate connector)).
# In order: each instance is placed by the first mate that names it on the left.
MATES = [
    ("FASTENED", ("Cabinet bracket 1", "Cabinet"), ("REF cabinet", "Bracket, top tread")),
    ("FASTENED", ("Cabinet bracket 2", "Cabinet"), ("REF cabinet", "Bracket, middle tread")),
    ("FASTENED", ("Front angle", "Cabinet"), ("REF cabinet", "Front angle")),
    ("FASTENED", ("Step ledge", "Cabinet"), ("REF cabinet", "Step ledge")),
    ("FASTENED", ("Stop pin 1", "Bracket"), ("Cabinet bracket 1", "Stop pin, back")),
    ("FASTENED", ("Stop pin 2", "Bracket"), ("Cabinet bracket 1", "Stop pin, front")),
    ("FASTENED", ("Stop pin 3", "Bracket"), ("Cabinet bracket 2", "Stop pin, back")),
    ("FASTENED", ("Stop pin 4", "Bracket"), ("Cabinet bracket 2", "Stop pin, front")),
    ("FASTENED", ("Tread axle 1", "Back end"), ("Cabinet bracket 1", "Axle")),
    ("FASTENED", ("Tread axle 2", "Back end"), ("Cabinet bracket 2", "Axle")),
    ("REVOLUTE", ("Tread 1", "Cabinet axle"), ("Cabinet bracket 1", "Axle")),
    ("REVOLUTE", ("Tread 2", "Cabinet axle"), ("Cabinet bracket 2", "Axle")),
    ("REVOLUTE", ("Outer rail", "Top tread axle"), ("Tread 1", "Outer axle")),
    ("REVOLUTE", ("Outer rail", "Middle tread axle"), ("Tread 2", "Outer axle")),
    ("FASTENED", ("Tread axle 3", "Back end"), ("Outer rail", "Top tread axle")),
    ("FASTENED", ("Tread axle 4", "Back end"), ("Outer rail", "Middle tread axle")),
    ("FASTENED", ("Step clip", "Outer rail"), ("Outer rail", "Step clip")),
    ("FASTENED", ("Hook-on step tray", "Cabinet-side axle"), ("Front angle", "Hook-on step")),
    ("FASTENED", ("Step axle 1", "Back end"), ("Hook-on step tray", "Cabinet-side axle")),
    ("FASTENED", ("Step axle 2", "Back end"), ("Hook-on step tray", "Outer axle")),
    ("REVOLUTE", ("Hook-on step leg, cabinet side", "Axle"), ("Hook-on step tray", "Cabinet-side axle")),
    ("REVOLUTE", ("Hook-on step leg, outer", "Axle"), ("Hook-on step tray", "Outer axle")),
]
# A fender washer and button-head screw on each axle end.
for i, (axle, end) in enumerate((a, e) for a in AXLES for e in ("Back end", "Front end")):
    MATES += [
        ("FASTENED", (f"M6 fender washer {i + 1}", "Axle"), (axle, end)),
        ("FASTENED", (f"M6 button-head screw {i + 1}", "Washer"), (f"M6 fender washer {i + 1}", "Screw")),
    ]


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
    missing = sorted({p for p in INSTANCES.values() if p not in studios["parts"]["parts"]}
                     | {f"{part_of.get(i, i)} / {n}" for _, *ends in MATES for i, n in ends
                        if (part_of.get(i, i), n) not in mcs})
    if missing:
        sys.exit(f"Not in the Part Studios (rerun onshape_check.py?): {', '.join(missing)}")
    for _, (child, cmc), (parent, pmc) in MATES:
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
        print(f"{len(instances)} instances, {len(MATES)} mates")
        return
    try:
        build(st, instances, mcs)
    finally:
        st.close()


def build(st, instances, mcs):
    api, state = st.api, st.data
    asm = state.setdefault("assembly", {})
    if "id" not in asm:
        asm["id"] = api("POST", f"/assemblies{api.dw}", {"name": NAME})["id"]
        st.save()
    path = f"/assemblies{api.dw}/e/{asm['id']}"

    # All instances in one call, each in its own transform group.
    if "instances" not in asm:
        if not asm.get("inserted"):
            groups = []
            for name, inst in instances.items():
                ps = state["part_studios"][inst["studio"]]
                params = {"documentId": api.did, "elementId": ps["id"], "partId": ps["parts"][inst["part"]],
                          "isAssembly": False, "isWholePartStudio": False}
                if inst["fixed"]:
                    params["isFixed"] = True
                groups.append({"instances": [params], "transform": flat(inst["transform"])})
            code, r = api("POST", f"{path}/transformedinstances", {"transformGroups": groups}, check=False)
            if code >= 400 and any(i["fixed"] for i in instances.values()):
                print(f"insert with isFixed refused ({code}: {str(r)[:500]}); inserting without it")
                for g in groups:
                    g["instances"][0].pop("isFixed", None)
                api("POST", f"{path}/transformedinstances", {"transformGroups": groups})
                asm["unfixed"] = True
            elif code >= 400:
                raise RuntimeError(f"insert failed {code}: {r}")
            asm["inserted"] = True
            st.save()
        # Match the new instances to the plan by part and position.
        asm["instances"] = match(api("GET", path), instances, state)
        st.save()
    ids = asm["instances"]

    # Mates, one call each. A mate names a Part Studio mate connector by the instance
    # and the id of the operation that made it.
    done = asm.setdefault("mates", {})
    pending = asm.setdefault("pending", {})
    made = 0
    for kind, (a, an), (b, bn) in MATES:
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
    print(f"assembly: {len(ids)} instances, {len(done)} mates"
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
