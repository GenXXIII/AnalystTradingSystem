"use client";

import { useEffect, useMemo, useRef, useState, type PointerEvent as ReactPointerEvent } from "react";
import {
  CandlestickSeries,
  ColorType,
  CrosshairMode,
  createChart,
  type CandlestickData,
  type IChartApi,
  type ISeriesApi,
  type LineData,
  LineSeries,
  LineStyle,
  LineType,
  type Time,
  type UTCTimestamp,
} from "lightweight-charts";
import type { MarketQuote, StoredMarketCandle } from "@/types/market";
import { formatSessionCountdown, type XauUsdMarketSession } from "@/lib/market/xauusd-session";

const price = new Intl.NumberFormat("en-US", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});
const utcTime = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
  timeZone: "UTC",
});
const MILLISECONDS_PER_DAY = 86_400_000;
const WEEK_WINDOW_DAYS = 7;
const RIGHT_PADDING_BARS = 6;
const EMA_PERIOD = 20;
const PRICE_FLOOR_PERIOD = 12;
const DEFAULT_PRICE_MARGIN = 0.16;
const MIN_PRICE_MARGIN = 0.05;
const MAX_PRICE_MARGIN = 0.25;
const CLOSED_SESSION_GAP_SECONDS = 6 * 60 * 60;
const MAX_CLOSED_SESSION_DISPLAY_BARS = 5_000;
const CLOSED_SESSION_UP_COLOR = "#00d897";
const CLOSED_SESSION_DOWN_COLOR = "#ff465d";
const INTRADAY_WINDOW_HOURS = 60;
const DAILY_WINDOW_HOURS = 7 * 24;
const TIMEFRAME_SECONDS: Record<string, number> = {
  M1: 60,
  M5: 5 * 60,
  M15: 15 * 60,
  M30: 30 * 60,
  H1: 60 * 60,
  H4: 4 * 60 * 60,
  D1: 24 * 60 * 60,
};

type ChartReadout = {
  open: number;
  high: number;
  low: number;
  close: number;
  time: Time;
  closedSession?: boolean;
};

type TradingChartProps = Readonly<{
  candles: StoredMarketCandle[];
  quote: MarketQuote | null;
  providerConnected: boolean;
  timeframe: string;
  nowUtcMilliseconds: number | null;
  marketSession: XauUsdMarketSession | null;
}>;

