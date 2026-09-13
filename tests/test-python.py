"""Test NotificationHub Python SDK."""
import os, sys, json, time, ssl
from urllib.request import Request, urlopen
from urllib.error import HTTPError

API_KEY = os.environ.get("NH_API_KEY")
BASE_URL = os.environ.get("NH_BASE_URL", "https://api.notificationhub.space")
EMAIL = os.environ.get("TEST_EMAIL", "test@notificationhub.space")

if not API_KEY:
    print("Set NH_API_KEY env var"); sys.exit(1)

ctx = ssl.create_default_context()
ctx.check_hostname = False
ctx.verify_mode = ssl.CERT_NONE

def api(method, path, body=None):
    url = f"{BASE_URL}{path}"
    data = json.dumps(body).encode() if body else None
    req = Request(url, data=data, method=method)
    req.add_header("X-Api-Key", API_KEY)
    req.add_header("Content-Type", "application/json")
    try:
        with urlopen(req, context=ctx) as resp:
            return json.loads(resp.read())
    except HTTPError as e:
        print(f"✗ API {e.code}: {e.read().decode()}"); sys.exit(1)

def main():
    print("=== NotificationHub Python SDK Test ===\n")

    # 1. Send
    print("1) Sending notification...")
    result = api("POST", "/api/v1/notifications", {
        "recipientEmail": EMAIL,
        "type": "transactional",
        "channel": "email",
        "payload": json.dumps({
            "subject": f"Test from Python SDK - {time.strftime('%H:%M:%S')}",
            "html": "<h1>Hello!</h1><p>This is a test from the Python SDK.</p>",
        }),
    })
    public_id = result["publicId"]
    print(f"   ✓ Sent! ID: {public_id}")

    # 2. Get detail
    print("\n2) Getting notification detail...")
    time.sleep(2)
    detail = api("GET", f"/api/v1/notifications/{public_id}")
    print(f"   ✓ Status: {detail['status']} | Provider: {detail.get('provider', 'pending')}")

    # 3. List
    print("\n3) Listing recent notifications...")
    lst = api("GET", "/api/v1/notifications?page=1&pageSize=3")
    print(f"   ✓ Total: {lst['totalCount']} notifications")
    for n in lst["items"][:3]:
        print(f"   - {n['publicId'][:12]}... | {n['status']} | {n['recipientEmail']}")

    print("\n=== All tests passed ===")

if __name__ == "__main__":
    main()
