#!/usr/bin/env python3
"""
Single-image YOLO detector (beginner-friendly)

- Loads YOLO11 (default yolo11n.pt)
- Reads ONE image from disk
- Detects objects
- Filters to classes you care about (e.g., person, cat)
- Optionally POSTs JSON to your API
- Optionally saves an annotated image

Examples:
  # detect only people
  python detect_image.py --image test.jpg --classes person

  # detect person + cat, send to API, and save an annotated image
  python detect_image.py --image test.jpg --classes person,cat \
      --api_url http://localhost:8000/detections --annotate out.jpg
"""
import argparse
import os
import json
from datetime import datetime
from typing import List, Dict, Any, Optional

import cv2
import numpy as np
import requests
from ultralytics import YOLO

# simple aliases so you can type "human"
ALIASES = {
    "human": "person",
    "person": "person",
    "people": "person",
    "cat": "cat",
    "dog": "dog",
    "pet": "dog",  # change if you prefer cat/dog/both
}


def parse_allowed_classes(s: str) -> List[str]:
    raw = [x.strip().lower() for x in s.split(",") if x.strip()]
    mapped = [ALIASES.get(x, x) for x in raw]
    seen = set()
    uniq = []
    for c in mapped:
        if c not in seen:
            seen.add(c)
            uniq.append(c)
    return uniq


def post_json(api_url: Optional[str], payload: Dict[str, Any]) -> Optional[int]:
    if not api_url:
        return None
    try:
        r = requests.post(api_url, json=payload, timeout=30)
        print(f"[API] status={r.status_code} response_len={len(r.text)}")
        return r.status_code
    except Exception as e:
        print(f"[API] error: {e}")
        return -1


def draw_boxes(image, dets, color=(0, 255, 0)):
    for d in dets:
        x1, y1, x2, y2 = map(int, d["bbox_xyxy"])
        label = f'{d["class_name"]} {d["confidence"]:.2f}'

        # main box
        cv2.rectangle(image, (x1, y1), (x2, y2), color, 2)

        # text size
        (tw, th), _ = cv2.getTextSize(label, cv2.FONT_HERSHEY_SIMPLEX, 0.6, 2)

        # keep the label background on-screen
        top = max(0, y1 - th - 6)
        # background rect for label  ← (x1,y1) pair FIXED here
        cv2.rectangle(image, (x1, top), (x1 + tw + 2, y1), color, -1)

        # text
        cv2.putText(image, label, (x1, y1 - 4),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 0), 2)


def main():
    ap = argparse.ArgumentParser(
        description="Single-image YOLO detection with class filter + optional API POST")
    ap.add_argument("--model", default="yolo11n.pt",
                    help="Ultralytics weights (yolo11n.pt, yolo11s.pt, etc.)")
    ap.add_argument("--image", required=True, help="Path to the input image")
    ap.add_argument("--classes", default="person",
                    help="Comma-separated classes to keep (e.g., 'person,cat')")
    ap.add_argument("--conf", type=float, default=0.25,
                    help="Confidence threshold")
    ap.add_argument("--api_url", default=None,
                    help="Optional endpoint to POST detection JSON")
    ap.add_argument("--annotate", default=None,
                    help="Optional path to save annotated image")
    args = ap.parse_args()

    if not os.path.exists(args.image):
        raise SystemExit(f"Image not found: {args.image}")

    allowed = parse_allowed_classes(args.classes)
    print(f"[INFO] Allowed classes: {allowed}")

    img = cv2.imread(args.image)
    if img is None:
        raise SystemExit(f"Failed to read image: {args.image}")
    h, w = img.shape[:2]

    print(f"[INFO] Loading model: {args.model}")
    model = YOLO(args.model)

    # run once on this image
    results = model.predict(img, conf=args.conf, verbose=False)
    names = model.model.names if hasattr(model, "model") else model.names

    dets = []
    if results:
        r = results[0]
        if r.boxes is not None and len(r.boxes) > 0:
            xyxy = r.boxes.xyxy.cpu().numpy()
            confs = r.boxes.conf.cpu().numpy()
            clss = r.boxes.cls.cpu().numpy().astype(int)

            for bb, c, ci in zip(xyxy, confs, clss):
                cname = names[int(ci)] if names and int(
                    ci) in names else str(int(ci))
                if cname not in allowed:
                    continue
                x1, y1, x2, y2 = map(float, bb)
                # also provide relative box (0..1)
                rel = [x1 / w, y1 / h, (x2 - x1) / w, (y2 - y1) / h]
                dets.append({
                    "class_id": int(ci),
                    "class_name": cname,
                    "confidence": float(c),
                    "bbox_xyxy": [x1, y1, x2, y2],
                    "bbox_xywh": [x1, y1, x2 - x1, y2 - y1],
                    "bbox_rel": rel,
                })

    payload = {
        "timestamp_utc": datetime.utcnow().isoformat(timespec="milliseconds") + "Z",
        "image_name": os.path.basename(args.image),
        "image_size": {"width": w, "height": h},
        "classes_requested": allowed,
        "detections": dets,
    }

    # print JSON to console
    print(json.dumps(payload, indent=2))

    # optional: POST
    post_json(args.api_url, payload)

    # optional: save annotated image
    if args.annotate:
        vis = img.copy()
        draw_boxes(vis, dets)
        cv2.imwrite("result.jpg", vis)
        print(f"[INFO] Saved annotated image to {args.annotate}")


if __name__ == "__main__":
    main()