export function TradingChart({ candles, quote, providerConnected, timeframe, nowUtcMilliseconds, marketSession }: TradingChartProps) {
  const shellRef = useRef<HTMLDivElement | null>(null);
  const canvasRef = useRef<HTMLDivElement | null>(null);
  const chartRef = useRef<IChartApi | null>(null);
  const seriesRef = useRef<ISeriesApi<"Candlestick"> | null>(null);
  const emaSeriesRef = useRef<ISeriesApi<"Line"> | null>(null);
  const priceFloorSeriesRef = useRef<ISeriesApi<"Line"> | null>(null);
  const activeTimeframeRef = useRef<string | null>(null);
  const fittedPointCountRef = useRef(0);
  const priceMarginRef = useRef(DEFAULT_PRICE_MARGIN);
  const priceScaleDragRef = useRef<{ startY: number; startMargin: number } | null>(null);
  const pointerInspectingRef = useRef(false);
  const [readout, setReadout] = useState<ChartReadout | null>(null);
  const [isFullscreen, setIsFullscreen] = useState(false);

  const points = useMemo(
    () => normalizeCandles(candles.filter((candle) => candle.timeframe === timeframe)),
    [candles, timeframe],
  );
  const displayPoints = useMemo(
    () => addClosedSessionDisplayBars(points, timeframe),
    [points, timeframe],
  );
  const hasClosedSessionBars = displayPoints.length > points.length;
  const latest = points.at(-1) ?? null;
  const utcDayStartMilliseconds = nowUtcMilliseconds === null
    ? null
    : Math.floor(nowUtcMilliseconds / MILLISECONDS_PER_DAY) * MILLISECONDS_PER_DAY;
  const quoteAgeSeconds = quote && nowUtcMilliseconds !== null
    ? Math.max(0, (nowUtcMilliseconds - Date.parse(quote.timestampUtc)) / 1_000)
    : null;
  const isLive = marketSession?.isOpen === true && providerConnected && quoteAgeSeconds !== null && quoteAgeSeconds < 90;
  const currentTime = nowUtcMilliseconds === null ? null : new Date(nowUtcMilliseconds);

  useEffect(() => {
    const container = canvasRef.current;
    if (!container) return;

    const chart = createChart(container, {
      width: container.clientWidth,
      height: container.clientHeight,
      layout: {
        attributionLogo: true,
        background: { type: ColorType.Solid, color: "#05090b" },
        textColor: "#73838b",
        fontFamily: "Inter, ui-sans-serif, system-ui, sans-serif",
        fontSize: 11,
      },
      grid: {
        vertLines: { color: "#172126" },
        horzLines: { color: "#172126" },
      },
      crosshair: {
        mode: CrosshairMode.Normal,
        vertLine: { color: "#64737a", width: 1, style: 3, labelBackgroundColor: "#25333a" },
        horzLine: { color: "#64737a", width: 1, style: 3, labelBackgroundColor: "#25333a" },
      },
      rightPriceScale: {
        visible: true,
        autoScale: true,
        borderColor: "#26343a",
        scaleMargins: { top: DEFAULT_PRICE_MARGIN, bottom: DEFAULT_PRICE_MARGIN },
      },
      leftPriceScale: { visible: false },
      timeScale: {
        borderColor: "#26343a",
        timeVisible: true,
        secondsVisible: false,
        rightOffset: RIGHT_PADDING_BARS,
        barSpacing: 8,
        minBarSpacing: 0.1,
        enableConflation: true,
        fixLeftEdge: true,
        fixRightEdge: false,
      },
      localization: {
        locale: "en-US",
        priceFormatter: (value: number) => price.format(value),
        timeFormatter: (time: Time) => formatChartTime(time),
      },
      handleScroll: {
        mouseWheel: true,
        pressedMouseMove: true,
        horzTouchDrag: true,
        vertTouchDrag: false,
      },
      handleScale: {
        axisPressedMouseMove: { time: true, price: false },
        axisDoubleClickReset: { time: true, price: true },
        mouseWheel: true,
        pinch: true,
      },
    });
    const emaSeries = chart.addSeries(LineSeries, {
      color: "#d7b52a",
      lineWidth: 2,
      lineStyle: LineStyle.Solid,
      lineType: LineType.Simple,
      priceLineVisible: false,
      lastValueVisible: false,
      crosshairMarkerVisible: false,
    });
    const priceFloorSeries = chart.addSeries(LineSeries, {
      color: "#008f63",
      lineWidth: 1,
      lineStyle: LineStyle.Solid,
      lineType: LineType.WithSteps,
      priceLineVisible: false,
      lastValueVisible: false,
      crosshairMarkerVisible: false,
    });
    const series = chart.addSeries(CandlestickSeries, {
      upColor: "#00d897",
      downColor: "#ff465d",
      borderUpColor: "#00d897",
      borderDownColor: "#ff465d",
      wickUpColor: "#00d897",
      wickDownColor: "#ff465d",
      priceLineColor: "#00d897",
      priceLineStyle: 2,
      priceLineWidth: 1,
      priceLineVisible: true,
      lastValueVisible: true,
    });

    const handleCrosshair = (parameter: { time?: Time; seriesData: Map<unknown, unknown> }) => {
      const data = parameter.seriesData.get(series) as CandlestickData<Time> | undefined;
      if (!parameter.time || !data) {
        pointerInspectingRef.current = false;
        return;
      }
      pointerInspectingRef.current = true;
      setReadout({
        open: data.open,
        high: data.high,
        low: data.low,
        close: data.close,
        time: data.time,
        closedSession: data.customValues?.closedSession === true,
      });
    };
    chart.subscribeCrosshairMove(handleCrosshair);

    const resize = new ResizeObserver(([entry]) => {
      const { width, height } = entry.contentRect;
      if (width > 0 && height > 0) chart.resize(Math.floor(width), Math.floor(height));
    });
    resize.observe(container);

    chartRef.current = chart;
    seriesRef.current = series;
    emaSeriesRef.current = emaSeries;
    priceFloorSeriesRef.current = priceFloorSeries;
    return () => {
      resize.disconnect();
      chart.unsubscribeCrosshairMove(handleCrosshair);
      chart.remove();
      chartRef.current = null;
      seriesRef.current = null;
      emaSeriesRef.current = null;
      priceFloorSeriesRef.current = null;
    };
  }, []);

  useEffect(() => {
    const series = seriesRef.current;
    const chart = chartRef.current;
    if (!series || !chart) return;

    series.setData(displayPoints);
    emaSeriesRef.current?.setData(calculateEma(points, EMA_PERIOD));
    priceFloorSeriesRef.current?.setData(calculateRollingLow(points, PRICE_FLOOR_PERIOD));
    if (!pointerInspectingRef.current) setReadout(latest);
    const weeklyWindowKey = utcDayStartMilliseconds === null ? null : `${timeframe}:${utcDayStartMilliseconds}`;
    const historyExpanded = points.length - fittedPointCountRef.current > 1;
    if (points.length > 0
        && weeklyWindowKey !== null
        && (activeTimeframeRef.current !== weeklyWindowKey || historyExpanded)) {
      activeTimeframeRef.current = weeklyWindowKey;
      priceMarginRef.current = DEFAULT_PRICE_MARGIN;
      applyPriceScaleMargin(chart, DEFAULT_PRICE_MARGIN);
      fitRecentCandles(chart, points, timeframe);
    }
    fittedPointCountRef.current = points.length;
  }, [displayPoints, latest, points, timeframe, utcDayStartMilliseconds]);

  useEffect(() => {
    seriesRef.current?.applyOptions({
      priceLineColor: marketSession?.isOpen === false ? "#718087" : "#00d897",
    });
  }, [marketSession?.isOpen]);

  useEffect(() => {
    const onFullscreenChange = () => setIsFullscreen(document.fullscreenElement === shellRef.current);
    document.addEventListener("fullscreenchange", onFullscreenChange);
    return () => document.removeEventListener("fullscreenchange", onFullscreenChange);
  }, []);

  const beginPriceScale = (event: ReactPointerEvent<HTMLButtonElement>) => {
    event.preventDefault();
    event.currentTarget.setPointerCapture(event.pointerId);
    priceScaleDragRef.current = { startY: event.clientY, startMargin: priceMarginRef.current };
  };
  const movePriceScale = (event: ReactPointerEvent<HTMLButtonElement>) => {
    const drag = priceScaleDragRef.current;
    if (!drag) return;
    const chartHeight = Math.max(240, shellRef.current?.clientHeight ?? 0);
    const margin = clampPriceMargin(drag.startMargin - ((event.clientY - drag.startY) / chartHeight) * 0.5);
    priceMarginRef.current = margin;
    applyPriceScaleMargin(chartRef.current, margin);
  };
  const endPriceScale = (event: ReactPointerEvent<HTMLButtonElement>) => {
    if (event.currentTarget.hasPointerCapture(event.pointerId)) event.currentTarget.releasePointerCapture(event.pointerId);
    priceScaleDragRef.current = null;
  };
  const resetPriceScale = () => {
    priceMarginRef.current = DEFAULT_PRICE_MARGIN;
    applyPriceScaleMargin(chartRef.current, DEFAULT_PRICE_MARGIN);
  };

  const displayed: ChartReadout | null = readout ?? (latest
    ? { ...latest, closedSession: false }
    : null);
  return (
    <div className="chart-wrap" data-market-state={marketSession?.isOpen === false ? "closed" : "open"} ref={shellRef}>
      <div className="chart-canvas" ref={canvasRef} aria-label={`${points.length} ${timeframe} XAUUSD candles`} />
      {points.length === 0 ? (
        <div className="chart-empty" role="status">
          <strong>No chart evidence yet</strong>
          <span>Waiting for AllTick and stored market candles.</span>
        </div>
      ) : null}
      <div className="chart-watermark" aria-hidden="true">XAUUSD <span>{timeframe}</span></div>
      {displayed ? (
        <div className="chart-ohlc" aria-live="polite">
          <time>{formatChartTime(displayed.time)} UTC</time>
          {displayed.closedSession ? (
            <span>Market closed · display carry only</span>
          ) : (
            <>
              <span>O <b>{price.format(displayed.open)}</b></span>
              <span>H <b>{price.format(displayed.high)}</b></span>
              <span>L <b>{price.format(displayed.low)}</b></span>
              <span>C <b className={displayed.close >= displayed.open ? "positive-text" : "negative-text"}>{price.format(displayed.close)}</b></span>
            </>
          )}
        </div>
      ) : null}
      {points.length > 0 ? (
        <div className="chart-overlay-legend" aria-label="Chart overlays">
          <span data-overlay="ema">EMA {EMA_PERIOD}</span>
          <span data-overlay="floor">{PRICE_FLOOR_PERIOD}-bar floor</span>
          {hasClosedSessionBars ? <span data-overlay="closed">Closed-session carry</span> : null}
        </div>
      ) : null}
      <div className={`market-session ${isLive ? "live" : "paused"}`}>
        <i aria-hidden="true" />
        <strong>{isLive ? "XAUUSD live market" : marketSession?.isOpen === false ? "XAUUSD market closed" : providerConnected ? "XAUUSD market open · awaiting tick" : "AllTick waiting for data"}</strong>
        {marketSession?.isOpen === false && latest ? <span>Last close {price.format(latest.close)} · {formatChartTime(latest.time)} UTC</span> : null}
        {marketSession && currentTime ? <span>{marketSession.nextTransitionLabel} / {marketSession.nextTransitionLocalLabel} · {formatSessionCountdown(currentTime, marketSession.nextTransitionAtUtc)}</span> : quote ? <span>Last tick {utcTime.format(new Date(quote.timestampUtc))} UTC</span> : null}
      </div>
      <div className="chart-controls" aria-label="Chart controls">
        <button type="button" onClick={() => zoomChart(chartRef.current, 0.78, points.length)} aria-label="Zoom in">+</button>
        <button type="button" onClick={() => zoomChart(chartRef.current, 1.28, points.length)} aria-label="Zoom out">−</button>
        <button type="button" onClick={() => fitRecentCandles(chartRef.current, points, timeframe)}>Recent</button>
        <button type="button" onClick={() => fitWeeklyCandles(chartRef.current, points, utcDayStartMilliseconds)}>Fit 7D</button>
        <button type="button" onClick={() => chartRef.current?.timeScale().scrollToRealTime()} aria-label={marketSession?.isOpen === false ? "Jump to last closed candle" : "Jump to live candle"}>{marketSession?.isOpen === false ? "Last" : "Live"}</button>
        <button type="button" onClick={() => void toggleFullscreen(shellRef.current)} aria-label={isFullscreen ? "Exit fullscreen" : "Open fullscreen"}>{isFullscreen ? "Exit" : "⛶"}</button>
      </div>
      <button
        type="button"
        className="price-axis-scaler"
        aria-label="Scale visible candles from the right price axis"
        title="Drag to scale visible candles. Double-click to reset."
        onPointerDown={beginPriceScale}
        onPointerMove={movePriceScale}
        onPointerUp={endPriceScale}
        onPointerCancel={endPriceScale}
        onDoubleClick={resetPriceScale}
      />
    </div>
  );
}

