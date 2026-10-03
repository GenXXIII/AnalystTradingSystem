"""Read-only JSON bridge between XAUUSD-AI and MetaTrader 5 Desktop."""

from __future__ import annotations

import json
import os
import platform
import sys
from datetime import datetime, timezone


def emit(payload: dict, exit_code: int = 0) -> None:
    sys.stdout.write(json.dumps(payload, separators=(",", ":")))
    sys.stdout.flush()
    raise SystemExit(exit_code)


def fail(code: str, message: str, exit_code: int = 1) -> None:
    emit({"success": False, "errorCode": code, "message": message}, exit_code)


def last_error(mt5) -> tuple[int, str]:
    value = mt5.last_error()
    if isinstance(value, tuple) and len(value) >= 2:
        return int(value[0]), str(value[1])
    return -1, "Unknown MetaTrader 5 error"


def mapped_error(mt5, fallback_code: str, fallback_message: str) -> None:
    code, _ = last_error(mt5)
    if code == -6:
        fail("MT5_AUTHENTICATION_FAILED", "MT5 authentication failed.")
    if code == -10005:
        fail("MT5_TIMEOUT", "The MT5 terminal request timed out.")
    if code == -10003:
        fail("MT5_INITIALIZATION_FAILED", "The MT5 terminal could not be initialized.")
    fail(fallback_code, fallback_message)


def as_decimal_text(value) -> str:
    return repr(float(value))


try:
    request = json.load(sys.stdin)
except (json.JSONDecodeError, UnicodeError):
    fail("MT5_CONFIGURATION_INVALID", "The MT5 bridge request was invalid.")

if platform.system() != "Windows":
    fail("MT5_CONFIGURATION_INVALID", "MT5 Desktop integration requires Windows.")

terminal_path = os.path.abspath(os.path.expandvars(str(request.get("terminalPath", ""))))
if not os.path.isfile(terminal_path):
    fail("MT5_TERMINAL_NOT_FOUND", "The configured MT5 terminal was not found.")

try:
    import MetaTrader5 as mt5
except ImportError:
    fail(
        "MT5_CONFIGURATION_INVALID",
        "The MetaTrader5 Python package is not installed for the configured Python executable.",
    )

try:
    login = int(request.get("login"))
except (TypeError, ValueError):
    fail("MT5_CONFIGURATION_INVALID", "The configured MT5 login is invalid.")

initialized = False
try:
    initialized = bool(
        mt5.initialize(
            terminal_path,
            login=login,
            password=str(request.get("password", "")),
            server=str(request.get("server", "")),
            timeout=int(request.get("connectionTimeoutMilliseconds", 30000)),
            portable=False,
        )
    )
    if not initialized:
        mapped_error(mt5, "MT5_CONNECTION_FAILED", "The MT5 terminal connection failed.")

    operation = str(request.get("operation", "")).lower()
    terminal = mt5.terminal_info()
    account = mt5.account_info()

    if operation == "status":
        emit(
            {
                "success": True,
                "status": {
                    "connected": bool(terminal is not None and terminal.connected and account is not None),
                    "login": int(account.login) if account is not None else None,
                    "server": str(account.server) if account is not None else None,
                    "terminalName": str(terminal.name) if terminal is not None else None,
                    "terminalBuild": int(terminal.build) if terminal is not None else None,
                },
            }
        )

    symbol = str(request.get("symbol", "")).strip()
    if not symbol or not mt5.symbol_select(symbol, True):
        fail("MT5_SYMBOL_NOT_FOUND", "The configured MT5 symbol was not found.")

    if operation == "quote":
        tick = mt5.symbol_info_tick(symbol)
        if tick is None:
            mapped_error(mt5, "MT5_DATA_REQUEST_FAILED", "The MT5 quote request failed.")
        timestamp_ms = int(getattr(tick, "time_msc", int(tick.time) * 1000))
        emit(
            {
                "success": True,
                "quote": {
                    "symbol": symbol,
                    "bid": as_decimal_text(tick.bid),
                    "ask": as_decimal_text(tick.ask),
                    "timestampMilliseconds": timestamp_ms,
                },
            }
        )

    if operation != "candles":
        fail("MT5_CONFIGURATION_INVALID", "The MT5 bridge operation was not supported.")

    timeframe_code = str(request.get("timeframe", "")).upper()
    timeframe_map = {
        "M1": (mt5.TIMEFRAME_M1, 60),
        "M5": (mt5.TIMEFRAME_M5, 300),
        "M15": (mt5.TIMEFRAME_M15, 900),
        "M30": (mt5.TIMEFRAME_M30, 1800),
        "H1": (mt5.TIMEFRAME_H1, 3600),
        "H4": (mt5.TIMEFRAME_H4, 14400),
        "D1": (mt5.TIMEFRAME_D1, 86400),
    }
    timeframe_value = timeframe_map.get(timeframe_code)
    if timeframe_value is None:
        fail("MARKET_DATA_REQUEST_INVALID", "The requested timeframe is not supported.")

    from_seconds = int(request.get("fromUnixSeconds"))
    to_seconds = int(request.get("toUnixSeconds"))
    max_bars = int(request.get("maxBars", 10000))
    if from_seconds >= to_seconds or max_bars < 1:
        fail("MARKET_DATA_REQUEST_INVALID", "The candle request range was invalid.")

    rates = mt5.copy_rates_range(
        symbol,
        timeframe_value[0],
        datetime.fromtimestamp(from_seconds, timezone.utc),
        datetime.fromtimestamp(to_seconds, timezone.utc),
    )
    if rates is None:
        mapped_error(mt5, "MT5_DATA_REQUEST_FAILED", "The MT5 candle request failed.")
    if len(rates) > max_bars:
        fail("MARKET_DATA_REQUEST_INVALID", "The candle request exceeded the configured limit.")

    now_seconds = int(datetime.now(timezone.utc).timestamp())
    candles = []
    for rate in rates:
        open_seconds = int(rate["time"])
        candles.append(
            {
                "symbol": symbol,
                "timeframe": timeframe_code,
                "openTimeSeconds": open_seconds,
                "open": as_decimal_text(rate["open"]),
                "high": as_decimal_text(rate["high"]),
                "low": as_decimal_text(rate["low"]),
                "close": as_decimal_text(rate["close"]),
                "tickVolume": str(int(rate["tick_volume"])),
                "realVolume": str(int(rate["real_volume"])),
                "spread": str(int(rate["spread"])),
                "isComplete": open_seconds + timeframe_value[1] <= now_seconds,
            }
        )

    emit({"success": True, "candles": candles})
finally:
    if initialized:
        mt5.shutdown()
