import os
import sqlite3
from datetime import datetime, timedelta
from typing import List, Tuple, Optional

from config import DB_NAME, RETENTION_DAYS, DELETE_OLD_FRAMES


def init_db() -> None:
    con = sqlite3.connect(DB_NAME)
    cur = con.cursor()
    cur.execute("""
    CREATE TABLE IF NOT EXISTS people_count (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        created_at TEXT NOT NULL,
        camera_id TEXT NOT NULL,
        count INTEGER NOT NULL,
        meta_json TEXT,
        frame_raw_path TEXT,
        frame_annotated_path TEXT,
        synced INTEGER NOT NULL DEFAULT 0
    );
    """)
    cur.execute(
        "CREATE INDEX IF NOT EXISTS idx_pc_synced ON people_count(synced);")
    cur.execute(
        "CREATE INDEX IF NOT EXISTS idx_pc_created ON people_count(created_at);")

    # gentle column adds for older DBs
    for col in ("meta_json", "frame_raw_path", "frame_annotated_path"):
        try:
            cur.execute(f"ALTER TABLE people_count ADD COLUMN {col} TEXT;")
        except Exception:
            pass

    con.commit()
    con.close()


def store_local(
    camera_id: str,
    count: int,
    meta_json: str,
    frame_raw_path: Optional[str],
    frame_annotated_path: Optional[str]
) -> None:
    con = sqlite3.connect(DB_NAME)
    cur = con.cursor()
    cur.execute(
        "INSERT INTO people_count (created_at, camera_id, count, meta_json, frame_raw_path, frame_annotated_path, synced) "
        "VALUES (?, ?, ?, ?, ?, ?, 0)",
        (datetime.utcnow().isoformat(timespec="seconds") + "Z", camera_id,
         count, meta_json, frame_raw_path, frame_annotated_path)
    )
    con.commit()
    con.close()


def get_unsynced_rows(limit: int) -> List[Tuple[int, str, str, int, Optional[str], Optional[str], Optional[str]]]:
    con = sqlite3.connect(DB_NAME)
    cur = con.cursor()
    cur.execute(
        "SELECT id, created_at, camera_id, count, meta_json, frame_raw_path, frame_annotated_path "
        "FROM people_count WHERE synced=0 ORDER BY id ASC LIMIT ?",
        (limit,)
    )
    rows = cur.fetchall()
    con.close()
    return rows


def mark_synced(row_id: int) -> None:
    con = sqlite3.connect(DB_NAME)
    cur = con.cursor()
    cur.execute("UPDATE people_count SET synced=1 WHERE id=?", (row_id,))
    con.commit()
    con.close()


def _safe_del(path: Optional[str]) -> None:
    if not path:
        return
    try:
        if os.path.isfile(path):
            os.remove(path)
    except Exception:
        pass


def cleanup_old_synced(retention_days: int = RETENTION_DAYS) -> int:
    cutoff = (datetime.utcnow() - timedelta(days=retention_days)
              ).isoformat(timespec="seconds") + "Z"
    con = sqlite3.connect(DB_NAME)
    cur = con.cursor()

    if DELETE_OLD_FRAMES:
        cur.execute(
            "SELECT frame_raw_path, frame_annotated_path FROM people_count WHERE synced=1 AND created_at < ?", (cutoff,))
        for raw, ann in cur.fetchall():
            _safe_del(raw)
            _safe_del(ann)

    cur.execute(
        "DELETE FROM people_count WHERE synced=1 AND created_at < ?", (cutoff,))
    deleted = cur.rowcount
    con.commit()
    con.close()
    return deleted