function normalizeCandles(candles: StoredMarketCandle[]): CandlestickData<UTCTimestamp>[] {
  const byTime = new Map<number, CandlestickData<UTCTimestamp>>();
  for (const candle of candles) {
    const timestamp = Math.floor(Date.parse(candle.openTimeUtc) / 1_000);
    if (!Number.isFinite(timestamp)) continue;
    byTime.set(timestamp, {
      time: timestamp as UTCTimestamp,
      open: candle.open,
      high: candle.high,
      low: candle.low,
      close: candle.close,
    });
  }
  return [...byTime.values()].sort((left, right) => Number(left.time) - Number(right.time));
}

function addClosedSessionDisplayBars(
  points: CandlestickData<UTCTimestamp>[],
  timeframe: string,
): CandlestickData<UTCTimestamp>[] {
  const intervalSeconds = TIMEFRAME_SECONDS[timeframe];
  if (!intervalSeconds || timeframe === "D1" || points.length < 2) return points;

  const displayPoints: CandlestickData<UTCTimestamp>[] = [];
  let displayBarCount = 0;
  for (let index = 0; index < points.length; index += 1) {
    const point = points[index];
    displayPoints.push(point);
    const next = points[index + 1];
    if (!next
        || Number(next.time) - Number(point.time) < CLOSED_SESSION_GAP_SECONDS
        || displayBarCount >= MAX_CLOSED_SESSION_DISPLAY_BARS) continue;

    for (let timestamp = Number(point.time) + intervalSeconds;
         timestamp < Number(next.time) && displayBarCount < MAX_CLOSED_SESSION_DISPLAY_BARS;
         timestamp += intervalSeconds) {
      const displayColor = displayBarCount % 2 === 0
        ? CLOSED_SESSION_UP_COLOR
        : CLOSED_SESSION_DOWN_COLOR;
      displayPoints.push({
        time: timestamp as UTCTimestamp,
        open: point.close,
        high: point.close,
        low: point.close,
        close: point.close,
        color: displayColor,
        borderColor: displayColor,
        wickColor: displayColor,
        customValues: { closedSession: true },
      });
      displayBarCount += 1;
    }
  }
  return displayPoints;
}

