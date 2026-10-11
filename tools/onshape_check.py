#!/usr/bin/env python3
"""Load onshape/B3_AllInOne.fs into an Onshape test document and check that every feature rebuilds.

Runs tools/fs_lint.py first and stops if it finds a problem, so no API calls
are spent on code that can't compile. Then it uploads the file to the Feature
Studio "B3 test AllInOne", reads back the features it defines, and puts them
in two Part Studios: "B3 test Part Studio" for the stair's parts, one of
each, and "B3 test Surroundings" for the reference blocks. It reports each
feature's status, its error text if it failed, the parts it built and their
mate connectors, and saves the part and mate connector ids for
tools/onshape_assembly.py.

The Onshape plan allows a limited number of API calls a year, so this keeps
them few: a first run costs about 16 calls, a rerun after a fix costs 4, and
--report costs 2.

Usage:
  python3 tools/onshape_check.py [--report] [--std STD_DIR] [--state FILE]

Needs ONSHAPE_ACCESS_KEY, ONSHAPE_SECRET_KEY and ONSHAPE_DOC_URL (a workspace
URL of the test document). Only touches the "B3 test" elements in that
document.
"""
import argparse
import json
import subprocess
import sys

from onshape_api import ROOT, State, fs_value, notices

FS_FILE = ROOT / "onshape" / "B3_AllInOne.fs"
STUDIO_NAME = "B3 test AllInOne"
# Part Studio key in the state file -> (tab name, the features it holds).
PART_STUDIOS = {
    "parts": ("B3 test Part Studio", ["b3Treads", "b3CabinetBrackets", "b3OuterRail", "b3StepClip",
                                      "b3PivotHardware", "b3FrontAngle", "b3StepLedge", "b3HookOnStep"]),
    "surroundings": ("B3 test Surroundings", ["b3Reference"]),
}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--std", help="std library checkout for the offline lint")
    ap.add_argument("--state", help="state file (default: the project's shared folder if present)")
    ap.add_argument("--report", action="store_true", help="only re-read the Part Studios (2 calls)")
    args = ap.parse_args()

    lint = [sys.executable, str(ROOT / "tools" / "fs_lint.py")] + ([args.std] if args.std else [])
    if subprocess.run(lint).returncode:
        sys.exit("Lint failed; fix that first, no API calls made.")

    st = State(args.state)
    migrate(st.data)
    try:
        bad = 0 if args.report else upload(st)
        if not bad:
            bad = sum(report(st, key) for key in PART_STUDIOS)
    finally:
        st.close()
    sys.exit(1 if bad else 0)


def migrate(state):
    """State files from before the Surroundings Part Studio kept one Part Studio at the top level."""
    if "part_studio" in state:
        state["part_studios"] = {"parts": {"id": state.pop("part_studio"), "features": state.pop("features", {})}}
    state.setdefault("part_studios", {})


def upload(st):
    api, state = st.api, st.data
    # Feature Studio: create once, then overwrite its contents.
    if "studio" not in state:
        e = api("POST", f"/featurestudios{api.dw}", {"name": STUDIO_NAME})
        state["studio"] = e["id"]
        st.save()
    up = api("POST", f"/featurestudios{api.dw}/e/{state['studio']}",
             {"contents": FS_FILE.read_text(), "rejectMicroversionSkew": False})
    for line in notices(up):
        print("studio notice", line)

    specs = api("GET", f"/featurestudios{api.dw}/e/{state['studio']}/featurespecs")
    found = {s["featureType"]: s for s in specs.get("featureSpecs", [])}
    print(f"studio compiled: {len(found)} feature(s): {', '.join(found) or '(none)'}")
    missing = [t for _, types in PART_STUDIOS.values() for t in types if t not in found]
    if missing:
        extra = {k: v for k, v in specs.items() if k != "featureSpecs"}
        print(f"MISSING FEATURES {missing}; specs response other fields: {json.dumps(extra)[:3000]}")
        return len(missing)

    # Part Studios: add the features once. Features from a Feature Studio in the same
    # workspace pick up its new contents by themselves.
    for key, (name, types) in PART_STUDIOS.items():
        ps = state["part_studios"].setdefault(key, {})
        if "id" not in ps:
            ps.update(id=api("POST", f"/partstudios{api.dw}", {"name": name})["id"], features={})
            st.save()
        path = f"/partstudios{api.dw}/e/{ps['id']}/features"
        have = ps["features"]
        for t in [t for t in have if t not in types]:
            api("DELETE", f"{path}/featureid/{have.pop(t)}")
            st.save()
        for t in types:
            if t not in have:
                feat = {"btType": "BTMFeature-134", "featureType": t, "name": found[t].get("featureTypeName") or t,
                        "namespace": found[t]["namespace"], "parameters": []}
                have[t] = api("POST", path, {"feature": feat})["feature"]["featureId"]
                st.save()
    return 0


