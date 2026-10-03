"use client";

import { useEffect, useMemo, useRef, useState, type PointerEvent as ReactPointerEvent } from "react";
import {
  CandlestickSeries,
  ColorType,
  CrosshairMode,
  createChart,
  type AutoscaleInfo,
  type CandlestickData,
  type IChartApi,
  type ISeriesApi,
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
const PRICE_RANGE_PADDING = 20;
const DEFAULT_PRICE_MARGIN = 0.16;
const MIN_PRICE_MARGIN = 0.05;
const MAX_PRICE_MARGIN = 0.25;

type ChartReadout = {
  open: number;
  high: number;
  low: number;
  close: number;
  time: Time;
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
  const activeTimeframeRef = useRef<string | null>(null);
  const priceMarginRef = useRef(DEFAULT_PRICE_MARGIN);
  const priceScaleDragRef = useRef<{ startY: number; startMargin: number } | null>(null);
  const pointerInspectingRef = useRef(false);
  const [readout, setReadout] = useState<ChartReadout | null>(null);
  const [isFullscreen, setIsFullscreen] = useState(false);

  const points = useMemo(
    () => normalizeCandles(candles.filter((candle) => candle.timeframe === timeframe)),
    [candles, timeframe],
  );
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
        minBarSpacing: 2,
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
      autoscaleInfoProvider: (baseImplementation: () => AutoscaleInfo | null) => {
        const autoscaleInfo = baseImplementation();
        if (!autoscaleInfo?.priceRange) return autoscaleInfo;
        return {
          ...autoscaleInfo,
          priceRange: {
            minValue: autoscaleInfo.priceRange.minValue - PRICE_RANGE_PADDING,
            maxValue: autoscaleInfo.priceRange.maxValue + PRICE_RANGE_PADDING,
          },
        };
      },
    });

    const handleCrosshair = (parameter: { time?: Time; seriesData: Map<unknown, unknown> }) => {
      const data = parameter.seriesData.get(series) as CandlestickData<Time> | undefined;
      if (!parameter.time || !data) {
        pointerInspectingRef.current = false;
        return;
      }
      pointerInspectingRef.current = true;
      setReadout({ open: data.open, high: data.high, low: data.low, close: data.close, time: data.time });
    };
    chart.subscribeCrosshairMove(handleCrosshair);

    const resize = new ResizeObserver(([entry]) => {
      const { width, height } = entry.contentRect;
      if (width > 0 && height > 0) chart.resize(Math.floor(width), Math.floor(height));
    });
    resize.observe(container);

    chartRef.current = chart;
    seriesRef.current = series;
    return () => {
      resize.disconnect();
      chart.unsubscribeCrosshairMove(handleCrosshair);
      chart.remove();
      chartRef.current = null;
      seriesRef.current = null;
    };
  }, []);

  useEffect(() => {
    const series = seriesRef.current;
    const chart = chartRef.current;
    if (!series || !chart) return;

    series.setData(points);
    if (!pointerInspectingRef.current) setReadout(latest);
    const weeklyWindowKey = utcDayStartMilliseconds === null ? null : `${timeframe}:${utcDayStartMilliseconds}`;
    if (points.length > 0 && weeklyWindowKey !== null && activeTimeframeRef.current !== weeklyWindowKey) {
      activeTimeframeRef.current = weeklyWindowKey;
      priceMarginRef.current = DEFAULT_PRICE_MARGIN;
      applyPriceScaleMargin(chart, DEFAULT_PRICE_MARGIN);
      fitWeeklyCandles(chart, points, utcDayStartMilliseconds);
    }
  }, [latest, points, timeframe, utcDayStartMilliseconds]);

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

  const displayed = readout ?? latest;
  return (
    <div className="chart-wrap" data-market-state={marketSession?.isOpen === false ? "closed" : "open"} ref={shellRef}>
      <div className="chart-canvas" ref={canvasRef} aria-label={`${points.length} ${timeframe} XAUUSD candles`} />
      {points.length === 0 ? (
        <div className="chart-empty" role="status">
          <strong>No chart evidence yet</strong>
          <span>Waiting for MT5 and stored market candles.</span>
        </div>
      ) : null}
      <div className="chart-watermark" aria-hidden="true">XAUUSD <span>{timeframe}</span></div>
      {displayed ? (
        <div className="chart-ohlc" aria-live="polite">
          <time>{formatChartTime(displayed.time)} UTC</time>
          <span>O <b>{price.format(displayed.open)}</b></span>
          <span>H <b>{price.format(displayed.high)}</b></span>
          <span>L <b>{price.format(displayed.low)}</b></span>
          <span>C <b className={displayed.close >= displayed.open ? "positive-text" : "negative-text"}>{price.format(displayed.close)}</b></span>
        </div>
      ) : null}
      <div className={`market-session ${isLive ? "live" : "paused"}`}>
        <i aria-hidden="true" />
        <strong>{isLive ? "XAUUSD live market" : marketSession?.isOpen === false ? "XAUUSD market closed" : providerConnected ? "XAUUSD market open · awaiting tick" : "MT5 provider offline"}</strong>
        {marketSession?.isOpen === false && latest ? <span>Last close {price.format(latest.close)} · {formatChartTime(latest.time)} UTC</span> : null}
        {marketSession && currentTime ? <span>{marketSession.nextTransitionLabel} / {marketSession.nextTransitionLocalLabel} · {formatSessionCountdown(currentTime, marketSession.nextTransitionAtUtc)}</span> : quote ? <span>Last tick {utcTime.format(new Date(quote.timestampUtc))} UTC</span> : null}
      </div>
      <div className="chart-controls" aria-label="Chart controls">
        <button type="button" onClick={() => zoomChart(chartRef.current, 0.78, points.length)} aria-label="Zoom in">+</button>
        <button type="button" onClick={() => zoomChart(chartRef.current, 1.28, points.length)} aria-label="Zoom out">−</button>
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

function zoomChart(chart: IChartApi | null, factor: number, pointCount: number) {
  if (!chart || pointCount === 0) return;
  const range = chart.timeScale().getVisibleLogicalRange();
  if (!range) return;
  const from = Math.max(0, range.from);
  const span = Math.max(4, (range.to - range.from) * factor);
  chart.timeScale().setVisibleLogicalRange({ from, to: from + span });
}

function fitWeeklyCandles(
  chart: IChartApi | null,
  points: CandlestickData<UTCTimestamp>[],
  utcDayStartMilliseconds: number | null,
) {
  if (!chart || points.length === 0 || utcDayStartMilliseconds === null) return;
  const windowStartSeconds = (utcDayStartMilliseconds - ((WEEK_WINDOW_DAYS - 1) * MILLISECONDS_PER_DAY)) / 1_000;
  const firstWeeklyIndex = points.findIndex((point) => Number(point.time) >= windowStartSeconds);
  const firstIndex = firstWeeklyIndex < 0 ? Math.max(0, points.length - 1) : firstWeeklyIndex;
  const lastIndex = points.length - 1;
  chart.timeScale().setVisibleLogicalRange({
    from: firstIndex,
    to: lastIndex + RIGHT_PADDING_BARS,
  });
}

function clampPriceMargin(value: number) {
  return Math.min(MAX_PRICE_MARGIN, Math.max(MIN_PRICE_MARGIN, value));
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