function zoomChart(chart: IChartApi | null, factor: number, pointCount: number) {
  if (!chart || pointCount === 0) return;
  const range = chart.timeScale().getVisibleLogicalRange();
  if (!range) return;
  const from = Math.max(0, range.from);
  const span = Math.max(4, (range.to - range.from) * factor);
  chart.timeScale().setVisibleLogicalRange({ from, to: from + span });
}

function fitRecentCandles(
  chart: IChartApi | null,
  points: CandlestickData<UTCTimestamp>[],
  timeframe: string,
) {
  if (!chart || points.length === 0) return;
  const lastIndex = points.length - 1;
  const intervalSeconds = TIMEFRAME_SECONDS[timeframe] ?? TIMEFRAME_SECONDS.M15;
  const windowHours = timeframe === "D1" ? DAILY_WINDOW_HOURS : INTRADAY_WINDOW_HOURS;
  const rawWindowStart = timeframe === "D1"
    ? Number(points[lastIndex].time) - (windowHours * 60 * 60)
    : getIntradayWindowStart(Number(points[lastIndex].time));
  const alignedWindowStart = Math.floor(rawWindowStart / intervalSeconds) * intervalSeconds;
  chart.timeScale().setVisibleRange({
    from: alignedWindowStart as UTCTimestamp,
    to: points[lastIndex].time,
  });
  chart.timeScale().applyOptions({ rightOffset: RIGHT_PADDING_BARS });
}

