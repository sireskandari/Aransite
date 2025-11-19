"""
YOLOv11 detection + tracking, with dynamic target classes fetched from your API.

- Reads TEST_FRAME_PATH (local image) or grabs one RTSP frame.
- Fetches target class names from REMOTE_TARGETS_URL (cached TTL).
- Maps class names -> YOLO class IDs (e.g., "person" -> 0).
- Runs model.track(classes=[...]) using those IDs.
- Saves RAW and (if any detections) ANNOTATED frames.
- Returns (count, raw_path, annotated_path, meta).
"""

import os
import time
from datetime import datetime
from typing import Any, Dict, Tuple, Optional, List
import json
import cv2
import numpy as np
import requests
from ultralytics import YOLO

from config import (
    FRAME_ROOT, FRAME_WIDTH, FRAME_HEIGHT, MODEL_NAME, TEST_FRAME_PATH,
    REMOTE_TARGETS_URL, REMOTE_TARGETS_TTL_SEC, REQUESTS_VERIFY_TLS,
    DETECTION_ENABLED,
)

# ------------------ model (lazy) ------------------
_MODEL: YOLO | None = None


def _get_model() -> YOLO:
    global _MODEL
    if _MODEL is None:
        _MODEL = YOLO(MODEL_NAME)  # auto-downloads on first use
    return _MODEL


# ------------------ targets cache ------------------

_targets_cache: Dict[str, Any] = {
    "expires_at": 0.0,
    "default": ["person"],   # fallback if API not reachable
    "by_camera": {},         # cam_id -> [targets]
}


def _now() -> float:
    return time.time()


def _fetch_targets() -> None:
    """Fill _targets_cache from REMOTE_TARGETS_URL (ApiResponse<List<DetectTargetResponse>>), with graceful fallbacks."""
    # Ensure we always move the expiry forward, even if we bail out
    def _bump_expiry():
        _targets_cache["expires_at"] = _now() + REMOTE_TARGETS_TTL_SEC

    if not REMOTE_TARGETS_URL:
        _bump_expiry()
        return

    try:
        r = requests.get(REMOTE_TARGETS_URL, timeout=30,
                         verify=REQUESTS_VERIFY_TLS)
        if r.status_code != 200:
            _bump_expiry()
            return

        data = r.json()
        # Accept either { "targets": [...] } or wrapped in ApiResponse { "data": {...} }
        if isinstance(data, dict) and "data" in data:
            data = data["data"]

        default = data.get("default") or data.get("targets") or ["all"]
        cams = data.get("cameras") or []

        by_cam: Dict[str, List[str]] = {}
        for c in cams:
            cid = str(c.get("id") or c.get("key") or "").strip()
            if not cid:
                continue
            t = c.get("targets") or default
            by_cam[cid] = [str(x).lower() for x in t]

        _targets_cache["default"] = [str(x).lower() for x in default]
        _targets_cache["by_camera"] = by_cam
        _bump_expiry()
    except Exception:
        # On any error: just extend TTL and keep old cache
        _bump_expiry()


def _get_targets_for_camera(cam_id: str) -> List[str]:
    """Return a list of target class names (lowercase) for this camera."""
    if _now() >= _targets_cache["expires_at"]:
        _fetch_targets()
    return _targets_cache["by_camera"].get(cam_id, _targets_cache["default"])

# ------------------ utils ------------------


def _ensure_dir(path: str) -> None:
    os.makedirs(path, exist_ok=True)


def _save_jpg(dir_path: str, cam_id: str, suffix: str, img) -> str:
    ts = datetime.utcnow().strftime("%Y%m%dT%H%M%S")
    name = f"{cam_id}_{ts}_{suffix}.jpg"
    path = os.path.join(dir_path, name)
    cv2.imwrite(path, img)
    return path


