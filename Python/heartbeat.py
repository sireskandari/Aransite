import threading
import time
import socket
from typing import Optional
import socket

import requests

from config import (
    HEARTBEAT_EVERY_SEC,
    HEARTBEAT_DEVICE_ID,
    HEARTBEAT_URL,
    HEARTBEAT_APP_VERSION,
    REQUESTS_VERIFY_TLS,
)
from colorama import Fore, Style


def hb_ok(m): print(Fore.GREEN + m + Style.RESET_ALL)
def hb_warn(m): print(Fore.YELLOW + m + Style.RESET_ALL)
def hb_err(m): print(Fore.RED + m + Style.RESET_ALL)


def get_local_ip() -> str | None:
    """
    Best-effort way to get the primary local IP used for outbound traffic.
    """
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        # We don't actually connect to 8.8.8.8, just use it to pick an interface
        s.connect(("8.8.8.8", 80))
        ip = s.getsockname()[0]
        s.close()
        return ip
    except Exception:
        return None


class HeartbeatThread(threading.Thread):
    def __init__(self, stop_event: threading.Event, get_last_capture_utc):
        """
        :param stop_event: threading.Event used to signal stop
        :param get_last_capture_utc: callable -> Optional[datetime] to expose last capture time
        """
        super().__init__(daemon=True)
        self.stop_event = stop_event
        self.get_last_capture_utc = get_last_capture_utc
        self.session = requests.Session()
        self.backoff_sec = HEARTBEAT_EVERY_SEC  # simple backoff if needed

    def run(self):
        hb_ok(
            f"[HB] Heartbeat thread started. Interval={HEARTBEAT_EVERY_SEC}s")

        hostname = socket.gethostname()
        local_ip = get_local_ip()
        first_capture_utc: Optional[str] = None

        while not self.stop_event.is_set():
            start_ts = time.time()
            try:
                last_capture = self.get_last_capture_utc()
                if last_capture and not first_capture_utc:
                    first_capture_utc = last_capture.isoformat()

                payload = {
                    "deviceId": HEARTBEAT_DEVICE_ID,
                    "hostname": hostname,
                    "localIp": local_ip,  # 👈 NEW
                    "captureSinceUtc": first_capture_utc,
                    "lastCaptureUtc": last_capture.isoformat() if last_capture else None,
                    "appVersion": HEARTBEAT_APP_VERSION,
                    "status": "ok",
                }

                resp = self.session.post(
                    HEARTBEAT_URL,
                    json=payload,
                    timeout=15,
                    verify=REQUESTS_VERIFY_TLS,
                )
                if 200 <= resp.status_code < 300:
                    # success
                    self.backoff_sec = HEARTBEAT_EVERY_SEC
                    # keep logs minimal
                    hb_ok(f"[HB] Heartbeat OK ({resp.status_code})")
                else:
                    hb_warn(
                        f"[HB] Heartbeat failed: {resp.status_code} {resp.text[:200]}")
                    # small backoff but don't explode
                    self.backoff_sec = min(self.backoff_sec * 2, 300)

            except Exception as ex:
                hb_warn(f"[HB] Heartbeat error: {ex}")
                self.backoff_sec = min(self.backoff_sec * 2, 300)

            # sleep with stop-event awareness
            delay = self.backoff_sec
            while delay > 0 and not self.stop_event.is_set():
                step = min(1, delay)
                self.stop_event.wait(step)
                delay -= step

        hb_warn("[HB] Heartbeat thread stopped.")
