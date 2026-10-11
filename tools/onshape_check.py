#!/usr/bin/env python3
"""Load onshape/B3_AllInOne.fs into an Onshape test document and check that every feature rebuilds.

Runs tools/fs_lint.py first and stops if it finds a problem, so no API calls
are spent on code that can't compile. Then it uploads the file to the Feature
Studio "B3 test AllInOne", reads back the features it defines, and puts each
one in the Part Studio "B3 test Part Studio". It reports each feature's
status, its error text if it failed, and the parts it built.

The Onshape plan allows a limited number of API calls a year, so this keeps
them few: a first run costs about 14 calls, a rerun after a fix costs 3.
Element and feature ids are kept in a state file between runs, and every call
is appended to a log next to it.

Usage:
  python3 tools/onshape_check.py [--fold DEG] [--std STD_DIR] [--state FILE]

Needs ONSHAPE_ACCESS_KEY, ONSHAPE_SECRET_KEY and ONSHAPE_DOC_URL (a workspace
URL of the test document). Only touches the two "B3 test" elements in that
document. --fold uploads the file with FOLD_ANGLE set to DEG.
"""
from datetime import datetime, timezone
from pathlib import Path
import argparse
import json
import os
import re
import subprocess
import sys

import requests

ROOT = Path(__file__).resolve().parent.parent
FS_FILE = ROOT / "onshape" / "B3_AllInOne.fs"
BASE = "https://cad.onshape.com/api/v10"
STUDIO_NAME = "B3 test AllInOne"
PART_STUDIO_NAME = "B3 test Part Studio"
FEATURES = ["b3Reference", "b3Treads", "b3CabinetBrackets", "b3OuterRail", "b3StepClip",
            "b3PivotHardware", "b3FrontAngle", "b3StepLedge", "b3HookOnStep"]
SHARED_STATE = Path("/mnt/project-files/onshape/state.json")


class Api:
    def __init__(self, url, log_path):
        m = re.search(r"/documents/(\w+)/w/(\w+)", url)
        if not m:
            sys.exit(f"ONSHAPE_DOC_URL is not a workspace URL: {url}")
        self.dw = f"/d/{m.group(1)}/w/{m.group(2)}"
        self.log_path = log_path
        self.calls = 0
        self.s = requests.Session()
        self.s.auth = (os.environ["ONSHAPE_ACCESS_KEY"], os.environ["ONSHAPE_SECRET_KEY"])
        self.s.headers.update({"Accept": "application/json", "Content-Type": "application/json"})

    def __call__(self, method, path, body=None):
        r = self.s.request(method, BASE + path, json=body, timeout=180)
        self.calls += 1
        with open(self.log_path, "a") as log:
            log.write(f"{datetime.now(timezone.utc).isoformat(timespec='seconds')} {method} {path} {r.status_code}\n")
        if not r.ok:
            raise RuntimeError(f"{method} {path} -> {r.status_code}: {r.text[:3000]}")
        return r.json() if r.text else {}


def fs_value(v):
    """Decode a BTFSValue from the evaluate endpoint into plain Python."""
    if isinstance(v, list):
        return [fs_value(x) for x in v]
    if not isinstance(v, dict):
        return v
    inner = v.get("value", v.get("message"))
    if isinstance(inner, list) and inner and isinstance(inner[0], dict) and "key" in inner[0]:
        return {str(fs_value(e["key"])): fs_value(e["value"]) for e in inner}
    if "typeTag" in v and "value" not in v:
        return None
    return fs_value(inner)


def notices(resp):
    out = []
    for n in resp.get("notices") or []:
        msg = n.get("message")
        msg = msg.get("message") if isinstance(msg, dict) else msg
        out.append(f"{n.get('level', '')}: {msg}")
    return out


def with_fold(text, fold):
    if fold is None:
        return text
    text, n = re.subn(r"export const FOLD_ANGLE = .*?;", f"export const FOLD_ANGLE = {fold:g} * degree;", text)
    assert n == 1
    return text


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--fold", type=float, help="upload with FOLD_ANGLE set to this many degrees")
    ap.add_argument("--std", help="std library checkout for the offline lint")
    ap.add_argument("--state", help="state file (default: the project's shared folder if present)")
    ap.add_argument("--report", action="store_true", help="only re-read the Part Studio's status (1 call)")
    ap.add_argument("--relink", action="store_true", help="also repoint the Part Studio's features at the new upload")
    args = ap.parse_args()

    lint = [sys.executable, str(ROOT / "tools" / "fs_lint.py")] + ([args.std] if args.std else [])
    if subprocess.run(lint).returncode:
        sys.exit("Lint failed; fix that first, no API calls made.")

    url = os.environ.get("ONSHAPE_DOC_URL")
    if not url:
        sys.exit("Set ONSHAPE_DOC_URL to the test document's workspace URL.")
    state_path = Path(args.state) if args.state else (
        SHARED_STATE if SHARED_STATE.parent.parent.exists() else ROOT / ".onshape_state.json")
    state_path.parent.mkdir(parents=True, exist_ok=True)
    state = json.loads(state_path.read_text()) if state_path.exists() else {}
    if state.get("doc") not in (None, url):
        state = {}
    state["doc"] = url
    api = Api(url, state_path.with_name("api_calls.log"))

    def save():
        state_path.write_text(json.dumps(state, indent=1))

    try:
        bad = report(api, state) if args.report else run(api, state, save, with_fold(FS_FILE.read_text(), args.fold), args.relink)
    finally:
        save()
        total = sum(1 for _ in open(api.log_path)) if api.log_path.exists() else api.calls
        print(f"API calls this run: {api.calls}; logged in total: {total} ({api.log_path})")
    sys.exit(1 if bad else 0)


