# main.py
import os
import sys
import json
import http.cookiejar as cookielib
from time import sleep, time
from datetime import datetime
from os.path import exists, dirname, abspath

import requests
from pypresence import Presence


# ----------------------------
# Logging
# ----------------------------
def log(msg):
    print(f"[{datetime.now().strftime('%H:%M:%S')}] {msg}", flush=True)


def error(msg):
    print(f"[{datetime.now().strftime('%H:%M:%S')}] ERROR: {msg}", flush=True)


# ----------------------------
# Config
# ----------------------------
if getattr(sys, 'frozen', False):
    BASE_PATH = dirname(sys.executable)
else:
    BASE_PATH = dirname(abspath(__file__))

CONFIG_PATH = os.path.join(BASE_PATH, "config.json")
COOKIES_TXT_PATH = os.path.join(BASE_PATH, "cookies.txt")

DEFAULT_DISCORD_APP_ID = "869994714093465680"  # fallback


def get_config():
    if not exists(CONFIG_PATH):
        error(f"Config not found: {CONFIG_PATH}")
        sys.exit(1)
    with open(CONFIG_PATH, "r", encoding="utf-8") as f:
        return json.load(f)


def _load_cookies_txt():
    """Load existing cookies.txt file (Netscape format)."""
    if not exists(COOKIES_TXT_PATH):
        return None
    try:
        cj = cookielib.MozillaCookieJar(COOKIES_TXT_PATH)
        cj.load(ignore_discard=True, ignore_expires=True)
        return cj
    except Exception as e:
        log(f"cookies.txt load failed: {e}")
        return None


# ----------------------------
# Discord IPC connect
# ----------------------------
def connect_discord(app_id: str, tries: int = 30, delay: float = 1.0):
    last_err = None
    for attempt in range(1, tries + 1):
        for pipe in range(0, 10):
            try:
                rpc = Presence(app_id, pipe=pipe)
                rpc.connect()
                log(f"Discord IPC connected (pipe={pipe}, app_id={app_id})")
                return rpc
            except Exception as e:
                last_err = e
        log(f"Discord not ready, retrying... ({attempt}/{tries}) err={last_err}")
        sleep(delay)
    raise last_err if last_err else Exception("Could not connect to Discord RPC")


# ----------------------------
# Steam helpers
# ----------------------------
def parse_user_ids(raw):
    if isinstance(raw, list):
        return [str(x).strip() for x in raw if str(x).strip()]
    if isinstance(raw, str):
        return [x.strip() for x in raw.split(",") if x.strip()]
    return []


def steam_get_current_game(steam_api_key: str, steamid64: str) -> str:
    url = (
        "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/"
        f"?key={steam_api_key}&format=json&steamids={steamid64}"
    )
    try:
        r = requests.get(url, timeout=10)
        if r.status_code != 200:
            return ""
        players = r.json().get("response", {}).get("players", [])
        if not players:
            return ""
        return players[0].get("gameextrainfo", "") or ""
    except Exception:
        return ""


def steam_get_rich_presence_en(steamid64: str) -> str:
    """
    Returns English rich presence string if available.
    Only uses cookies.txt fallback. Automatic extraction eliminated.
    """
    try:
        mini_id3 = int(steamid64) - 76561197960265728
    except Exception:
        return ""

    url = f"https://steamcommunity.com/miniprofile/{mini_id3}/json?l=english&_={int(time()*1000)}"

    headers = {
        "User-Agent": (
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
            "(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
        ),
        "Accept": "application/json,text/plain,*/*",
        "X-Requested-With": "XMLHttpRequest",
        "Referer": "https://steamcommunity.com/",
        "Accept-Language": "en-US,en;q=0.9",
    }

    cookies = _load_cookies_txt()

    try:
        r = requests.get(url, headers=headers, cookies=cookies, timeout=10)
        if r.status_code != 200:
            return ""
        data = r.json()
        in_game = data.get("in_game")
        if isinstance(in_game, dict):
            rp = in_game.get("rich_presence")
            if isinstance(rp, str) and rp.strip():
                return rp.strip()
            else:
                pass
        else:
            pass
        return ""
    except Exception:
        return ""


