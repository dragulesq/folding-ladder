"""Shared helpers for the Onshape scripts: the API client, the state file and BTFSValue decoding.

The Onshape plan allows a limited number of API calls a year, so every call
is appended to api_calls.log next to the state file, and element, feature and
instance ids are kept in the state file so reruns don't repeat work.
"""
from datetime import datetime, timezone
from pathlib import Path
import json
import os
import re
import sys

import requests

ROOT = Path(__file__).resolve().parent.parent
BASE = "https://cad.onshape.com/api/v10"
SHARED_STATE = Path("/mnt/project-files/onshape/state.json")


class Api:
    def __init__(self, url, log_path):
        m = re.search(r"/documents/(\w+)/w/(\w+)", url)
        if not m:
            sys.exit(f"ONSHAPE_DOC_URL is not a workspace URL: {url}")
        self.did, self.wid = m.group(1), m.group(2)
        self.dw = f"/d/{self.did}/w/{self.wid}"
        self.log_path = log_path
        self.calls = 0
        self.s = requests.Session()
        self.s.auth = (os.environ["ONSHAPE_ACCESS_KEY"], os.environ["ONSHAPE_SECRET_KEY"])
        self.s.headers.update({"Accept": "application/json", "Content-Type": "application/json"})

    def __call__(self, method, path, body=None, check=True):
        """One API call. With check=False, returns (status code, body) instead of raising on an error."""
        r = self.s.request(method, BASE + path, json=body, timeout=180)
        self.calls += 1
        with open(self.log_path, "a") as log:
            log.write(f"{datetime.now(timezone.utc).isoformat(timespec='seconds')} {method} {path} {r.status_code}\n")
        try:
            data = r.json() if r.text else {}
        except ValueError:
            data = {"text": r.text[:3000]}
        if not check:
            return r.status_code, data
        if not r.ok:
            raise RuntimeError(f"{method} {path} -> {r.status_code}: {r.text[:3000]}")
        return data


class State:
    """The state file, and an Api for the test document. Prints the call count on exit."""

    def __init__(self, path=None):
        url = os.environ.get("ONSHAPE_DOC_URL")
        if not url:
            sys.exit("Set ONSHAPE_DOC_URL to the test document's workspace URL.")
        self.path = Path(path) if path else (
            SHARED_STATE if SHARED_STATE.parent.parent.exists() else ROOT / ".onshape_state.json")
        self.path.parent.mkdir(parents=True, exist_ok=True)
        self.data = json.loads(self.path.read_text()) if self.path.exists() else {}
        if self.data.get("doc") not in (None, url):
            self.data = {}
        self.data["doc"] = url
        self.api = Api(url, self.path.with_name("api_calls.log"))

    def save(self):
        self.path.write_text(json.dumps(self.data, indent=1))

    def close(self):
        self.save()
        log = self.api.log_path
        total = sum(1 for _ in open(log)) if log.exists() else self.api.calls
        print(f"API calls this run: {self.api.calls}; logged in total: {total} ({log})")


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