def _grab_raw_frame(camera: Dict) -> np.ndarray:
    import time
    import cv2
    import numpy as np
    from config import FRAME_WIDTH, FRAME_HEIGHT

    rtsp = (camera or {}).get("rtsp")
    if rtsp:
        cap = cv2.VideoCapture(rtsp, cv2.CAP_FFMPEG)
        if cap.isOpened():
            try:
                cap.set(cv2.CAP_PROP_BUFFERSIZE, 1)
            except Exception:
                pass

            t0 = time.time()
            while time.time() - t0 < 3.0:  # warm up ≤3s
                ret, frame = cap.read()
                if ret and frame is not None and frame.size:
                    # treat “all black” (decoder/pipeline) as unusable
                    if frame.mean() > 1.0 or frame.var() > 1.0:
                        cap.release()
                        return frame
                time.sleep(0.02)
        cap.release()

    # synthetic fallback (keeps pipeline alive)
    img = np.zeros((int(FRAME_HEIGHT), int(FRAME_WIDTH), 3), dtype=np.uint8)
    img[:] = (20, 20, 20)
    return img


def _draw_anno(img: np.ndarray, dets: List[Dict]) -> np.ndarray:
    out = img.copy()
    for d in dets:
        x1, y1, x2, y2 = map(int, d["bbox_xyxy"])
        color = (0, 220, 255)
        cv2.rectangle(out, (x1, y1), (x2, y2), color, 2)
        label = f'id{d["track_id"]} {d["class_name"]} {d["confidence"]:.2f}'
        (tw, th), _ = cv2.getTextSize(label, cv2.FONT_HERSHEY_SIMPLEX, 0.4, 1)
        cv2.rectangle(out, (x1, y1 - th - 4), (x1 + tw, y1), color, -1)
        cv2.putText(out, label, (x1, y1 - 2),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.4, (0, 0, 0), 1)
    return out


def _to_meta(cam_id: str, w: int, h: int, dets: List[Dict], inf_ms: float, targets: List[str]) -> Dict:
    people = [d for d in dets if d.get("class_name") == "person"]
    vehicles = [
        d for d in dets
        if d.get("class_name") in {"car", "truck", "bus", "motorcycle"}
    ]

    people_conf_avg = round(
        sum(d["confidence"] for d in people) / len(people), 3) if people else 0.0
    vehicles_conf_avg = round(
        sum(d["confidence"] for d in vehicles) / len(vehicles), 3) if vehicles else 0.0

    return {
        "timestamp_utc": datetime.utcnow().isoformat(timespec="milliseconds") + "Z",
        "camera_id": cam_id,
        "image": {"width": int(w), "height": int(h)},
        "compute": {"inference_ms": float(inf_ms), "model": MODEL_NAME},
        "targets": targets,
        "detections": dets,  # full list – all classes
        "people": {
            "count": len(people),
            "confidence_avg": people_conf_avg,
        },
        "vehicles": {
            "count": len(vehicles),
            "confidence_avg": vehicles_conf_avg,
        },
    }


def _extract_dets(result, names: Dict[int, str], allowed_ids: Optional[set]) -> List[Dict]:
    dets: List[Dict] = []
    boxes = getattr(result, "boxes", None)
    if boxes is None:
        return dets

    xyxy = boxes.xyxy.cpu().numpy() if hasattr(boxes, "xyxy") else None
    conf = boxes.conf.cpu().numpy() if hasattr(boxes, "conf") else None
    cls = boxes.cls.cpu().numpy() if hasattr(boxes, "cls") else None

    ids = None
    if hasattr(boxes, "id") and boxes.id is not None:
        try:
            ids = boxes.id.cpu().numpy()
        except Exception:
            ids = None

    if xyxy is None or conf is None or cls is None:
        return dets

    for i in range(len(xyxy)):
        class_id = int(cls[i])
        if allowed_ids is not None and class_id not in allowed_ids:
            continue
        class_name = names.get(class_id, str(class_id))
        dets.append({
            "class_id": class_id,
            "class_name": class_name,
            "confidence": float(conf[i]),
            "bbox_xyxy": [float(x) for x in xyxy[i].tolist()],
            "track_id": int(ids[i]) if ids is not None and i < len(ids) and ids[i] is not None else None
        })
    return dets