def discord_detectable_app_id(game_name: str) -> str:
    try:
        r = requests.get(
            "https://discordapp.com/api/v8/applications/detectable", timeout=10
        )
        if r.status_code != 200:
            return DEFAULT_DISCORD_APP_ID
        game_l = game_name.strip().lower()
        for item in r.json():
            if item.get("name", "").strip().lower() == game_l:
                return str(item.get("id"))
        return DEFAULT_DISCORD_APP_ID
    except Exception:
        return DEFAULT_DISCORD_APP_ID


# ----------------------------
# Main
# ----------------------------
def main():
    log("Starting...")

    cfg = get_config()
    steam_api_key = cfg.get("STEAM_API_KEY")
    user_ids_raw = cfg.get("USER_IDS")
    excluded_games_raw = cfg.get("EXCLUDED_GAMES", [])
    
    if not steam_api_key or not user_ids_raw:
        error("config.json missing STEAM_API_KEY or USER_IDS")
        sys.exit(1)

    user_ids = parse_user_ids(user_ids_raw)
    if not user_ids or user_ids[0] == "ENTER_YOURS":
        error("Steam ID is missing or set to ENTER_YOURS. Please configure it in the UI.")
        sys.exit(1)

    excluded_games = [str(x).strip().lower() for x in excluded_games_raw if str(x).strip()]

    log("Manual cookie extraction mode active. Reading from cookies.txt (if present).")

    prev_game = ""
    prev_state = ""
    rpc = None
    start_ts = 0

    while True:
        current_game = ""
        current_user = None

        for sid in user_ids:
            g = steam_get_current_game(steam_api_key, sid)
            if g:
                current_game = g
                current_user = sid
                break

        # EXCLUDE fallback: If the game is in the excluded list, ignore it so native RPC can run!
        if current_game and current_game.lower() in excluded_games:
            log(f"Game '{current_game}' is EXCLUDED in config.json. Steam Presence will ignore this game.")
            current_game = ""

        # If no game is being played -> clear activity, but DO NOT close RPC.
        if current_game == "":
            if rpc is None:
                try:
                    rpc = connect_discord(DEFAULT_DISCORD_APP_ID, tries=3, delay=1.0)
                except Exception as e:
                    log(f"Discord still not ready: {e}")

            if prev_game != "":
                log("No game detected (or game is excluded). Clearing Discord activity...")
                if rpc:
                    try:
                        rpc.clear()
                    except Exception:
                        pass

            prev_game = ""
            prev_state = ""
            start_ts = 0
            sleep(1) # <--- INSTANT UDPATES (1 second)
            continue

        # Try to ensure we have an RPC connection when a game is detected
        if rpc is None:
            try:
                rpc = connect_discord(DEFAULT_DISCORD_APP_ID, tries=3, delay=1.0)
            except Exception as e:
                error(f"Discord connection failed (no rpc yet): {e}")
                sleep(1)
                continue

        # Rich presence (English) if possible
        state = ""
        if current_user:
            state = steam_get_rich_presence_en(current_user)

        # If the game changed, reconnect with best app id (optional)
        if current_game != prev_game:
            log(f"Game detected: {current_game}")
            app_id = discord_detectable_app_id(current_game)
            start_ts = int(time())

            # Reconnect only if app_id changed or we want fresh session
            try:
                if rpc:
                    try:
                        rpc.close()
                    except Exception:
                        pass
                rpc = connect_discord(app_id, tries=5, delay=1.0)
            except Exception as e:
                error(f"Discord connection failed (game app_id): {e}")
                rpc = None
                prev_game = ""
                sleep(1)
                continue

            prev_game = current_game
            prev_state = ""

        # Update activity
        if rpc:
            try:
                # Only update Discord if state changed, prevents spam/rate-limit dropping presence
                if state != prev_state:
                    payload = {
                        "details": None,
                        "state": state or None,
                        "start": start_ts,
                    }
                    rpc.update(**payload)
                    if state and state != prev_state:
                        log(f"State updated: {state}")
                    prev_state = state
            except Exception as e:
                error(f"Discord update failed: {e}")
                try:
                    rpc.close()
                except Exception:
                    pass
                rpc = None
                prev_game = ""

        sleep(3) # <--- INSTANT UDPATES (3 seconds)


if __name__ == "__main__":
    main()
