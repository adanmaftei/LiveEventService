import base64
import json
import os
import time
import urllib.error
import urllib.request


GRAFANA_URL = os.environ.get("GRAFANA_URL", "http://grafana:3000")
GRAFANA_USER = os.environ.get("GRAFANA_USER", "admin")
GRAFANA_PASS = os.environ.get("GRAFANA_PASS", "admin")
DASHBOARD_PATH = os.environ.get(
    "DASHBOARD_PATH", "/dashboards/liveevent-overview.json"
)


def wait_for_grafana_ready(timeout_seconds: int = 120) -> None:
    deadline = time.time() + timeout_seconds
    health_url = f"{GRAFANA_URL}/api/health"
    while time.time() < deadline:
        try:
            with urllib.request.urlopen(health_url, timeout=5) as resp:
                if resp.status == 200:
                    return
        except Exception:
            pass
        time.sleep(2)
    raise TimeoutError("Grafana did not become ready in time")


def import_dashboard() -> None:
    with open(DASHBOARD_PATH, "r", encoding="utf-8") as f:
        dashboard = json.load(f)

    payload = {
        "dashboard": dashboard,
        "overwrite": True,
        "folderId": 0,
    }
    body = json.dumps(payload).encode("utf-8")

    url = f"{GRAFANA_URL}/api/dashboards/db"
    req = urllib.request.Request(url, data=body, method="POST")
    token = base64.b64encode(f"{GRAFANA_USER}:{GRAFANA_PASS}".encode()).decode()
    req.add_header("Authorization", f"Basic {token}")
    req.add_header("Content-Type", "application/json")

    try:
        with urllib.request.urlopen(req, timeout=10) as resp:
            print(f"Imported dashboard: {resp.status}")
            print(resp.read().decode())
    except urllib.error.HTTPError as e:
        print(f"Import failed: {e.code}")
        print(e.read().decode())
        raise


if __name__ == "__main__":
    wait_for_grafana_ready()
    import_dashboard()