# ------------------ main entry ------------------


def detect_one(camera: Dict) -> Tuple[int, Optional[str], Optional[str], Dict]:
    """
    Main entry point for one capture + (optional) detection pass.

    Returns:
        count:              number of detections after filtering to targets
        raw_path:           path to saved raw frame (always saved if grab worked)
        annotated_path:     path to annotated frame (only if there are detections)
        meta:               JSON-serializable metadata dict
    """
    t0 = time.time()
    cam_key = camera.get("key") or camera.get("id") or "unknown"
    cam_id = camera.get("id") or cam_key

    # Folder per day
    day = datetime.utcnow().strftime("%Y-%m-%d")
    day_dir = os.path.join(FRAME_ROOT, day)
    os.makedirs(day_dir, exist_ok=True)

    # --- Always grab + save RAW frame (pipeline must keep working) ---
    raw = _grab_raw_frame(camera)
    if raw is None or getattr(raw, "size", 0) == 0:
        # Could not grab a usable frame; return empty meta
        meta = _to_meta(cam_id, FRAME_WIDTH, FRAME_HEIGHT, [], 0.0, [])
        return 0, None, None, meta

    h, w = raw.shape[:2]
    raw_path = _save_jpg(day_dir, cam_id, "raw", raw)

    # If detection globally disabled, just return meta with no detections
    if not DETECTION_ENABLED:
        meta = _to_meta(cam_id, w, h, [], 0.0, [])
        # You can uncomment this if you want to see it in the DB:
        # meta["compute"] = {"detection_enabled": False}
        return 0, raw_path, None, meta

    # ---------------- YOLO path guarded by try/except ----------------
    try:
        # Targets from API (names -> IDs)
        targets = _get_targets_for_camera(cam_key)  # e.g., ["person","dog"]

        model = _get_model()
        # YOLO name dict: id -> name
        names = model.model.names if hasattr(model, "model") and hasattr(
            model.model, "names") else model.names

        # 1) If "All" (or "*") is present -> no class filter (detect everything)
        detect_all = any(str(t).lower() in ("all", "*") for t in targets)
        if detect_all:
            classes_param = None  # YOLO will detect every class it knows
        else:
            # 2) Map target names -> class IDs (case-insensitive)
            name_to_id = {str(v).lower(): k for k, v in names.items()}
            wanted_ids = {
                int(name_to_id[str(t).lower()])
                for t in targets
                if str(t).lower() in name_to_id
            }
            # None => all (fallback)
            classes_param = sorted(wanted_ids) if wanted_ids else None

        # Inference & tracking
        t1 = time.time()
        results = model.track(
            source=raw,
            tracker="bytetrack.yaml",
            persist=True,
            classes=classes_param,  # ← filter to targets (or None for all)
            conf=0.20,
            verbose=False,
        )
        inf_ms = (time.time() - t1) * 1000.0

        res = results[0]
        dets = _extract_dets(
            res,
            names,
            set(classes_param) if classes_param is not None else None,
        )

        # Annotated only if there are detections
        annotated_path = None
        if dets:
            ann = _draw_anno(raw, dets)
            annotated_path = _save_jpg(day_dir, cam_id, "annotated", ann)

        meta = _to_meta(
            cam_id,
            w,
            h,
            dets,
            inf_ms if inf_ms > 0 else (time.time() - t0) * 1000.0,
            targets,
        )

        return len(dets), raw_path, annotated_path, meta

    except Exception as e:
        # VERY IMPORTANT: never let a YOLO/model error break the main loop
        print(f"[DETECT] model error for camera={cam_id}: {e}")

        meta = _to_meta(
            cam_id,
            w,
            h,
            [],
            0.0,
            [],
        )
        # Optional: mark detection error
        # meta["compute"] = {"detection_error": str(e)}
        return 0, raw_path, None, meta
