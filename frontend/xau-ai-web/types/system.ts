export type ConnectionState = "checking" | "online" | "offline";

export interface SystemStatus {
  service: string;
  status: string;
  checkedAtUtc: string;
}
