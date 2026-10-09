"use client";

import { useEffect, useMemo, useRef, useState, type PointerEvent as ReactPointerEvent } from "react";
import {
  CandlestickSeries,
  ColorType,
  CrosshairMode,
  createChart,
  createSeriesMarkers,
  type CandlestickData,
  type HistogramData,
  HistogramSeries,
  type IChartApi,
  type IPriceLine,
  type ISeriesApi,
  type ISeriesMarkersPluginApi,
  type LineData,
  LineSeries,
  LineStyle,
  LineType,
  type Logical,
  type MouseEventParams,
  type SeriesMarker,
  type Time,
  type UTCTimestamp,
} from "lightweight-charts";
import type { MarketQuote, StoredMarketCandle } from "@/types/market";
import type { LocalSignalChartMarker, LocalSignalSnapshot } from "@/features/analysis/api/get-local-analyst";
import type { FullAnalysisResult } from "@/features/full-analysis/api/full-analyst";
import type { TargetAnalysisResult } from "@/features/target-analysis/api/target-analyst";
import type { XauUsdMarketSession } from "@/lib/market/xauusd-session";
import { DISPLAY_TIME_ZONE, DISPLAY_TIME_ZONE_LABEL } from "@/lib/time/utc-plus-seven";
import type { TechnicalAnalysis } from "@/types/analysis";

const price = new Intl.NumberFormat("en-US", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});
const displayTime = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
  timeZone: DISPLAY_TIME_ZONE,
});
const MILLISECONDS_PER_DAY = 86_400_000;
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

type DrawingTool = "navigate" | "line" | "draw" | "position" | "erase";
type DrawingPoint = { logical: Logical; price: number };
type LineDrawing = { start: DrawingPoint; end: DrawingPoint };
type ProjectedLine = { id: string; x1: number; y1: number; x2: number; y2: number };
type ProjectedPosition = {
  id: string;
  left: number;
  right: number;
  width: number;
  top: number;
  entryY: number;
  bottom: number;
  profitHeight: number;
  riskHeight: number;
  tpPrice: number;
  entryPrice: number;
  slPrice: number;
  handles: Array<{ x: number; y: number }>;
};

type LiveSignalPreview = {
  state: "Buy" | "Sell" | "Stop" | "Wait";
  confidence: number;
  reason: string;
  candleTime: UTCTimestamp;
};
type PositionLevel = "entry" | "outerA" | "outerB" | "start" | "end";
type PositionDrawing = {
  entryPrice: number;
  outerAPrice: number;
  outerBPrice: number;
  startLogical: Logical;
  endLogical: Logical;
};

type TradingChartProps = Readonly<{
  candles: StoredMarketCandle[];
  quote: MarketQuote | null;
  providerConnected: boolean;
  timeframe: string;
  nowUtcMilliseconds: number | null;
  marketSession: XauUsdMarketSession | null;
  signalMarkers: LocalSignalChartMarker[];
  localSignal: LocalSignalSnapshot | null;
  analysis: TechnicalAnalysis | null;
  targetAnalysis: TargetAnalysisResult | null;
  fullAnalysis: FullAnalysisResult | null;
}>;