def report(st, key):
    """One evaluate call: each feature's error or warning, the parts and their mate connectors."""
    api, state = st.api, st.data
    name, types = PART_STUDIOS[key]
    ps = state["part_studios"][key]
    ids = ", ".join(f'"{t}" : "{ps["features"][t]}"' for t in types)
    script = """function(context is Context, queries is map)
{
    const ids = { %s };
    var errors = {};
    var warnings = {};
    for (var t, fid in ids)
    {
        const e = getFeatureError(context, makeId(fid));
        if (e != undefined)
            errors[t] = toString(e);
        const w = getFeatureWarning(context, makeId(fid));
        if (w != undefined)
            warnings[t] = toString(w);
    }
    var parts = [];
    for (var b in evaluateQuery(context, qSheetMetalFlatFilter(qAllModifiableSolidBodies(), SMFlatType.NO)))
    {
        const bb = evBox3d(context, { "topology" : b, "tight" : true });
        parts = append(parts, [getProperty(context, { "entity" : b, "propertyType" : PropertyType.NAME }),
                    transientQueriesToStrings(b), bb.minCorner / inch, bb.maxCorner / inch]);
    }
    var flats = [];
    for (var b in evaluateQuery(context, qSheetMetalFlatFilter(qEverything(EntityType.BODY), SMFlatType.YES)))
    {
        const bb = evBox3d(context, { "topology" : b, "tight" : true });
        flats = append(flats, [try silent(getProperty(context, { "entity" : b, "propertyType" : PropertyType.NAME })),
                    (bb.maxCorner - bb.minCorner) / inch]);
    }
    var mcs = [];
    for (var mc in evaluateQuery(context, qHasAttribute("b3Mc")))
    {
        const a = getAttribute(context, { "entity" : mc, "name" : "b3Mc" });
        const cs = evMateConnector(context, { "mateConnector" : mc });
        mcs = append(mcs, { "part" : a.part, "name" : a.name, "featureId" : a.featureId,
                    "origin" : [cs.origin[0] / meter, cs.origin[1] / meter, cs.origin[2] / meter],
                    "x" : [cs.xAxis[0], cs.xAxis[1], cs.xAxis[2]], "z" : [cs.zAxis[0], cs.zAxis[1], cs.zAxis[2]] });
    }
    return { "errors" : errors, "warnings" : warnings, "parts" : parts, "mcs" : mcs, "flats" : flats };
}""" % ids
    ev = api("POST", f"/partstudios{api.dw}/e/{ps['id']}/featurescript", {"script": script, "queries": {}})
    for line in notices(ev):
        print("evaluate notice", line)
    if ev.get("console"):
        print("console:", ev["console"][:3000])
    res = fs_value(ev.get("result"))
    if not isinstance(res, dict):
        print("could not read the evaluate result:", json.dumps(ev.get("result"))[:3000])
        return len(types)
    errors, warnings = res.get("errors") or {}, res.get("warnings") or {}
    print(f"{name}:")
    bad = 0
    for t in types:
        status = "ERROR" if t in errors else "WARNING" if t in warnings else "OK"
        bad += status == "ERROR"
        print(f"  {t}: {status}  {errors.get(t) or warnings.get(t) or ''}".rstrip())
    parts = res.get("parts") or []
    mcs = res.get("mcs") or []
    print(f"  parts ({len(parts)}):")
    for p in parts:
        lo, hi = p[2], p[3]
        # Onshape's Y is the page's -y.
        print(f"    {p[0]:34} x {lo[0]:7.3f} to {hi[0]:7.3f}  y {-hi[1]:7.3f} to {-lo[1]:7.3f}  z {lo[2]:7.3f} to {hi[2]:7.3f}")
    flats = res.get("flats") or []
    for f in flats:
        size = sorted((abs(v) for v in f[1]), reverse=True)
        print(f"  flat pattern {f[0] or '(unnamed)'}: {size[0]:.3f} x {size[1]:.3f} x {size[2]:.3f} in")
    for p in sorted({m["part"] for m in mcs}):
        print(f"  mate connectors on {p}: {', '.join(m['name'] for m in mcs if m['part'] == p)}")
    ps["parts"] = {p[0]: p[1] for p in parts}
    ps["mate_connectors"] = mcs
    print("  all OK" if not bad else f"  {bad} feature(s) failed")
    return bad


if __name__ == "__main__":
    main()
