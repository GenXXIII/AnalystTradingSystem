import type { MarketTimeframeCode } from "@/features/market/api/get-pipeline-data";

export type AnalysisReadiness = "InsufficientData" | "WarmingUp" | "Ready";
export type AnalyticalDirection = "Bullish" | "Bearish" | "Neutral" | "Transition" | "Conflicting";
export type IndicatorZone = "Unavailable" | "Oversold" | "Neutral" | "Overbought";
export type VolatilityRegime = "Unavailable" | "VeryLow" | "Low" | "Normal" | "High" | "VeryHigh";

export interface IndicatorValue {
  name: string;
  period: number;
  value: number | null;
  previousValue: number | null;
  readiness: AnalysisReadiness;
}

export interface RsiAnalysis {
  period: number;
  value: number | null;
  previousValue: number | null;
  zone: IndicatorZone;
  momentum: AnalyticalDirection;
  readiness: AnalysisReadiness;
}

export interface MacdAnalysis {
  fastPeriod: number;
  slowPeriod: number;
  signalPeriod: number;
  macd: number | null;
  signal: number | null;
  histogram: number | null;
  previousHistogram: number | null;
  crossover: "None" | "Bullish" | "Bearish";
  momentum: AnalyticalDirection;
  readiness: AnalysisReadiness;
}

export interface TrendAnalysis {
  direction: AnalyticalDirection;
  strength: string;
  emaAlignment: string;
  priceVsEma: string;
  sma: IndicatorValue[];
  ema: IndicatorValue[];
}

export interface MomentumAnalysis {
  rsi: RsiAnalysis;
  macd: MacdAnalysis;
  stochastic: {
    kPeriod: number;
    dPeriod: number;
    percentK: number | null;
    percentD: number | null;
    crossover: "None" | "Bullish" | "Bearish";
    zone: IndicatorZone;
    readiness: AnalysisReadiness;
  };
}

export interface VolatilityAnalysis {
  regime: VolatilityRegime;
  atrRelativeToBaseline: number | null;
  currentRangeRelativeToAverage: number;
  atr: { period: number; value: number | null; previousValue: number | null; readiness: AnalysisReadiness };
  bollingerBands: {
    period: number;
    standardDeviations: number;
    middle: number | null;
    upper: number | null;
    lower: number | null;
    width: number | null;
    percentB: number | null;
    readiness: AnalysisReadiness;
  };
  evidence: string[];
}

export interface IndicatorSet {
  sma: IndicatorValue[];
  ema: IndicatorValue[];
  rsi: RsiAnalysis;
  macd: MacdAnalysis;
  atr: VolatilityAnalysis["atr"];
  adx: {
    period: number;
    adx: number | null;
    plusDi: number | null;
    minusDi: number | null;
    strength: string;
    directionalBias: AnalyticalDirection;
    readiness: AnalysisReadiness;
  };
  bollingerBands: VolatilityAnalysis["bollingerBands"];
  stochastic: MomentumAnalysis["stochastic"];
}

export interface StrategySetup {
  family: string;
  strategy: string;
  direction: AnalyticalDirection;
  state: string;
  quality: number;
  evidence: string[];
}

export interface StrategyAnalysis {
  setups: StrategySetup[];
  flow: {
    dataMethod: string;
    trueBidAskDeltaAvailable: boolean;
    tickVolumeRatio: number | null;
    directionalPressure: number | null;
    direction: AnalyticalDirection;
    divergence: string;
    absorptionCandidate: boolean;
    exhaustionCandidate: boolean;
    breakoutConfirmed: boolean;
    evidence: string[];
  };
  ktr: {
    openingPrice: number;
    unit: number;
    method: string;
    levels: Array<{ label: string; price: number; multiple: number }>;
  };
  coveredFamilies: string[];
  unavailableCapabilities: string[];
}

export interface TechnicalAnalysis {
  symbol: string;
  timeframe: MarketTimeframeCode;
  analyzedAtUtc: string;
  lastCandleCloseTimeUtc: string | null;
  trend: TrendAnalysis;
  momentum: MomentumAnalysis;
  volatility: VolatilityAnalysis;
  marketStructure: {
    direction: AnalyticalDirection;
    structure: string;
    swings: Array<{ kind: string; classification: string; candleTimeUtc: string; price: number; candleIndex: number }>;
    readiness: AnalysisReadiness;
  };
  supportResistance: Array<{
    lower: number;
    upper: number;
    center: number;
    type: "Support" | "Resistance" | "Pivot";
    strength: string;
    touches: number;
    timeframes: MarketTimeframeCode[];
    distanceFromPricePercent: number;
  }>;
  candlestickPatterns: Array<{
    pattern: string;
    direction: "Bullish" | "Bearish" | "Neutral";
    timeframe: MarketTimeframeCode;
    candleTimeUtc: string;
    quality: number;
    supportingConditions: string[];
  }>;
  indicators: IndicatorSet;
  strategies: StrategyAnalysis | null;
  conflicts: string[];
  diagnostics: {
    durationMilliseconds: number;
    candlesRead: number;
    candlesUsed: number;
    duplicateCandlesIgnored: number;
    invalidCandlesIgnored: number;
    missingIntervalCount: number;
    indicatorsCalculated: number;
    insufficientIndicators: number;
    dataCutoffUtc: string;
  };
}

export interface MultiTimeframeAnalysis {
  symbol: string;
  analyzedAtUtc: string;
  timeframes: Array<{
    timeframe: MarketTimeframeCode;
    trend: AnalyticalDirection;
    structure: AnalyticalDirection;
    momentum: AnalyticalDirection;
    volatility: VolatilityRegime;
    lastCandleCloseTimeUtc: string | null;
    candlesUsed: number;
  }>;
  trendAlignment: string;
  agreements: string[];
  conflicts: string[];
  durationMilliseconds: number;
}
