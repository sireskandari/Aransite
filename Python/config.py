# -----------------------
# CONFIG (edit as needed)
# -----------------------

# Cloud REST endpoint (must return HTTP 200 on success)
API_URL: str = "https://localhost:5292/api/v1/EdgeData"

# --- remote targets (what classes to detect) ---
# Your API should return JSON with either:
#   { "targets": ["person","dog"] }
# or per-camera:
#   { "default": ["person"], "cameras": [{ "id":"CAM1","targets":["person"] }, ...] }
REMOTE_TARGETS_URL: str | None = "https://localhost:5292/api/v1/DetectTargets/all"
REMOTE_TARGETS_TTL_SEC: int = 60  # refresh every 60s
REMOTE_CAMERAS_REQUIRED = True        # fail fast if remote list is unavailable
REQUESTS_VERIFY_TLS = False           # or True if your cert is valid
TEST_FRAME_PATH = None
# set None to disable remote
REMOTE_CAMERAS_URL: str | None = "https://localhost:5292/api/v1/cameras/all"
REMOTE_CAMERAS_TTL_SEC: int = 60   # refresh list every 60s
# if True and API fails at startup -> exit
REMOTE_CAMERAS_REQUIRED: bool = False

# Local SQLite DB filename
DB_NAME: str = "edge_data.db"

# JSON file that lists cameras (unlimited)
CAMERAS_JSON_PATH: str = "cameras.json"

# Detection cadence (seconds): one record per camera every X sec
DETECT_EVERY_SEC: int = 60

# How often to run "sync unsent rows" scheduler (seconds)
# (Backoff inside sync controls real retry timing)
SYNC_EVERY_SEC: int = 5

# Retention cleanup cadence & policy
CLEANUP_EVERY_SEC: int = 3600     # run cleanup hourly
RETENTION_DAYS: int = 30          # delete *synced* rows older than X days
DELETE_OLD_FRAMES: bool = True    # also delete frame image files for those rows

# Max rows to push per sync pass
SYNC_BATCH_SIZE: int = 200

# Exponential backoff for failed syncs
BACKOFF_START: int = 10           # 10 seconds
BACKOFF_MAX: int = 600            # 10 minutes (reset when reached)

# Where to save captured frames (today: fake frame image; later: real snapshot)
# images will be in frames/YYYY-MM-DD/<camera_id>_timestamp.jpg
FRAME_ROOT: str = "frames"
FRAME_WIDTH: int = 1280           # for generated/annotated frames
FRAME_HEIGHT: int = 720

# For now we’re generating fake detections; later you’ll plug in YOLO here.
MODEL_NAME: str = "yolo11m.pt"

# NEW: if set to a local image path, that image will be sent as the frame (no camera needed)
# e.g. TEST_FRAME_PATH = "C:/Users/Ahmad/Pictures/test.jpg"  or  "test.jpg"
# set to None to disable test-image mode
TEST_FRAME_PATH: str | None = "test.jpg"

# NEW: TLS dev only — ignore self-signed cert warnings (keep False for real HTTPS)
REQUESTS_VERIFY_TLS: bool = False

SEND_IMAGES_ONLY_IF_COUNT_POSITIVE: bool = True
DELETE_RAW_AFTER_SUCCESS_SYNC: bool = True

MODEL_NAME: str = "yolo11n.pt"   # or yolo11s.pt / m / l as you like
# if set, we’ll use this image instead of RTSP
TEST_FRAME_PATH: str | None = "test.jpg"
FRAME_ROOT: str = "frames"
FRAME_WIDTH: int = 1280          # only used for synthetic fallback
FRAME_HEIGHT: int = 720