export function TradingChart({ candles, quote, providerConnected, timeframe, nowUtcMilliseconds, marketSession, signalMarkers, localSignal, analysis, targetAnalysis, fullAnalysis }: TradingChartProps) {
  const shellRef = useRef<HTMLDivElement | null>(null);
  const canvasRef = useRef<HTMLDivElement | null>(null);
  const chartRef = useRef<IChartApi | null>(null);
  const seriesRef = useRef<ISeriesApi<"Candlestick"> | null>(null);
  const signalMarkersRef = useRef<ISeriesMarkersPluginApi<Time> | null>(null);
  const emaSeriesRef = useRef<ISeriesApi<"Line"> | null>(null);
  const priceFloorSeriesRef = useRef<ISeriesApi<"Line"> | null>(null);
  const targetSeriesRef = useRef<ISeriesApi<"Line"> | null>(null);
  const invalidationSeriesRef = useRef<ISeriesApi<"Line"> | null>(null);
  const bollingerUpperSeriesRef = useRef<ISeriesApi<"Line"> | null>(null);
  const bollingerMiddleSeriesRef = useRef<ISeriesApi<"Line"> | null>(null);
  const bollingerLowerSeriesRef = useRef<ISeriesApi<"Line"> | null>(null);
  const volumeSeriesRef = useRef<ISeriesApi<"Histogram"> | null>(null);
  const macdSeriesRef = useRef<ISeriesApi<"Histogram"> | null>(null);
  const ktrPriceLinesRef = useRef<IPriceLine[]>([]);
  const drawingToolRef = useRef<DrawingTool>("navigate");
  const timeframeRef = useRef(timeframe);
  const marketPriceRef = useRef<number | null>(quote?.bid ?? null);
  const drawingAnchorRef = useRef<DrawingPoint | null>(null);
  const lineDrawingsRef = useRef<LineDrawing[]>([]);
  const userPriceLinesRef = useRef<IPriceLine[]>([]);
  const userDrawingSeriesRef = useRef<ISeriesApi<"Line">[]>([]);
  const positionDrawingsRef = useRef<PositionDrawing[]>([]);
  const positionDragRef = useRef<{
    drawing: PositionDrawing;
    level: PositionLevel;
    pointerId: number;
    lastPrice: number;
    lastLogical: Logical | null;
  } | null>(null);
  const freehandPathsRef = useRef<string[]>([]);
  const activeFreehandPointsRef = useRef<Array<[number, number]> | null>(null);
  const activeTimeframeRef = useRef<string | null>(null);
  const fittedPointCountRef = useRef(0);
  const priceMarginRef = useRef(DEFAULT_PRICE_MARGIN);
  const priceScaleDragRef = useRef<{ startY: number; startMargin: number } | null>(null);
  const pointerInspectingRef = useRef(false);
  const [readout, setReadout] = useState<ChartReadout | null>(null);
  const [drawingTool, setDrawingTool] = useState<DrawingTool>("navigate");
  const [freehandPaths, setFreehandPaths] = useState<string[]>([]);
  const [projectedLines, setProjectedLines] = useState<ProjectedLine[]>([]);
  const [projectedPositions, setProjectedPositions] = useState<ProjectedPosition[]>([]);
  const [hasDrawings, setHasDrawings] = useState(false);
  const [showIndicators, setShowIndicators] = useState(true);

  const points = useMemo(
    () => normalizeCandles(candles.filter((candle) => candle.timeframe === timeframe)),
    [candles, timeframe],
  );
  const displayPoints = useMemo(
    () => addClosedSessionDisplayBars(points, timeframe),
    [points, timeframe],
  );
  const liveSignalPreview = useMemo(
    () => createLiveSignalPreview(candles, timeframe, analysis, localSignal, quote?.bid ?? null),
    [analysis, candles, localSignal, quote?.bid, timeframe],
  );
  const chartSignalMarkers = useMemo(() => [
    ...createSignalMarkers(points, signalMarkers),
    ...createLiveSignalMarker(points, liveSignalPreview),
    ...createFullAnalysisMarkers(points, fullAnalysis, timeframe),
    ...createTargetAnalysisMarkers(points, targetAnalysis, timeframe),
  ].sort((left, right) => Number(left.time) - Number(right.time)), [fullAnalysis, liveSignalPreview, points, signalMarkers, targetAnalysis, timeframe]);
  const hasClosedSessionBars = displayPoints.length > points.length;
  const latest = points.at(-1) ?? null;
  useEffect(() => {
    timeframeRef.current = timeframe;
    marketPriceRef.current = quote?.bid ?? latest?.close ?? null;
  }, [latest?.close, quote?.bid, timeframe]);
  const utcDayStartMilliseconds = nowUtcMilliseconds === null
    ? null
    : Math.floor(nowUtcMilliseconds / MILLISECONDS_PER_DAY) * MILLISECONDS_PER_DAY;
  const quoteAgeSeconds = quote && nowUtcMilliseconds !== null
    ? Math.max(0, (nowUtcMilliseconds - Date.parse(quote.timestampUtc)) / 1_000)
    : null;
  const isLive = marketSession?.isOpen === true && providerConnected && quoteAgeSeconds !== null && quoteAgeSeconds < 90;

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
        scaleMargins: { top: DEFAULT_PRICE_MARGIN, bottom: 0.38 },
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
    const targetSeries = chart.addSeries(LineSeries, {
      color: "#27d3c2",
      lineWidth: 2,
      lineStyle: LineStyle.Dashed,
      lineType: LineType.Simple,
      priceLineVisible: false,
      lastValueVisible: true,
      crosshairMarkerVisible: false,
      title: "Target",
    });
    const invalidationSeries = chart.addSeries(LineSeries, {
      color: "#ff6170",
      lineWidth: 1,
      lineStyle: LineStyle.Dotted,
      lineType: LineType.Simple,
      priceLineVisible: false,
      lastValueVisible: true,
      crosshairMarkerVisible: false,
      title: "Invalidation",
    });
    const bollingerUpperSeries = chart.addSeries(LineSeries, {
      color: "rgba(125, 164, 255, .65)", lineWidth: 1, lineStyle: LineStyle.Dotted,
      priceLineVisible: false, lastValueVisible: false, crosshairMarkerVisible: false,
    });
    const bollingerMiddleSeries = chart.addSeries(LineSeries, {
      color: "rgba(185, 202, 255, .42)", lineWidth: 1, lineStyle: LineStyle.Dashed,
      priceLineVisible: false, lastValueVisible: false, crosshairMarkerVisible: false,
    });
    const bollingerLowerSeries = chart.addSeries(LineSeries, {
      color: "rgba(125, 164, 255, .65)", lineWidth: 1, lineStyle: LineStyle.Dotted,
      priceLineVisible: false, lastValueVisible: false, crosshairMarkerVisible: false,
    });
    const volumeSeries = chart.addSeries(HistogramSeries, {
      priceScaleId: "volume",
      priceFormat: { type: "volume" },
      priceLineVisible: false,
      lastValueVisible: false,
    });
    chart.priceScale("volume").applyOptions({
      visible: false,
      scaleMargins: { top: 0.70, bottom: 0.17 },
    });
    const macdSeries = chart.addSeries(HistogramSeries, {
      priceScaleId: "macd",
      priceLineVisible: false,
      lastValueVisible: false,
      base: 0,
    });
    chart.priceScale("macd").applyOptions({
      visible: false,
      scaleMargins: { top: 0.86, bottom: 0.02 },
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
    const markerPlugin = createSeriesMarkers(series);
    let suppressNextChartClick = false;
    const refreshDrawings = () => {
      setProjectedLines(projectLineDrawings(chart, series, lineDrawingsRef.current));
      setProjectedPositions(projectPositionDrawings(chart, series, positionDrawingsRef.current));
    };
    const refreshDrawingState = () => {
      refreshDrawings();
      setFreehandPaths([...freehandPathsRef.current]);
      setHasDrawings(
        lineDrawingsRef.current.length > 0
        || positionDrawingsRef.current.length > 0
        || freehandPathsRef.current.length > 0,
      );
    };

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

    const finishDrawingStep = () => {
      drawingAnchorRef.current = null;
    };
    const handleChartClick = (parameter: MouseEventParams<Time>) => {
      if (suppressNextChartClick) {
        suppressNextChartClick = false;
        return;
      }
      const tool = drawingToolRef.current;
      if (tool === "navigate" || tool === "erase" || !parameter.point) return;
      const selectedPrice = series.coordinateToPrice(parameter.point.y);
      const selectedLogical = chart.timeScale().coordinateToLogical(parameter.point.x);
      if (selectedPrice === null || selectedLogical === null) return;
      const selectedPoint = { logical: selectedLogical, price: selectedPrice };

      if (tool === "line") {
        const anchor = drawingAnchorRef.current;
        if (!anchor) {
          drawingAnchorRef.current = selectedPoint;
          return;
        }
        lineDrawingsRef.current.push({ start: anchor, end: selectedPoint });
        refreshDrawings();
        setHasDrawings(true);
        finishDrawingStep();
        return;
      }

      if (tool === "draw") return;

      const entryPrice = marketPriceRef.current ?? selectedPrice;
      const distance = Math.max(1, Math.abs(entryPrice) * 0.0015);
      const intervalSeconds = TIMEFRAME_SECONDS[timeframeRef.current] ?? TIMEFRAME_SECONDS.M15;
      const defaultWidth = Math.max(1, Math.ceil((30 * 60) / intervalSeconds));
      const positionDrawing = {
        entryPrice,
        outerAPrice: entryPrice + distance,
        outerBPrice: entryPrice - distance,
        startLogical: selectedLogical,
        endLogical: (Number(selectedLogical) + defaultWidth) as Logical,
      };
      positionDrawingsRef.current.push(positionDrawing);
      refreshDrawings();
      setHasDrawings(true);
      finishDrawingStep();
    };
    chart.subscribeClick(handleChartClick);

    const localPoint = (event: PointerEvent): [number, number] => {
      const rect = container.getBoundingClientRect();
      return [event.clientX - rect.left, event.clientY - rect.top];
    };
    const finishPointerInteraction = (event: PointerEvent) => {
      if (activeFreehandPointsRef.current) {
        activeFreehandPointsRef.current = null;
      }
      if (positionDragRef.current?.pointerId === event.pointerId) positionDragRef.current = null;
      if (container.hasPointerCapture(event.pointerId)) container.releasePointerCapture(event.pointerId);
    };
    const handlePointerDown = (event: PointerEvent) => {
      if (event.button !== 0) return;
      if (drawingToolRef.current === "draw") {
        event.preventDefault();
        event.stopPropagation();
        container.setPointerCapture(event.pointerId);
        const point = localPoint(event);
        activeFreehandPointsRef.current = [point];
        const nextPaths = [...freehandPathsRef.current, freehandPath([point])];
        freehandPathsRef.current = nextPaths;
        setFreehandPaths(nextPaths);
        setHasDrawings(true);
        return;
      }
      if (drawingToolRef.current === "erase") {
        event.preventDefault();
        event.stopPropagation();
        const [x, y] = localPoint(event);
        const target = findDrawingAt(chart, series, lineDrawingsRef.current, positionDrawingsRef.current, freehandPathsRef.current, x, y);
        if (!target) return;
        if (target.kind === "line") lineDrawingsRef.current.splice(target.index, 1);
        if (target.kind === "position") positionDrawingsRef.current.splice(target.index, 1);
        if (target.kind === "freehand") freehandPathsRef.current.splice(target.index, 1);
        refreshDrawingState();
        return;
      }
      if (drawingToolRef.current !== "position") return;
      const [x, y] = localPoint(event);
      const dragTarget = nearestPositionLevel(chart, series, positionDrawingsRef.current, x, y);
      if (!dragTarget) return;
      event.preventDefault();
      event.stopPropagation();
      suppressNextChartClick = true;
      container.setPointerCapture(event.pointerId);
      positionDragRef.current = {
        ...dragTarget,
        pointerId: event.pointerId,
        lastPrice: dragTarget.price,
        lastLogical: chart.timeScale().coordinateToLogical(x),
      };
    };
    const handlePointerMove = (event: PointerEvent) => {
      const freehandPoints = activeFreehandPointsRef.current;
      if (freehandPoints) {
        event.preventDefault();
        event.stopPropagation();
        const point = localPoint(event);
        const previous = freehandPoints.at(-1);
        if (previous && Math.hypot(point[0] - previous[0], point[1] - previous[1]) < 2) return;
        freehandPoints.push(point);
        const nextPaths = [...freehandPathsRef.current];
        nextPaths[nextPaths.length - 1] = freehandPath(freehandPoints);
        freehandPathsRef.current = nextPaths;
        setFreehandPaths(nextPaths);
        return;
      }
      const drag = positionDragRef.current;
      if (!drag || drag.pointerId !== event.pointerId) return;
      event.preventDefault();
      event.stopPropagation();
      const [x, y] = localPoint(event);
      if (drag.level === "start" || drag.level === "end") {
        const nextLogical = chart.timeScale().coordinateToLogical(x);
        if (nextLogical === null) return;
        movePositionTime(drag.drawing, drag.level, nextLogical);
        refreshDrawings();
        return;
      }
      const nextPrice = series.coordinateToPrice(y);
      if (nextPrice === null) return;
      movePositionLevel(drag.drawing, drag.level, nextPrice, drag.lastPrice);
      drag.lastPrice = nextPrice;
      if (drag.level === "entry") {
        const nextLogical = chart.timeScale().coordinateToLogical(x);
        if (nextLogical !== null && drag.lastLogical !== null) {
          movePosition(Number(nextLogical) - Number(drag.lastLogical), drag.drawing);
          drag.lastLogical = nextLogical;
        }
      }
      refreshDrawings();
    };
    const handlePointerUp = (event: PointerEvent) => finishPointerInteraction(event);
    const handlePointerCancel = (event: PointerEvent) => finishPointerInteraction(event);
    container.addEventListener("pointerdown", handlePointerDown, true);
    container.addEventListener("pointermove", handlePointerMove, true);
    container.addEventListener("pointerup", handlePointerUp, true);
    container.addEventListener("pointercancel", handlePointerCancel, true);

    const resize = new ResizeObserver(([entry]) => {
      const { width, height } = entry.contentRect;
      if (width > 0 && height > 0) {
        chart.resize(Math.floor(width), Math.floor(height));
        refreshDrawings();
      }
    });
    resize.observe(container);
    chart.timeScale().subscribeVisibleLogicalRangeChange(refreshDrawings);

    chartRef.current = chart;
    seriesRef.current = series;
    signalMarkersRef.current = markerPlugin;
    emaSeriesRef.current = emaSeries;
    priceFloorSeriesRef.current = priceFloorSeries;
    targetSeriesRef.current = targetSeries;
    invalidationSeriesRef.current = invalidationSeries;
    bollingerUpperSeriesRef.current = bollingerUpperSeries;
    bollingerMiddleSeriesRef.current = bollingerMiddleSeries;
    bollingerLowerSeriesRef.current = bollingerLowerSeries;
    volumeSeriesRef.current = volumeSeries;
    macdSeriesRef.current = macdSeries;
    return () => {
      resize.disconnect();
      chart.timeScale().unsubscribeVisibleLogicalRangeChange(refreshDrawings);
      chart.unsubscribeCrosshairMove(handleCrosshair);
      chart.unsubscribeClick(handleChartClick);
      container.removeEventListener("pointerdown", handlePointerDown, true);
      container.removeEventListener("pointermove", handlePointerMove, true);
      container.removeEventListener("pointerup", handlePointerUp, true);
      container.removeEventListener("pointercancel", handlePointerCancel, true);
      chart.remove();
      chartRef.current = null;
      seriesRef.current = null;
      signalMarkersRef.current = null;
      emaSeriesRef.current = null;
      priceFloorSeriesRef.current = null;
      targetSeriesRef.current = null;
      invalidationSeriesRef.current = null;
      bollingerUpperSeriesRef.current = null;
      bollingerMiddleSeriesRef.current = null;
      bollingerLowerSeriesRef.current = null;
      volumeSeriesRef.current = null;
      macdSeriesRef.current = null;
      ktrPriceLinesRef.current = [];
      userPriceLinesRef.current = [];
      userDrawingSeriesRef.current = [];
      lineDrawingsRef.current = [];
      positionDrawingsRef.current = [];
      positionDragRef.current = null;
      freehandPathsRef.current = [];
      activeFreehandPointsRef.current = null;
    };
  }, []);

  useEffect(() => {
    chartRef.current?.applyOptions({
      handleScroll: {
        mouseWheel: true,
        pressedMouseMove: drawingTool === "navigate",
        horzTouchDrag: drawingTool === "navigate",
        vertTouchDrag: false,
      },
    });
  }, [drawingTool]);

  useEffect(() => {
    const series = seriesRef.current;
    const chart = chartRef.current;
    if (!series || !chart) return;

    series.setData(displayPoints);
    signalMarkersRef.current?.setMarkers(chartSignalMarkers);
    emaSeriesRef.current?.setData(showIndicators ? calculateEma(points, EMA_PERIOD) : []);
    priceFloorSeriesRef.current?.setData(showIndicators ? calculateRollingLow(points, PRICE_FLOOR_PERIOD) : []);
    const bollinger = showIndicators ? calculateBollingerBands(points, 20, 2) : null;
    bollingerUpperSeriesRef.current?.setData(bollinger?.upper ?? []);
    bollingerMiddleSeriesRef.current?.setData(bollinger?.middle ?? []);
    bollingerLowerSeriesRef.current?.setData(bollinger?.lower ?? []);
    volumeSeriesRef.current?.setData(showIndicators ? calculateTickVolume(candles, timeframe) : []);
    macdSeriesRef.current?.setData(showIndicators ? calculateMacdHistogram(points, 12, 26, 9) : []);
    applyPriceScaleMargin(chart, priceMarginRef.current, showIndicators);
    if (!pointerInspectingRef.current) setReadout(latest);
    const weeklyWindowKey = utcDayStartMilliseconds === null ? null : `${timeframe}:${utcDayStartMilliseconds}`;
    const historyExpanded = points.length - fittedPointCountRef.current > 1;
    if (points.length > 0
        && weeklyWindowKey !== null
        && (activeTimeframeRef.current !== weeklyWindowKey || historyExpanded)) {
      activeTimeframeRef.current = weeklyWindowKey;
      priceMarginRef.current = DEFAULT_PRICE_MARGIN;
      applyPriceScaleMargin(chart, DEFAULT_PRICE_MARGIN, showIndicators);
      fitRecentCandles(chart, points, timeframe);
    }
    fittedPointCountRef.current = points.length;
  }, [candles, chartSignalMarkers, displayPoints, latest, points, showIndicators, timeframe, utcDayStartMilliseconds]);

  useEffect(() => {
    const visibleTarget = isVisibleTarget(targetAnalysis, timeframe) ? targetAnalysis : null;
    targetSeriesRef.current?.setData(createHorizontalLevel(points, visibleTarget?.targetPrice ?? null));
    invalidationSeriesRef.current?.setData(createHorizontalLevel(points, visibleTarget?.invalidationPrice ?? null));
  }, [points, targetAnalysis, timeframe]);

  useEffect(() => {
    const series = seriesRef.current;
    if (!series) return;
    for (const line of ktrPriceLinesRef.current) series.removePriceLine(line);
    ktrPriceLinesRef.current = [];
    if (!showIndicators || analysis?.timeframe !== timeframe || !analysis.strategies) return;
    ktrPriceLinesRef.current = analysis.strategies.ktr.levels.map((level) => series.createPriceLine({
      price: level.price,
      color: level.multiple === 0 ? "#d6dde0" : level.multiple > 0 ? "#e6b95e" : "#60d79f",
      lineWidth: level.multiple === 0 ? 1 : 1,
      lineStyle: level.multiple === 0 ? LineStyle.Dotted : LineStyle.Dashed,
      axisLabelVisible: true,
      title: level.label,
    }));
  }, [analysis, showIndicators, timeframe]);

  useEffect(() => {
    seriesRef.current?.applyOptions({
      priceLineColor: marketSession?.isOpen === false ? "#718087" : "#00d897",
    });
  }, [marketSession?.isOpen]);

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
    applyPriceScaleMargin(chartRef.current, margin, showIndicators);
    setProjectedLines(projectLineDrawings(chartRef.current, seriesRef.current, lineDrawingsRef.current));
    setProjectedPositions(projectPositionDrawings(chartRef.current, seriesRef.current, positionDrawingsRef.current));
  };
  const endPriceScale = (event: ReactPointerEvent<HTMLButtonElement>) => {
    if (event.currentTarget.hasPointerCapture(event.pointerId)) event.currentTarget.releasePointerCapture(event.pointerId);
    priceScaleDragRef.current = null;
  };
  const resetPriceScale = () => {
    priceMarginRef.current = DEFAULT_PRICE_MARGIN;
    applyPriceScaleMargin(chartRef.current, DEFAULT_PRICE_MARGIN, showIndicators);
    setProjectedLines(projectLineDrawings(chartRef.current, seriesRef.current, lineDrawingsRef.current));
    setProjectedPositions(projectPositionDrawings(chartRef.current, seriesRef.current, positionDrawingsRef.current));
  };
  const selectDrawingTool = (tool: DrawingTool) => {
    drawingToolRef.current = tool;
    drawingAnchorRef.current = null;
    setDrawingTool(tool);
  };
  const clearDrawings = () => {
    const series = seriesRef.current;
    const chart = chartRef.current;
    if (series) {
      for (const line of userPriceLinesRef.current) series.removePriceLine(line);
    }
    if (chart) {
      for (const drawingSeries of userDrawingSeriesRef.current) chart.removeSeries(drawingSeries);
    }
    userPriceLinesRef.current = [];
    userDrawingSeriesRef.current = [];
    lineDrawingsRef.current = [];
    positionDrawingsRef.current = [];
    positionDragRef.current = null;
    freehandPathsRef.current = [];
    activeFreehandPointsRef.current = null;
    drawingAnchorRef.current = null;
    setFreehandPaths([]);
    setProjectedLines([]);
    setProjectedPositions([]);
    setHasDrawings(false);
  };

  const displayed: ChartReadout | null = readout ?? (latest
    ? { ...latest, closedSession: false }
    : null);
  return (
    <div
      className="chart-wrap"
      data-market-state={marketSession?.isOpen === false ? "closed" : "open"}
      data-drawing-tool={drawingTool}
      ref={shellRef}
    >
      <div className="chart-canvas" ref={canvasRef} aria-label={`${points.length} ${timeframe} XAUUSD candles and ${chartSignalMarkers.length} signal or analyst events`} />
      <svg className="chart-drawing-layer" aria-hidden="true">
        {projectedPositions.map((drawing) => (
          <g key={drawing.id} className="position-drawing">
            <rect className="position-profit-zone" x={drawing.left} y={drawing.top} width={drawing.width} height={drawing.profitHeight} />
            <rect className="position-risk-zone" x={drawing.left} y={drawing.entryY} width={drawing.width} height={drawing.riskHeight} />
            <line className="position-tp-line" x1={drawing.left} y1={drawing.top} x2={drawing.right} y2={drawing.top} />
            <line className="position-entry-line" x1={drawing.left} y1={drawing.entryY} x2={drawing.right} y2={drawing.entryY} />
            <line className="position-sl-line" x1={drawing.left} y1={drawing.bottom} x2={drawing.right} y2={drawing.bottom} />
            <text className="position-label position-label-tp" x={drawing.left + 5} y={drawing.top - 5}>TP {price.format(drawing.tpPrice)}</text>
            <text className="position-label position-label-entry" x={drawing.left + 5} y={drawing.entryY - 5}>ENTRY {price.format(drawing.entryPrice)}</text>
            <text className="position-label position-label-sl" x={drawing.left + 5} y={drawing.bottom - 5}>SL {price.format(drawing.slPrice)}</text>
            {drawing.handles.map((handle) => <rect key={`${drawing.id}:${handle.x}:${handle.y}`} className="position-handle" x={handle.x - 4} y={handle.y - 4} width="8" height="8" rx="1" />)}
          </g>
        ))}
        {projectedLines.map((line) => <line key={line.id} className="user-trend-line" x1={line.x1} y1={line.y1} x2={line.x2} y2={line.y2} />)}
        {freehandPaths.map((pathValue, index) => <path key={`${index}-${pathValue.length}`} d={pathValue} />)}
      </svg>
      {points.length === 0 ? (
        <div className="chart-empty" role="status">
          <strong>No chart evidence yet</strong>
          <span>Waiting for AllTick and stored market candles.</span>
        </div>
      ) : null}
      <div className="chart-watermark" aria-hidden="true">XAUUSD <span>{timeframe}</span></div>
      {displayed ? (
        <div className="chart-ohlc" aria-live="polite">
          <time>{formatChartTime(displayed.time)} {DISPLAY_TIME_ZONE_LABEL}</time>
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
          {showIndicators ? <span data-overlay="ema">EMA {EMA_PERIOD}</span> : null}
          {showIndicators ? <span data-overlay="bollinger">Bollinger 20</span> : null}
          {showIndicators ? <span data-overlay="ktr">OP / KTR</span> : null}
          {chartSignalMarkers.length > 0 ? <span data-overlay="signal">Signal events</span> : null}
          {isVisibleTarget(targetAnalysis, timeframe) ? <span data-overlay="target">Target levels</span> : null}
          {isVisibleFullAnalysis(fullAnalysis, timeframe) ? <span data-overlay="full">Future analysis</span> : null}
          {hasClosedSessionBars ? <span data-overlay="closed">Closed-session carry</span> : null}
        </div>
      ) : null}
      {liveSignalPreview ? (
        <div className="live-signal-preview" data-state={liveSignalPreview.state.toLowerCase()} aria-live="polite">
          <span>Live candle · provisional</span>
          <strong>{liveSignalPreview.state.toUpperCase()} {Math.round(liveSignalPreview.confidence * 100)}%</strong>
          <small>{liveSignalPreview.reason}</small>
        </div>
      ) : null}
      {showIndicators && points.length > 0 ? (
        <div className="chart-indicator-readout" aria-label="Measured chart indicators">
          <span data-pane="volume">
            <b>Tick activity</b>
            {analysis?.strategies?.flow.tickVolumeRatio === null || analysis?.strategies?.flow.tickVolumeRatio === undefined
              ? " unavailable"
              : ` ${analysis.strategies.flow.tickVolumeRatio.toFixed(2)}x · ${analysis.strategies.flow.direction}`}
          </span>
          <span data-pane="momentum">
            <b>MACD</b> {formatIndicator(analysis?.indicators.macd.histogram)} · <b>RSI</b> {formatIndicator(analysis?.indicators.rsi.value)}
          </span>
        </div>
      ) : null}
      <div className={`market-session ${isLive ? "live" : "paused"}`}>
        <i aria-hidden="true" />
        <strong>{isLive ? "XAUUSD live market" : marketSession?.isOpen === false ? "XAUUSD market closed" : providerConnected ? "XAUUSD market open · awaiting tick" : "XAUUSD market waiting"}</strong>
        {marketSession ? <span>{marketSession.weeklyOpenLabel} · {marketSession.weeklyCloseLabel}</span> : null}
        {marketSession ? <span>{marketSession.nextTradingSessionLabel}</span> : null}
      </div>
      <div className="chart-controls" aria-label="Chart controls">
        <button type="button" title="Toggle EMA, Bollinger, OP/KTR, tick activity and MACD panels" aria-label="Indicators" className={showIndicators ? "active indicator-toggle" : "indicator-toggle"} aria-pressed={showIndicators} onClick={() => setShowIndicators((value) => !value)}>
          <svg viewBox="0 0 20 20" aria-hidden="true"><path d="M3 14h2V9H3v5Zm4 0h2V5H7v9Zm4 0h2V8h-2v6Zm4 0h2V3h-2v11Z" /><path d="M2 16h16" /></svg>
        </button>
        <button type="button" title="Navigate: drag or scroll the market chart" aria-label="Navigate chart" className={drawingTool === "navigate" ? "active" : ""} aria-pressed={drawingTool === "navigate"} onClick={() => selectDrawingTool("navigate")}>
          <svg viewBox="0 0 20 20" aria-hidden="true"><path d="M7 9V4a1.5 1.5 0 0 1 3 0v4-3a1.5 1.5 0 0 1 3 0v3-2a1.5 1.5 0 0 1 3 0v5c0 4-2 7-6 7H8c-2 0-3-1-4-3l-2-4a1.5 1.5 0 0 1 2.6-1.5L7 12V9Z" /></svg>
        </button>
        <button type="button" title="Line: select two chart points" aria-label="Line" className={drawingTool === "line" ? "active" : ""} aria-pressed={drawingTool === "line"} onClick={() => selectDrawingTool("line")}>
          <svg viewBox="0 0 20 20" aria-hidden="true"><path d="M3 16 17 4" /><circle cx="3" cy="16" r="1.5" /><circle cx="17" cy="4" r="1.5" /></svg>
        </button>
        <button type="button" title="Pencil: drag to draw" aria-label="Freehand draw" className={drawingTool === "draw" ? "active" : ""} aria-pressed={drawingTool === "draw"} onClick={() => selectDrawingTool("draw")}>
          <svg viewBox="0 0 20 20" aria-hidden="true"><path d="m4 15 1-4L14 2l4 4-9 9-4 1Z" /><path d="m12.5 3.5 4 4M5 11l4 4" /></svg>
        </button>
        <button type="button" title="Position: start a 30-minute Entry, TP and SL box at live market price" aria-label="Position" className={drawingTool === "position" ? "active" : ""} aria-pressed={drawingTool === "position"} onClick={() => selectDrawingTool("position")}>
          <svg viewBox="0 0 20 20" aria-hidden="true"><path d="M3 5h14M3 10h14M3 15h14" /><path d="m14 2 3 3-3 3M6 7 3 10l3 3M14 12l3 3-3 3" /></svg>
        </button>
        <button type="button" title="Erase: click one line, drawing, or position to remove it" aria-label="Erase one drawing" className={drawingTool === "erase" ? "active" : ""} aria-pressed={drawingTool === "erase"} onClick={() => selectDrawingTool("erase")}>
          <svg viewBox="0 0 20 20" aria-hidden="true"><path d="m4 12 7-7 5 5-7 7H5l-3-3 2-2Z" /><path d="M9 17h8" /></svg>
        </button>
        <button type="button" title="Clear drawings" aria-label="Clear drawings" onClick={clearDrawings} disabled={!hasDrawings}>
          <svg viewBox="0 0 20 20" aria-hidden="true"><path d="m5 12 7-7 4 4-7 7H5l-2-2 2-2Z" /><path d="M9 16h8" /></svg>
        </button>
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

function createSignalMarkers(
  points: CandlestickData<UTCTimestamp>[],
  signalMarkers: LocalSignalChartMarker[],
): SeriesMarker<UTCTimestamp>[] {
  const candleTimes = new Set(points.map((point) => Number(point.time)));
  const seenMarkers = new Set<string>();
  const markers: SeriesMarker<UTCTimestamp>[] = [];

  for (const signal of signalMarkers) {
    const timestamp = Math.floor(Date.parse(signal.candleTimeUtc) / 1_000);
    if (!Number.isFinite(timestamp) || !candleTimes.has(timestamp)) continue;

    const marker = signalMarkerAppearance(signal);
    if (!marker) continue;

    const id = `${signal.signalId}:${signal.state}:${timestamp}`;
    if (seenMarkers.has(id)) continue;
    seenMarkers.add(id);
    markers.push({
      id,
      time: timestamp as UTCTimestamp,
      position: "belowBar",
      ...marker,
    });
  }

  return markers.sort((left, right) => Number(left.time) - Number(right.time));
}

function signalMarkerAppearance(
  signal: LocalSignalChartMarker,
): Pick<SeriesMarker<UTCTimestamp>, "color" | "shape" | "text"> | null {
  switch (signal.state) {
    case "Buy":
      return { color: "#00d897", shape: "arrowUp", text: "BUY" };
    case "Sell":
      return { color: "#ff465d", shape: "arrowDown", text: "SELL" };
    case "Stop":
      return { color: "#e6b95e", shape: "circle", text: "STOP" };
    default:
      return null;
  }
}

function createLiveSignalPreview(
  candles: StoredMarketCandle[],
  timeframe: string,
  analysis: TechnicalAnalysis | null,
  activeSignal: LocalSignalSnapshot | null,
  quotePrice: number | null,
): LiveSignalPreview | null {
  const forming = candles
    .filter((candle) => candle.timeframe === timeframe && !candle.isComplete)
    .sort((left, right) => Date.parse(left.openTimeUtc) - Date.parse(right.openTimeUtc))
    .at(-1);
  if (!forming) return null;
  const timestamp = Math.floor(Date.parse(forming.openTimeUtc) / 1_000);
  if (!Number.isFinite(timestamp)) return null;

  let bullish = 0;
  let bearish = 0;
  const evidence: string[] = [];
  const range = Math.max(0.01, forming.high - forming.low);
  if (forming.close - forming.open > range * 0.08) {
    bullish += 1;
    evidence.push("forming candle bullish");
  } else if (forming.open - forming.close > range * 0.08) {
    bearish += 1;
    evidence.push("forming candle bearish");
  }
  if (analysis?.trend.direction === "Bullish") bullish += 1;
  if (analysis?.trend.direction === "Bearish") bearish += 1;
  if (analysis?.marketStructure.direction === "Bullish") bullish += 1;
  if (analysis?.marketStructure.direction === "Bearish") bearish += 1;
  if ((analysis?.indicators.macd.histogram ?? 0) > 0) bullish += 0.75;
  if ((analysis?.indicators.macd.histogram ?? 0) < 0) bearish += 0.75;
  if (analysis?.strategies?.flow.direction === "Bullish") bullish += 0.75;
  if (analysis?.strategies?.flow.direction === "Bearish") bearish += 0.75;
  if ((analysis?.indicators.rsi.value ?? 50) <= 35) bullish += 0.5;
  if ((analysis?.indicators.rsi.value ?? 50) >= 65) bearish += 0.5;

  const marketPrice = quotePrice ?? forming.close;
  const activeDirection = activeSignal?.status === "ACTIVE"
    ? activeSignal.originDirection ?? activeSignal.state
    : null;
  if (activeDirection === "Buy" || activeDirection === "Sell") {
    const invalidated = activeDirection === "Buy"
      ? activeSignal?.invalidationPrice !== null && activeSignal?.invalidationPrice !== undefined && marketPrice <= activeSignal.invalidationPrice
      : activeSignal?.invalidationPrice !== null && activeSignal?.invalidationPrice !== undefined && marketPrice >= activeSignal.invalidationPrice;
    const targetTouched = activeDirection === "Buy"
      ? activeSignal?.targetPrice !== null && activeSignal?.targetPrice !== undefined && marketPrice >= activeSignal.targetPrice
      : activeSignal?.targetPrice !== null && activeSignal?.targetPrice !== undefined && marketPrice <= activeSignal.targetPrice;
    const opposing = activeDirection === "Buy" ? bearish - bullish : bullish - bearish;
    if (invalidated || targetTouched || opposing >= 2.5) {
      return {
        state: "Stop",
        confidence: invalidated || targetTouched ? 0.95 : Math.min(0.9, 0.55 + (opposing * 0.08)),
        reason: invalidated ? "live invalidation touched" : targetTouched ? "live target touched" : "strong live opposing evidence",
        candleTime: timestamp as UTCTimestamp,
      };
    }
    return {
      state: activeDirection,
      confidence: Math.max(0.5, activeSignal?.confidence ?? 0),
      reason: `active ${activeDirection.toLowerCase()} remains valid on live price`,
      candleTime: timestamp as UTCTimestamp,
    };
  }

  const directionalDifference = bullish - bearish;
  if (Math.abs(directionalDifference) < 1.5) {
    return {
      state: "Wait",
      confidence: Math.min(0.74, 0.45 + (Math.abs(directionalDifference) * 0.1)),
      reason: evidence[0] ?? "live evidence is balanced",
      candleTime: timestamp as UTCTimestamp,
    };
  }
  return {
    state: directionalDifference > 0 ? "Buy" : "Sell",
    confidence: Math.min(0.9, 0.5 + (Math.abs(directionalDifference) * 0.08)),
    reason: `${directionalDifference > 0 ? "bullish" : "bearish"} live confluence · ${evidence[0] ?? "closed-candle context"}`,
    candleTime: timestamp as UTCTimestamp,
  };
}

function createLiveSignalMarker(
  points: CandlestickData<UTCTimestamp>[],
  preview: LiveSignalPreview | null,
): SeriesMarker<UTCTimestamp>[] {
  if (!preview || !points.some((point) => Number(point.time) === Number(preview.candleTime))) return [];
  const appearance = preview.state === "Buy"
    ? { color: "#54f0bd", shape: "arrowUp" as const, text: "LIVE BUY?" }
    : preview.state === "Sell"
      ? { color: "#ff7080", shape: "arrowDown" as const, text: "LIVE SELL?" }
      : preview.state === "Stop"
        ? { color: "#f1cd78", shape: "circle" as const, text: "LIVE STOP?" }
        : { color: "#a7b4b9", shape: "circle" as const, text: "LIVE WAIT" };
  return [{
    id: `live:${preview.candleTime}:${preview.state}`,
    time: preview.candleTime,
    position: "belowBar",
    ...appearance,
  }];
}

function createFullAnalysisMarkers(
  points: CandlestickData<UTCTimestamp>[],
  analysis: FullAnalysisResult | null,
  timeframe: string,
): SeriesMarker<UTCTimestamp>[] {
  if (!isVisibleFullAnalysis(analysis, timeframe) || !analysis || points.length === 0) return [];

  const analysisTimestamp = Math.floor(Date.parse(analysis.analysisTimeUtc) / 1_000);
  const candle = points.findLast((point) => Number(point.time) <= analysisTimestamp) ?? points.at(-1);
  if (!candle) return [];

  if (analysis.decision === "Buy") {
    return [{ id: `full:${analysis.id}`, time: candle.time, position: "belowBar", color: "#00d897", shape: "arrowUp", text: "FUTURE BUY" }];
  }
  if (analysis.decision === "Sell") {
    return [{ id: `full:${analysis.id}`, time: candle.time, position: "belowBar", color: "#ff465d", shape: "arrowDown", text: "FUTURE SELL" }];
  }
  return [{ id: `full:${analysis.id}`, time: candle.time, position: "belowBar", color: "#e6b95e", shape: "circle", text: "FUTURE WAIT" }];
}

function createTargetAnalysisMarkers(
  points: CandlestickData<UTCTimestamp>[],
  analysis: TargetAnalysisResult | null,
  timeframe: string,
): SeriesMarker<UTCTimestamp>[] {
  if (!isVisibleTarget(analysis, timeframe) || !analysis || points.length === 0) return [];
  const analysisTimestamp = Math.floor(Date.parse(analysis.analysisTimeUtc) / 1_000);
  const candle = points.findLast((point) => Number(point.time) <= analysisTimestamp) ?? points.at(-1);
  if (!candle) return [];
  const direction = targetDirection(analysis);
  if (direction === "up") {
    return [{ id: `target:${analysis.id}`, time: candle.time, position: "belowBar", color: "#27d3c2", shape: "arrowUp", text: "TARGET BUY" }];
  }
  if (direction === "down") {
    return [{ id: `target:${analysis.id}`, time: candle.time, position: "belowBar", color: "#ff7080", shape: "arrowDown", text: "TARGET SELL" }];
  }
  return [{ id: `target:${analysis.id}`, time: candle.time, position: "belowBar", color: "#e6b95e", shape: "circle", text: "TARGET WAIT" }];
}

function createHorizontalLevel(
  points: CandlestickData<UTCTimestamp>[],
  value: number | null,
): LineData<UTCTimestamp>[] {
  if (value === null || points.length === 0) return [];
  if (points.length === 1) return [{ time: points[0].time, value }];
  return [
    { time: points[0].time, value },
    { time: points[points.length - 1].time, value },
  ];
}

function isVisibleTarget(analysis: TargetAnalysisResult | null, timeframe: string): boolean {
  return analysis !== null
    && analysis.timeframe === timeframe
    && !["Cancelled", "Expired", "Invalidated", "TargetHit"].includes(analysis.status);
}

function isVisibleFullAnalysis(analysis: FullAnalysisResult | null, timeframe: string): boolean {
  return analysis !== null
    && analysis.timeframe === timeframe
    && !["Cancelled", "Expired", "Invalidated"].includes(analysis.status);
}

function targetDirection(analysis: TargetAnalysisResult): "up" | "down" | "none" {
  if (analysis.currentPrice === null || analysis.targetPrice === null) return "none";
  return analysis.targetPrice >= analysis.currentPrice ? "up" : "down";
}

function formatIndicator(value: number | null | undefined): string {
  return value === null || value === undefined ? "—" : value.toFixed(2);
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

function calculateBollingerBands(
  points: CandlestickData<UTCTimestamp>[],
  period: number,
  standardDeviations: number,
): { upper: LineData<UTCTimestamp>[]; middle: LineData<UTCTimestamp>[]; lower: LineData<UTCTimestamp>[] } {
  const upper: LineData<UTCTimestamp>[] = [];
  const middle: LineData<UTCTimestamp>[] = [];
  const lower: LineData<UTCTimestamp>[] = [];
  for (let index = period - 1; index < points.length; index += 1) {
    const closes = points.slice(index - period + 1, index + 1).map((point) => point.close);
    const mean = closes.reduce((total, value) => total + value, 0) / period;
    const deviation = Math.sqrt(closes.reduce((total, value) => total + ((value - mean) ** 2), 0) / period);
    const time = points[index].time;
    upper.push({ time, value: mean + (deviation * standardDeviations) });
    middle.push({ time, value: mean });
    lower.push({ time, value: mean - (deviation * standardDeviations) });
  }
  return { upper, middle, lower };
}

function calculateTickVolume(candles: StoredMarketCandle[], timeframe: string): HistogramData<UTCTimestamp>[] {
  return candles
    .filter((candle) => candle.timeframe === timeframe && candle.tickVolume !== null)
    .map((candle) => ({
      time: Math.floor(Date.parse(candle.openTimeUtc) / 1_000) as UTCTimestamp,
      value: candle.tickVolume ?? 0,
      color: candle.close >= candle.open ? "rgba(0, 216, 151, .72)" : "rgba(255, 70, 93, .72)",
    }))
    .filter((item) => Number.isFinite(Number(item.time)))
    .sort((left, right) => Number(left.time) - Number(right.time));
}

function calculateMacdHistogram(
  points: CandlestickData<UTCTimestamp>[],
  fastPeriod: number,
  slowPeriod: number,
  signalPeriod: number,
): HistogramData<UTCTimestamp>[] {
  if (points.length < slowPeriod + signalPeriod) return [];
  const fastMultiplier = 2 / (fastPeriod + 1);
  const slowMultiplier = 2 / (slowPeriod + 1);
  const signalMultiplier = 2 / (signalPeriod + 1);
  let fast = points.slice(0, fastPeriod).reduce((total, point) => total + point.close, 0) / fastPeriod;
  let slow = points.slice(0, slowPeriod).reduce((total, point) => total + point.close, 0) / slowPeriod;
  const macdValues: Array<{ time: UTCTimestamp; value: number }> = [];
  for (let index = slowPeriod; index < points.length; index += 1) {
    fast = ((points[index].close - fast) * fastMultiplier) + fast;
    slow = ((points[index].close - slow) * slowMultiplier) + slow;
    macdValues.push({ time: points[index].time, value: fast - slow });
  }
  if (macdValues.length < signalPeriod) return [];
  let signal = macdValues.slice(0, signalPeriod).reduce((total, point) => total + point.value, 0) / signalPeriod;
  return macdValues.slice(signalPeriod - 1).map((point, index) => {
    if (index > 0) signal = ((point.value - signal) * signalMultiplier) + signal;
    const value = point.value - signal;
    return { time: point.time, value, color: value >= 0 ? "rgba(55, 159, 255, .82)" : "rgba(129, 103, 255, .82)" };
  });
}

function applyPriceScaleMargin(chart: IChartApi | null, margin: number, showIndicators: boolean) {
  if (!chart) return;
  chart.priceScale("right").applyOptions({
    autoScale: true,
    scaleMargins: { top: margin, bottom: showIndicators ? 0.38 : margin },
  });
}

function formatChartTime(time: Time): string {
  if (typeof time === "number") return displayTime.format(new Date(time * 1_000));
  if (typeof time === "string") return time;
  return `${String(time.day).padStart(2, "0")}/${String(time.month).padStart(2, "0")}/${time.year}`;
}

function nearestPositionLevel(
  chart: IChartApi,
  series: ISeriesApi<"Candlestick">,
  drawings: PositionDrawing[],
  pointerX: number,
  pointerY: number,
): { drawing: PositionDrawing; level: PositionLevel; price: number } | null {
  let nearest: { drawing: PositionDrawing; level: PositionLevel; price: number; distance: number } | null = null;
  for (const drawing of drawings) {
    const startX = chart.timeScale().logicalToCoordinate(drawing.startLogical);
    const endX = chart.timeScale().logicalToCoordinate(drawing.endLogical);
    const topY = series.priceToCoordinate(Math.max(drawing.outerAPrice, drawing.outerBPrice));
    const bottomY = series.priceToCoordinate(Math.min(drawing.outerAPrice, drawing.outerBPrice));
    if (startX === null || endX === null || topY === null || bottomY === null) continue;
    const left = Math.min(Number(startX), Number(endX));
    const right = Math.max(Number(startX), Number(endX));
    const top = Math.min(Number(topY), Number(bottomY));
    const bottom = Math.max(Number(topY), Number(bottomY));
    if (pointerY >= top - 10 && pointerY <= bottom + 10) {
      const startDistance = Math.abs(pointerX - Number(startX));
      const endDistance = Math.abs(pointerX - Number(endX));
      if (startDistance <= 10 && (!nearest || startDistance < nearest.distance)) {
        nearest = { drawing, level: "start", price: drawing.entryPrice, distance: startDistance };
      }
      if (endDistance <= 10 && (!nearest || endDistance < nearest.distance)) {
        nearest = { drawing, level: "end", price: drawing.entryPrice, distance: endDistance };
      }
    }
    if (pointerX < left - 10 || pointerX > right + 10) continue;
    const levels: Array<[PositionLevel, number]> = [
      ["entry", drawing.entryPrice],
      ["outerA", drawing.outerAPrice],
      ["outerB", drawing.outerBPrice],
    ];
    for (const [level, priceValue] of levels) {
      const coordinate = series.priceToCoordinate(priceValue);
      if (coordinate === null) continue;
      const distance = Math.abs(Number(coordinate) - pointerY);
      if (distance <= 10 && (!nearest || distance < nearest.distance)) {
        nearest = { drawing, level, price: priceValue, distance };
      }
    }
  }
  return nearest;
}

function movePositionTime(drawing: PositionDrawing, level: "start" | "end", nextLogical: Logical) {
  const minimumWidth = 1;
  if (level === "start") {
    drawing.startLogical = Math.min(Number(nextLogical), Number(drawing.endLogical) - minimumWidth) as Logical;
  } else {
    drawing.endLogical = Math.max(Number(nextLogical), Number(drawing.startLogical) + minimumWidth) as Logical;
  }
}

function movePosition(deltaLogical: number, drawing: PositionDrawing) {
  drawing.startLogical = (Number(drawing.startLogical) + deltaLogical) as Logical;
  drawing.endLogical = (Number(drawing.endLogical) + deltaLogical) as Logical;
}

function movePositionLevel(
  drawing: PositionDrawing,
  level: Exclude<PositionLevel, "start" | "end">,
  nextPrice: number,
  previousPrice: number,
) {
  if (level === "entry") {
    const delta = nextPrice - previousPrice;
    drawing.entryPrice += delta;
    drawing.outerAPrice += delta;
    drawing.outerBPrice += delta;
  } else {
    const changedKey = level === "outerA" ? "outerAPrice" : "outerBPrice";
    const otherKey = level === "outerA" ? "outerBPrice" : "outerAPrice";
    const previousSide = Math.sign(drawing[changedKey] - drawing.entryPrice) || 1;
    const minimumDistance = Math.max(0.01, Math.abs(drawing.entryPrice) * 0.00001);
    drawing[changedKey] = Math.abs(nextPrice - drawing.entryPrice) < minimumDistance
      ? drawing.entryPrice + previousSide * minimumDistance
      : nextPrice;
    const changedSide = Math.sign(drawing[changedKey] - drawing.entryPrice);
    const otherSide = Math.sign(drawing[otherKey] - drawing.entryPrice);
    if (changedSide === otherSide || otherSide === 0) {
      const otherDistance = Math.max(minimumDistance, Math.abs(drawing[otherKey] - drawing.entryPrice));
      drawing[otherKey] = drawing.entryPrice - changedSide * otherDistance;
    }
  }

}

function projectLineDrawings(
  chart: IChartApi | null,
  series: ISeriesApi<"Candlestick"> | null,
  drawings: LineDrawing[],
): ProjectedLine[] {
  if (!chart || !series) return [];
  return drawings.flatMap((drawing, index) => {
    const x1 = chart.timeScale().logicalToCoordinate(drawing.start.logical);
    const y1 = series.priceToCoordinate(drawing.start.price);
    const x2 = chart.timeScale().logicalToCoordinate(drawing.end.logical);
    const y2 = series.priceToCoordinate(drawing.end.price);
    return x1 === null || y1 === null || x2 === null || y2 === null
      ? []
      : [{ id: `line-${index}`, x1: Number(x1), y1: Number(y1), x2: Number(x2), y2: Number(y2) }];
  });
}

function projectPositionDrawings(
  chart: IChartApi | null,
  series: ISeriesApi<"Candlestick"> | null,
  drawings: PositionDrawing[],
): ProjectedPosition[] {
  if (!chart || !series) return [];
  return drawings.flatMap((drawing, index) => {
    const startX = chart.timeScale().logicalToCoordinate(drawing.startLogical);
    const endX = chart.timeScale().logicalToCoordinate(drawing.endLogical);
    const topY = series.priceToCoordinate(Math.max(drawing.outerAPrice, drawing.outerBPrice));
    const entryY = series.priceToCoordinate(drawing.entryPrice);
    const bottomY = series.priceToCoordinate(Math.min(drawing.outerAPrice, drawing.outerBPrice));
    if (startX === null || endX === null || topY === null || entryY === null || bottomY === null) return [];
    const left = Math.min(Number(startX), Number(endX));
    const right = Math.max(Number(startX), Number(endX));
    const top = Number(topY);
    const entry = Number(entryY);
    const bottom = Number(bottomY);
    return [{
      id: `position-${index}`,
      left,
      right,
      width: Math.max(1, right - left),
      top,
      entryY: entry,
      bottom,
      profitHeight: Math.max(0, entry - top),
      riskHeight: Math.max(0, bottom - entry),
      tpPrice: Math.max(drawing.outerAPrice, drawing.outerBPrice),
      entryPrice: drawing.entryPrice,
      slPrice: Math.min(drawing.outerAPrice, drawing.outerBPrice),
      handles: [
        { x: left, y: top }, { x: right, y: top },
        { x: left, y: entry }, { x: right, y: entry },
        { x: left, y: bottom }, { x: right, y: bottom },
      ],
    }];
  });
}

function findDrawingAt(
  chart: IChartApi,
  series: ISeriesApi<"Candlestick">,
  lines: LineDrawing[],
  positions: PositionDrawing[],
  freehandPaths: string[],
  pointerX: number,
  pointerY: number,
): { kind: "line" | "position" | "freehand"; index: number } | null {
  const tolerance = 10;
  for (let index = positions.length - 1; index >= 0; index -= 1) {
    const drawing = positions[index];
    const startX = chart.timeScale().logicalToCoordinate(drawing.startLogical);
    const endX = chart.timeScale().logicalToCoordinate(drawing.endLogical);
    const topY = series.priceToCoordinate(Math.max(drawing.outerAPrice, drawing.outerBPrice));
    const bottomY = series.priceToCoordinate(Math.min(drawing.outerAPrice, drawing.outerBPrice));
    if (startX === null || endX === null || topY === null || bottomY === null) continue;
    const left = Math.min(Number(startX), Number(endX));
    const right = Math.max(Number(startX), Number(endX));
    const top = Math.min(Number(topY), Number(bottomY));
    const bottom = Math.max(Number(topY), Number(bottomY));
    if (pointerX >= left - tolerance && pointerX <= right + tolerance
        && pointerY >= top - tolerance && pointerY <= bottom + tolerance) {
      return { kind: "position", index };
    }
  }

  for (let index = lines.length - 1; index >= 0; index -= 1) {
    const drawing = lines[index];
    const x1 = chart.timeScale().logicalToCoordinate(drawing.start.logical);
    const y1 = series.priceToCoordinate(drawing.start.price);
    const x2 = chart.timeScale().logicalToCoordinate(drawing.end.logical);
    const y2 = series.priceToCoordinate(drawing.end.price);
    if (x1 === null || y1 === null || x2 === null || y2 === null) continue;
    if (distanceToSegment(pointerX, pointerY, Number(x1), Number(y1), Number(x2), Number(y2)) <= tolerance) {
      return { kind: "line", index };
    }
  }

  for (let index = freehandPaths.length - 1; index >= 0; index -= 1) {
    const values = [...freehandPaths[index].matchAll(/-?\d+(?:\.\d+)?/g)].map((match) => Number(match[0]));
    for (let offset = 0; offset + 3 < values.length; offset += 2) {
      if (distanceToSegment(pointerX, pointerY, values[offset], values[offset + 1], values[offset + 2], values[offset + 3]) <= tolerance) {
        return { kind: "freehand", index };
      }
    }
  }
  return null;
}

function distanceToSegment(
  pointX: number,
  pointY: number,
  startX: number,
  startY: number,
  endX: number,
  endY: number,
) {
  const deltaX = endX - startX;
  const deltaY = endY - startY;
  if (deltaX === 0 && deltaY === 0) return Math.hypot(pointX - startX, pointY - startY);
  const projection = Math.max(0, Math.min(1, (((pointX - startX) * deltaX) + ((pointY - startY) * deltaY)) / ((deltaX ** 2) + (deltaY ** 2))));
  return Math.hypot(pointX - (startX + (projection * deltaX)), pointY - (startY + (projection * deltaY)));
}

function freehandPath(points: Array<[number, number]>) {
  return points.map(([x, y], index) => `${index === 0 ? "M" : "L"}${x.toFixed(1)} ${y.toFixed(1)}`).join(" ");
}
