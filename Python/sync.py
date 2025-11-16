import json
import os
import time
from typing import Optional

import requests
from colorama import init as colorama_init, Fore, Style

from config import (
    API_URL, SYNC_BATCH_SIZE, BACKOFF_START, BACKOFF_MAX, REQUESTS_VERIFY_TLS,
    SEND_IMAGES_ONLY_IF_COUNT_POSITIVE, DELETE_RAW_AFTER_SUCCESS_SYNC
)
from db import get_unsynced_rows, mark_synced

colorama_init(autoreset=True)
def _ok(m): print(Fore.GREEN + m + Style.RESET_ALL)
def _info(m): print(Fore.CYAN + m + Style.RESET_ALL)
def _warn(m): print(Fore.YELLOW + m + Style.RESET_ALL)
def _err(m): print(Fore.RED + m + Style.RESET_ALL)


_current_backoff = BACKOFF_START


def _reset_backoff():
    global _current_backoff
    _current_backoff = BACKOFF_START
    _info(f"[BACKOFF] reset to {BACKOFF_START}s")


def _increase_backoff():
    global _current_backoff
    _current_backoff *= 2
    if _current_backoff >= BACKOFF_MAX:
        _warn("[BACKOFF] reached max; resetting to start")
        _reset_backoff()
    else:
        _warn(f"[BACKOFF] increased to {_current_backoff}s")


def _send(meta_json: str, raw_path: Optional[str], ann_path: Optional[str]) -> bool:
    # Build multipart. Only include frames if provided.
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
                "annotated.jpg", open(ann_path, "rb"), "image/jpeg")
        except Exception as e:
            _warn(f"[SYNC] cannot open annotated: {e}")

    try:
        r = requests.post(API_URL, files=files, timeout=30,
                          verify=REQUESTS_VERIFY_TLS)
        _info(f"[SYNC] server status: {r.status_code}")
        if r.text:
            print(r.text[:400])
        return r.status_code == 200
    except Exception as e:
        _err(f"[SYNC] HTTP error: {e}")
        return False
    finally:
        for k in ("frame_raw", "frame_annotated"):
            if k in files and hasattr(files[k][1], "close"):
                try:
                    files[k][1].close()
                except Exception:
                    pass


def sync_unsent_once() -> None:
    rows = get_unsynced_rows(SYNC_BATCH_SIZE)
    if not rows:
        return

    for row_id, ts, cam, cnt, meta_json, raw_path, ann_path in rows:
        # Build what we actually send under Option C
        use_raw = raw_path
        use_ann = ann_path

        # Fallback minimal meta if older rows
        if not meta_json:
            meta_json = json.dumps(
                {"timestamp_utc": ts, "camera_id": cam, "people": {"count": cnt}})

        ok = _send(meta_json, use_raw, use_ann)
        if ok:
            mark_synced(row_id)
            _ok(f"[SYNC] OK id={row_id}")
            _reset_backoff()

            # After success, optionally delete RAW to save disk
            if DELETE_RAW_AFTER_SUCCESS_SYNC and use_raw:
                try:
                    if os.path.isfile(use_raw):
                        os.remove(use_raw)
                except Exception:
                    pass
        else:
            _err(f"[SYNC] FAILED id={row_id}, waiting {_current_backoff}s")
            time.sleep(_current_backoff)
            _increase_backoff()
            break  # stop this pass on first failure
