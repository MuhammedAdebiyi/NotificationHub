#!/usr/bin/env python3
"""Quick test: send, get, list."""
import json, ssl, time, sys
from urllib.request import Request, urlopen
from urllib.error import HTTPError

KEY = "nhub_live_rCi8D8rvPyidV2xD2ate9oaVBFYMU3cU"
BASE = "https://api.notificationhub.space"
EMAIL = "test@notificationhub.space"

ctx = ssl.create_default_context()
ctx.check_hostname = False
ctx.verify_mode = ssl.CERT_NONE

def api(method, path, body=None):
    data = json.dumps(body).encode() if body else None
    req = Request(f"{BASE}{path}", data=data, method=method)
    req.add_header("X-Api-Key", KEY)
    req.add_header("Content-Type", "application/json")
    try:
        with urlopen(req, context=ctx) as r:
            return json.loads(r.read())
    except HTTPError as e:
        print(f"FAIL {e.code}: {e.read().decode()}")
        sys.exit(1)

print("1) Send notification...")
r = api("POST", "/api/v1/notifications", {
    "recipientEmail": EMAIL,
    "type": "transactional",
    "channel": "Email",
    "payload": json.dumps({"subject": "SDK Test", "html": "<h1>Hello!</h1><p>Test.</p>"})
})
pid = r["publicId"]
print(f"   OK id={pid}")

time.sleep(2)
print("\n2) Get detail...")
d = api("GET", f"/api/v1/notifications/{pid}")
print(f"   OK status={d['status']} provider={d.get('provider','pending')}")

print("\n3) List notifications...")
lst = api("GET", "/api/v1/notifications?page=1&pageSize=3")
print(f"   OK total={lst['totalCount']}")
for n in lst["items"][:3]:
    print(f"   {n['publicId'][:12]}... {n['status']} {n['recipientEmail']}")

print("\nAll tests passed!")
