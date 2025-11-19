import json
import os
import time
from typing import Optional

import requests
from colorama import init as colorama_init, Fore, Style

from config import (
    API_URL,
    SYNC_BATCH_SIZE,
    BACKOFF_START,
    BACKOFF_MAX,
    REQUESTS_VERIFY_TLS,
    DELETE_RAW_AFTER_SUCCESS_SYNC,
)
from db import get_unsynced_rows, mark_synced, mark_missing_files

colorama_init(autoreset=True)


def _ok(m: str) -> None:
    print(Fore.GREEN + m + Style.RESET_ALL)


def _info(m: str) -> None:
    print(Fore.CYAN + m + Style.RESET_ALL)


def _warn(m: str) -> None:
    print(Fore.YELLOW + m + Style.RESET_ALL)


def _err(m: str) -> None:
    print(Fore.RED + m + Style.RESET_ALL)


# Exponential backoff state
_current_backoff = BACKOFF_START
_next_allowed_sync_ts: float = 0.0  # POSIX timestamp; 0 means "no backoff"


def _reset_backoff() -> None:
    """Reset backoff to initial state silently unless we were in backoff."""
    global _current_backoff, _next_allowed_sync_ts

    # Only log if we were actually in a backoff window
    if _next_allowed_sync_ts != 0:
        _info(f"[BACKOFF] reset to {BACKOFF_START}s")

    _current_backoff = BACKOFF_START
    _next_allowed_sync_ts = 0.0


def _increase_backoff() -> None:
    """Increase backoff delay and schedule next allowed sync attempt."""
    global _current_backoff, _next_allowed_sync_ts
    _current_backoff *= 2
    if _current_backoff >= BACKOFF_MAX:
        _warn("[BACKOFF] reached max; resetting to start")
        _current_backoff = BACKOFF_START
    else:
        _warn(f"[BACKOFF] increased to {_current_backoff}s")

    _next_allowed_sync_ts = time.time() + _current_backoff
    _warn(f"[BACKOFF] next sync attempt after {_current_backoff}s")


def _send(meta_json: str, raw_path: Optional[str], ann_path: Optional[str]) -> bool:
    """Send one record to the cloud using multipart/form-data.

    meta_json is always sent. raw_path / ann_path are optional JPEGs.
    Returns True if the server responded with HTTP 200.
    """
    files = {"meta": (None, meta_json, "application/json")}

    if raw_path:
        try:
            files["frame_raw"] = ("raw.jpg", open(
                raw_path, "rb"), "image/jpeg")
        except Exception as e:
            _warn(f"[SYNC] cannot open raw: {e}")

    if ann_path:
        try:
            files["frame_annotated"] = (
                "annotated.jpg",
                open(ann_path, "rb"),
                "image/jpeg",
            )
        except Exception as e:
            _warn(f"[SYNC] cannot open annotated: {e}")

    try:
        r = requests.post(
            API_URL,
            files=files,
            timeout=5,  # keep relatively low so we never block for too long
            verify=REQUESTS_VERIFY_TLS,
        )
        _info(f"[SYNC] server status: {r.status_code}")
        if r.text:
            print(r.text[:400])
        return r.status_code == 200
    except Exception as e:
        _err(f"[SYNC] HTTP error: {e}")
        return False
    finally:
        # Ensure all file handles are closed
        for k in ("frame_raw", "frame_annotated"):
            if k in files and hasattr(files[k][1], "close"):
                try:
                    files[k][1].close()
                except Exception:
                    pass


def sync_unsent_once() -> None:
    """Attempt a non-blocking sync of unsent rows."""

    global _next_allowed_sync_ts

    now = time.time()

    # Respect backoff window
    if _next_allowed_sync_ts and now < _next_allowed_sync_ts:
        return

    rows = get_unsynced_rows(SYNC_BATCH_SIZE)
    if not rows:
        # nothing to sync
        return

    for row_id, ts, cam, cnt, meta_json, raw_path, ann_path in rows:

        # -------------------------
        # RAW MUST EXIST (mandatory)
        # -------------------------
        if not raw_path or not os.path.isfile(raw_path):
            _warn(
                f"[SYNC] Skipped row id={row_id}: RAW image missing -> {raw_path}"
            )
            mark_missing_files(row_id)
            mark_synced(row_id)
            continue  # move to next DB row

        use_raw = raw_path
        use_ann = ann_path

        # ---------------------------------
        # Annotated file is optional
        # ---------------------------------
        if use_ann and not os.path.isfile(use_ann):
            _warn(
                f"[SYNC] Annotated file missing for id={row_id}, continuing without it -> {use_ann}"
            )
            use_ann = None

        # Prepare meta fallback
        if not meta_json:
            meta_json = json.dumps(
                {
                    "timestamp_utc": ts,
                    "camera_id": cam,
                    "people": {"count": cnt},
                }
            )

        # ---------------------------------
        # Attempt sending (raw is guaranteed)
        # ---------------------------------
        _info(
            f"[SYNC] Sending row id={row_id} (cam={cam}) with RAW: {use_raw}"
            + (f", ANN: {use_ann}" if use_ann else ", ANN: None")
        )

        ok = _send(meta_json, use_raw, use_ann)

        if ok:
            _ok(f"[SYNC] Successfully synced row id={row_id}")
            mark_synced(row_id)
            _reset_backoff()

            # Optional cleanup
            if DELETE_RAW_AFTER_SUCCESS_SYNC and os.path.isfile(use_raw):
                try:
                    os.remove(use_raw)
                except Exception:
                    _warn(
                        f"[SYNC] Could not delete RAW file after sync -> {use_raw}")
        else:
            _err(
                f"[SYNC] Failed syncing row id={row_id}, entering backoff for {_current_backoff}s"
            )
            _increase_backoff()
            break  # stop this batch on first failure
