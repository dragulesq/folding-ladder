#!/usr/bin/env python3
"""Create an Onshape drawing of a sheet-metal part's flat pattern, for export to SendCutSend.

  python3 tools/flat_drawings.py "Step clip" ["Stow bracket" ...]

Each drawing ("Drawing: <part>") is in inches, has no border or title block and holds one
view of the part's flat pattern in "B3 test Part Studio", from the top at 1:1,
with tangent lines off (the view only drops them when it is also
set to quality rendering). It follows the model like any drawing. The API cannot
turn a view's bend notes off: untick them on the view by hand. About 5 calls
a drawing.
"""
import json
import sys
import time

from onshape_api import State


def modify(api, eid, request):
    r = api("POST", f"/drawings{api.dw}/e/{eid}/modify", {"description": request["description"], "jsonRequests": [request]})
    for _ in range(10):
        time.sleep(5)
        s = api("GET", f"/drawings/modify/status/{r['id']}")
        if s.get("requestState") != "ACTIVE":
            break
    out = json.loads(s.get("output") or "{}")
    if out.get("status") != "OK":
        raise SystemExit(f"drawing change failed: {s}")
    return out["results"][0]


def main():
    st = State(None)
    try:
        api, ps = st.api, st.data["part_studios"]["parts"]
        flats = {p["name"]: p["partId"] for p in api("GET", f"/parts{api.dw}/e/{ps['id']}?includeFlatParts=true") if p.get("isFlattenedBody")}
        for name in sys.argv[1:]:
            d = api("POST", f"/drawings{api.dw}/create", {"drawingName": f"Drawing: {name}", "elementId": ps["id"], "partId": flats[name],
                                                          "views": "zero", "border": False, "titleblock": False,
                                                          # Without units a drawing made through the API exports in meters.
                                                          "units": "INCH", "isFlattenedPart": True, "referenceType": 4})
            view = modify(api, d["id"], {"messageName": "onshapeCreateViews", "formatVersion": "2021-01-01", "description": "Add the flat pattern",
                                         "views": [{"viewType": "TopLevel", "position": {"x": 8.5, "y": 5.5}, "orientation": "top",
                                                    "scale": {"scaleSource": "Custom", "numerator": 1, "denumerator": 1},
                                                    "reference": {"elementId": ps["id"], "idTag": flats[name]}}]})
            modify(api, d["id"], {"messageName": "onshapeEditViews", "formatVersion": "2021-01-01", "description": "Hide tangent lines",
                                  "views": [{"viewId": view["viewId"], "showTangentLines": False, "showHiddenLines": False, "renderMode": "quality"}]})
            print(f"created Drawing: {name} ({d['id']})")
    finally:
        st.close()


if __name__ == "__main__":
    main()
