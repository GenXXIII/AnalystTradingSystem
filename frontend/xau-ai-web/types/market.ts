export type MarketProviderState =
  | "Disabled"
  | "Available"
  | "Connected"
  | "Disconnected"
  | "ConfigurationError"
  | "AuthenticationFailed"
  | "ConnectionFailed";

export interface MarketProviderStatus {
  provider: string;
  state: MarketProviderState;
  enabled: boolean;
  connected: boolean;
  applicationSymbol: string;
  providerSymbol: string;
  server: string | null;
  message: string;
  checkedAtUtc: string;
}

export interface MarketQuote {
  symbol: string;
  providerSymbol: string;
  bid: number;
  ask: number;
  timestampUtc: string;
}

export interface MarketCandle {
  symbol: string;
  providerSymbol: string;
  timeframe: string;
  openTimeUtc: string;
  closeTimeUtc: string;
  open: number;
  high: number;
  low: number;
  close: number;
  tickVolume: number | null;
  realVolume: number | null;
  spread: number | null;
  isComplete: boolean;
  sourceTimeZone: string;
  fetchedAtUtc: string;
}

export interface StoredMarketCandle {
  symbol: string;
  providerSymbol: string;
  timeframe: string;
  openTimeUtc: string;
  closeTimeUtc: string;
  open: number;
  high: number;
  low: number;
  close: number;
  tickVolume: number | null;
  realVolume: number | null;
  spread: number | null;
  isComplete: boolean;
  fetchedAtUtc: string;
}

export interface MarketDataAvailability {
  symbol: string;
  timeframe: string;
  storedCandles: number;
  oldestCandleOpenTimeUtc: string | null;
  latestCandleOpenTimeUtc: string | null;
  lastCompletedCandleOpenTimeUtc: string | null;
}

export interface MarketDataSyncStatus {
  symbol: string;
  timeframe: string;
  status: string;
  lastAttemptAtUtc: string | null;
  lastSuccessfulSyncAtUtc: string | null;
  lastRequestedFromUtc: string | null;
  lastRequestedToUtc: string | null;
  lastStoredCandleOpenTimeUtc: string | null;
  consecutiveFailures: number;
  detectedGapCount: number;
  lastErrorCode: string | null;
  lastErrorMessage: string | null;
}

export interface MarketDataPipelineStatus {
  availability: MarketDataAvailability;
  synchronization: MarketDataSyncStatus | null;
}

export interface MarketDataPipelineResult {
  runId: string;
  symbol: string;
  timeframe: string;
  requestedFromUtc: string;
  requestedToUtc: string;
  lastStoredCandleOpenTimeUtc: string | null;
  batchesProcessed: number;
  received: number;
  accepted: number;
  inserted: number;
  updated: number;
  skipped: number;
  rejected: number;
  detectedGaps: number;
  durationMilliseconds: number;
}

export interface MarketDataSourceComparison {
  symbol: string;
  timeframe: string;
  primaryProviderKey: string;
  referenceProviderKey: string;
  referenceEnabled: boolean;
  requestedLimit: number;
  matchedCandles: number;
  latestComparedOpenTimeUtc: string | null;
  latestPrimaryClose: number | null;
  latestReferenceClose: number | null;
  latestDeviationBps: number | null;
  maximumDeviationBps: number | null;
  toleranceBps: number;
  isWithinTolerance: boolean | null;
}
