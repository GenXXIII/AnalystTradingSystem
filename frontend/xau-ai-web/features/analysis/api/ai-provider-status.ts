export interface AiProviderAccountStatus {
  provider: string;
  state: "Available" | "QuotaExhausted" | "Unavailable" | "Unverified";
  canGenerate: boolean;
  isFreeTier: boolean | null;
  dailyUsed: number | null;
  dailyLimit: number | null;
  dailyRemaining: number | null;
  message: string;
  checkedAtUtc: string;
}
