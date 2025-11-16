#!/usr/bin/env python3
import json
import signal
import time
from typing import List, Dict, Any
from datetime import datetime

import requests
from colorama import init as colorama_init, Fore, Style

from config import (
    CAMERAS_JSON_PATH, DETECT_EVERY_SEC, SYNC_EVERY_SEC,
    CLEANUP_EVERY_SEC, RETENTION_DAYS,
    REMOTE_CAMERAS_URL, REMOTE_CAMERAS_TTL_SEC, REMOTE_CAMERAS_REQUIRED,
    REQUESTS_VERIFY_TLS
)
from db import init_db, store_local, cleanup_old_synced
from detect import detect_one
from sync import sync_unsent_once

colorama_init(autoreset=True)
def ok(m): print(Fore.GREEN + m + Style.RESET_ALL)
def info(m): print(Fore.CYAN + m + Style.RESET_ALL)
def warn(m): print(Fore.YELLOW + m + Style.RESET_ALL)
def err(m): print(Fore.RED + m + Style.RESET_ALL)


stop_flag = False


def _handle(sig, frame):
    global stop_flag
    stop_flag = True
    warn("\n[SYS] Stop signal received. Shutting down...")


signal.signal(signal.SIGINT, _handle)
signal.signal(signal.SIGTERM, _handle)

# ---------------- remote cameras cache ----------------
_cameras: List[Dict[str, Any]] = []
_cam_expires_at: float = 0.0

try:
    from zoneinfo import ZoneInfo  # Python 3.9+
    TORONTO_TZ = ZoneInfo("America/Toronto")
except Exception:
    TORONTO_TZ = None  # Fallback if zoneinfo isn't available


def _uniq_ids(cams: List[Dict[str, Any]]) -> None:
    ids = [c.get("id") for c in cams]
    if len(ids) != len(set(ids)):
        raise ValueError("Duplicate camera 'id' from remote/local cameras.")


def _normalize_cam(c: Dict[str, Any]) -> Dict[str, Any]:
    return {
        "key": str(c.get("key", "")).strip(),
        "id": str(c.get("id", "")).strip(),
        "location": c.get("location"),
        "rtsp": c.get("rtsp")
    }


def _fetch_remote_cameras() -> List[Dict[str, Any]]:
    if not REMOTE_CAMERAS_URL:
        return []
    try:
        r = requests.get(REMOTE_CAMERAS_URL, timeout=30,
                         verify=REQUESTS_VERIFY_TLS)
        if r.status_code != 200:
            warn(f"[REMOTE] GET /cameras -> {r.status_code}")
            return []
        data = r.json()
        if not isinstance(data["result"], list):
            warn("[REMOTE] invalid payload (expected array)")
            return []
        activeCameras = [x for x in data["result"] if x.get("isActive")]
        cams = [_normalize_cam(c)
                for c in activeCameras if isinstance(c, dict)]
        cams = [c for c in cams if c["id"]]  # require id
        _uniq_ids(cams)
        return cams
    except Exception as e:
        warn(f"[REMOTE] cameras fetch error: {e}")
        return []


def _load_local_cameras() -> List[Dict[str, Any]]:
    try:
        with open(CAMERAS_JSON_PATH, "r", encoding="utf-8") as f:
            cams = json.load(f)
        if not isinstance(cams, list):
            raise ValueError("cameras.json must be an array")
        cams = [_normalize_cam(c) for c in cams if isinstance(c, dict)]
        cams = [c for c in cams if c["id"]]
        _uniq_ids(cams)
        return cams
    except Exception as e:
        warn(f"[LOCAL] cameras.json fallback failed: {e}")
        return []


def _refresh_cameras(force: bool = False) -> None:
    global _cameras, _cam_expires_at
    now = time.time()
    if not force and now < _cam_expires_at:
        return

    cams = _fetch_remote_cameras()
    src = "REMOTE"
    # if not cams:
    #     cams = _load_local_cameras()
    #     src = "LOCAL"

    if not cams:
        if REMOTE_CAMERAS_REQUIRED:
            raise RuntimeError("No cameras available (remote required).")
        warn("[CAMERAS] none available; using empty list")
        _cameras = []
        _cam_expires_at = now + REMOTE_CAMERAS_TTL_SEC
        return

    _cameras = cams
    _cam_expires_at = now + REMOTE_CAMERAS_TTL_SEC
    info(f"[CAMERAS] {len(_cameras)} loaded from {src}")

# ------------------------------------------------------


def _toronto_now_from_ts(ts: float) -> datetime:
    """
    Convert a POSIX timestamp to Toronto local time (America/Toronto).
    Falls back to system local time if zoneinfo isn't available.
    """
    if TORONTO_TZ is not None:
        return datetime.fromtimestamp(ts, TORONTO_TZ)
    # Fallback: assume system local time is already Toronto time
    return datetime.fromtimestamp(ts)


def _detect_interval_seconds(now_ts: float) -> int:
    """
    Return capture interval (in seconds) based on Toronto local time:
    - 06:00 <= time < 18:00 -> every 5 minutes
    - otherwise             -> every 1 hour
    """
    dt = _toronto_now_from_ts(now_ts)
    hour = dt.hour

    if 6 <= hour < 18:
        # Daytime: every 5 minutes
        return 5 * 60
    else:
        # Nighttime: every 1 hour
        return 60 * 60


def main():
    info("[SYS] Initializing DB...")
    init_db()

    # First load (required before loop)
    _refresh_cameras(force=True)

    info("[SYS] Running. Press Ctrl+C to stop.")
    last_detect = 0.0
    last_sync = 0.0
    last_cleanup = 0.0
    last_cam_refresh = 0.0

    while not stop_flag:
        now = time.time()

        # refresh camera list by TTL
        if now - last_cam_refresh >= 1.0:  # check TTL every second
            _refresh_cameras()
            last_cam_refresh = now

        # detect cadence
        detect_interval = _detect_interval_seconds(now)
        if now - last_detect >= detect_interval:
            if not _cameras:
                warn("[DETECT] skipped: no cameras configured")
            else:
                for cam in _cameras:
                    cam_id = cam["key"]
                    count, raw_path, ann_path, meta = detect_one(cam)
                    meta_json = json.dumps(meta, ensure_ascii=False)
                    store_local(cam_id, count, meta_json, raw_path, ann_path)
                    ok(
                        f"[DETECT] camera={cam_id} count={count} saved "
                        f"(raw={bool(raw_path)} ann={bool(ann_path)})"
                    )
            last_detect = now

        # sync cadence (backoff is handled inside)
        if now - last_sync >= SYNC_EVERY_SEC:
            sync_unsent_once()
            last_sync = now

        # cleanup cadence
        if now - last_cleanup >= CLEANUP_EVERY_SEC:
            deleted = cleanup_old_synced(RETENTION_DAYS)
            if deleted > 0:
                warn(
                    f"[CLEANUP] Deleted {deleted} old synced rows (> {RETENTION_DAYS} days)")
            last_cleanup = now

        time.sleep(0.2)

    info("[SYS] Exiting.")


if __name__ == "__main__":
    main()