def run(api, state, save, contents, relink=False):
    # Feature Studio: create once, then overwrite its contents.
    if "studio" not in state:
        e = api("POST", f"/featurestudios{api.dw}", {"name": STUDIO_NAME})
        state["studio"] = e["id"]
        save()
    up = api("POST", f"/featurestudios{api.dw}/e/{state['studio']}",
             {"contents": contents, "rejectMicroversionSkew": False})
    for line in notices(up):
        print("studio notice", line)

    specs = api("GET", f"/featurestudios{api.dw}/e/{state['studio']}/featurespecs")
    found = {s["featureType"]: s for s in specs.get("featureSpecs", [])}
    extra = {k: v for k, v in specs.items() if k not in ("featureSpecs",)}
    print(f"studio compiled: {len(found)} feature(s): {', '.join(found) or '(none)'}")
    missing = [t for t in FEATURES if t not in found]
    if missing:
        print(f"MISSING FEATURES {missing}; specs response other fields: {json.dumps(extra)[:3000]}")
        return len(missing)

    # Part Studio: add the features once. Features from a Feature Studio in the same
    # workspace pick up its new contents by themselves; --relink repoints them anyway.
    if "part_studio" not in state:
        e = api("POST", f"/partstudios{api.dw}", {"name": PART_STUDIO_NAME})
        state["part_studio"], state["features"] = e["id"], {}
        save()
    ps = f"/partstudios{api.dw}/e/{state['part_studio']}"
    statuses = {}
    have = state.get("features", {})
    if all(t in have for t in FEATURES):
        if not relink:
            return report(api, state)
        feats = [{"btType": "BTMFeature-134", "featureId": have[t], "featureType": t,
                  "name": found[t].get("featureTypeName") or t, "namespace": found[t]["namespace"],
                  "parameters": []} for t in FEATURES]
        r = api("POST", f"{ps}/features/updates", {"features": feats})
        for t in FEATURES:
            statuses[t] = (r.get("featureStates", {}).get(have[t]) or {}).get("featureStatus", "?")
    else:
        for t in FEATURES:
            if t in have:
                continue
            feat = {"btType": "BTMFeature-134", "featureType": t, "name": found[t].get("featureTypeName") or t,
                    "namespace": found[t]["namespace"], "parameters": []}
            r = api("POST", f"{ps}/features", {"feature": feat})
            have[t] = r["feature"]["featureId"]
            state["features"] = have
            save()
            statuses[t] = (r.get("featureState") or {}).get("featureStatus", "?")

    return report(api, state, statuses)


def report(api, state, statuses=None):
    """One evaluate call: each feature's error or warning and the parts in the Part Studio."""
    statuses = statuses or {}
    have = state["features"]
    ids = ", ".join(f'"{t}" : "{have[t]}"' for t in FEATURES)
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
    var screws = [];
    var others = [];
    for (var b in evaluateQuery(context, qAllModifiableSolidBodies()))
    {
        const n = getProperty(context, { "entity" : b, "propertyType" : PropertyType.NAME });
        parts = append(parts, n);
        if (startsWith(n, "Axle screw"))
            screws = append(screws, [b, n]);
        else if (!startsWith(n, "Axle"))
            others = append(others, [b, n]);
    }
    // Each axle screw's two nearest parts other than the axles, in mm. The nearest is
    // usually the lip its own axle runs through; the second is what it must clear.
    var clearance = {};
    for (var s in screws)
    {
        var d1 = undefined;
        var n1 = "";
        var d2 = undefined;
        var n2 = "";
        for (var o in others)
        {
            const d = evDistance(context, { "side0" : s[0], "side1" : o[0] }).distance;
            if (d1 == undefined || d < d1)
            {
                d2 = d1;
                n2 = n1;
                d1 = d;
                n1 = o[1];
            }
            else if (d2 == undefined || d < d2)
            {
                d2 = d;
                n2 = o[1];
            }
        }
        if (d2 != undefined)
            clearance[s[1]] = toString(roundToPrecision(d1 / millimeter, 2)) ~ " mm to " ~ n1 ~ "; "
                ~ toString(roundToPrecision(d2 / millimeter, 2)) ~ " mm to " ~ n2;
    }
    return { "errors" : errors, "warnings" : warnings, "parts" : parts, "clearance" : clearance };
}""" % ids
    ps = f"/partstudios{api.dw}/e/{state['part_studio']}"
    ev = api("POST", f"{ps}/featurescript", {"script": script, "queries": {}})
    for line in notices(ev):
        print("evaluate notice", line)
    if ev.get("console"):
        print("console:", ev["console"][:3000])
    res = fs_value(ev.get("result"))
    if not isinstance(res, dict):
        print("could not read the evaluate result:", json.dumps(ev.get("result"))[:3000])
        return len(FEATURES)
    errors, warnings = res.get("errors") or {}, res.get("warnings") or {}
    bad = 0
    for t in FEATURES:
        status = "ERROR" if t in errors else "WARNING" if t in warnings else statuses.get(t, "OK")
        bad += status == "ERROR"
        print(f"  {t}: {status}  {errors.get(t) or warnings.get(t) or ''}".rstrip())
    parts = res.get("parts") or []
    for name, text in sorted((res.get("clearance") or {}).items(), key=lambda kv: float(kv[1].split()[0])):
        print(f"  clearance  {name}: {text}")
    print(f"parts ({len(parts)}): {', '.join(map(str, parts))}")
    print("ALL OK" if not bad else f"{bad} feature(s) failed")
    return bad

if __name__ == "__main__":
    main()