function getIntradayWindowStart(latestTimestamp: number) {
  const latest = new Date(latestTimestamp * 1_000);
  const day = latest.getUTCDay();
  if (day === 0 || day === 1) {
    const daysSinceSaturday = day === 0 ? 1 : 2;
    return Date.UTC(
      latest.getUTCFullYear(),
      latest.getUTCMonth(),
      latest.getUTCDate() - daysSinceSaturday,
    ) / 1_000;
  }
  return latestTimestamp - (INTRADAY_WINDOW_HOURS * 60 * 60);
}

function fitWeeklyCandles(
  chart: IChartApi | null,
  points: CandlestickData<UTCTimestamp>[],
  utcDayStartMilliseconds: number | null,
) {
  if (!chart || points.length === 0 || utcDayStartMilliseconds === null) return;
  const windowStartSeconds = (utcDayStartMilliseconds - ((WEEK_WINDOW_DAYS - 1) * MILLISECONDS_PER_DAY)) / 1_000;
  const firstWeeklyIndex = points.findIndex((point) => Number(point.time) >= windowStartSeconds);
  const firstIndex = firstWeeklyIndex < 0 ? points.length - 1 : firstWeeklyIndex;
  chart.timeScale().setVisibleRange({
    from: points[firstIndex].time,
    to: points[points.length - 1].time,
  });
  chart.timeScale().applyOptions({ rightOffset: RIGHT_PADDING_BARS });
}

function clampPriceMargin(value: number) {
  return Math.min(MAX_PRICE_MARGIN, Math.max(MIN_PRICE_MARGIN, value));
}

function calculateEma(
  points: CandlestickData<UTCTimestamp>[],
  period: number,
): LineData<UTCTimestamp>[] {
  if (points.length < period) return [];
  const seed = points.slice(0, period).reduce((total, point) => total + point.close, 0) / period;
  const multiplier = 2 / (period + 1);
  const values: LineData<UTCTimestamp>[] = [{ time: points[period - 1].time, value: seed }];
  let previous = seed;
  for (let index = period; index < points.length; index += 1) {
    previous = ((points[index].close - previous) * multiplier) + previous;
    values.push({ time: points[index].time, value: previous });
  }
  return values;
}

function calculateRollingLow(
  points: CandlestickData<UTCTimestamp>[],
  period: number,
): LineData<UTCTimestamp>[] {
  if (points.length < period) return [];
  const values: LineData<UTCTimestamp>[] = [];
  for (let index = period - 1; index < points.length; index += 1) {
    let low = points[index].low;
    for (let offset = 1; offset < period; offset += 1) {
      low = Math.min(low, points[index - offset].low);
    }
    values.push({ time: points[index].time, value: low });
  }
  return values;
}

function applyPriceScaleMargin(chart: IChartApi | null, margin: number) {
  if (!chart) return;
  chart.priceScale("right").applyOptions({
    autoScale: true,
    scaleMargins: { top: margin, bottom: margin },
  });
}

async function toggleFullscreen(element: HTMLDivElement | null) {
  if (!element) return;
  if (document.fullscreenElement === element) {
    await document.exitFullscreen();
  } else {
    await element.requestFullscreen();
  }
}

function formatChartTime(time: Time): string {
  if (typeof time === "number") return utcTime.format(new Date(time * 1_000));
  if (typeof time === "string") return time;
  return `${String(time.day).padStart(2, "0")}/${String(time.month).padStart(2, "0")}/${time.year}`;
}
