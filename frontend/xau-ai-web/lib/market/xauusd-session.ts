export type XauUsdMarketSession = {
  isOpen: boolean;
  nextTransitionAtUtc: Date;
  nextTransitionLabel: string;
  nextTransitionLocalLabel: string;
  weeklyOpenLabel: string;
  weeklyCloseLabel: string;
  nextTradingSessionLabel: string;
  season: "summer" | "winter";
};

export type TradingSessionName = "Sydney" | "Tokyo" | "London" | "New York";

const tradingSessions: ReadonlyArray<{
  name: TradingSessionName;
  timeZone: string;
  opensAtHour: number;
  closesAtHour: number;
}> = [
  { name: "Sydney", timeZone: "Australia/Sydney", opensAtHour: 8, closesAtHour: 17 },
  { name: "Tokyo", timeZone: "Asia/Tokyo", opensAtHour: 9, closesAtHour: 18 },
  { name: "London", timeZone: "Europe/London", opensAtHour: 8, closesAtHour: 17 },
  { name: "New York", timeZone: "America/New_York", opensAtHour: 8, closesAtHour: 17 },
];

const localTransitionFormatter = new Intl.DateTimeFormat("en-GB", {
  weekday: "short",
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
  timeZone: "Asia/Bangkok",
});
const nextSessionFormatter = new Intl.DateTimeFormat("en-GB", {
  weekday: "short",
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
  timeZone: "Asia/Bangkok",
});

let nextSessionCacheMinute = -1;
let nextSessionCacheValue = "Next session checking";

export function getXauUsdMarketSession(now: Date): XauUsdMarketSession {
  const summer = isUsDaylightSavingSeason(now);
  const day = now.getUTCDay();
  const minutes = now.getUTCHours() * 60 + now.getUTCMinutes();
  const sundayOpen = summer ? 22 * 60 + 5 : 23 * 60 + 5;
  const dailyClose = summer ? 20 * 60 + 58 : 21 * 60 + 58;
  const dailyOpen = summer ? 22 * 60 + 2 : 23 * 60 + 2;
  let isOpen = false;
  let transition: Date;

  if (day === 0) {
    isOpen = minutes >= sundayOpen;
    transition = isOpen
      ? atUtcTime(now, 1, dailyClose)
      : atUtcTime(now, 0, sundayOpen);
  } else if (day >= 1 && day <= 4) {
    isOpen = minutes < dailyClose || minutes >= dailyOpen;
    if (minutes < dailyClose) {
      transition = atUtcTime(now, 0, dailyClose);
    } else if (minutes < dailyOpen) {
      transition = atUtcTime(now, 0, dailyOpen);
    } else {
      transition = atUtcTime(now, 1, dailyClose);
    }
  } else if (day === 5) {
    isOpen = minutes < dailyClose;
    transition = isOpen
      ? atUtcTime(now, 0, dailyClose)
      : atUtcTime(now, 2, sundayOpen);
  } else {
    transition = atUtcTime(now, 1, sundayOpen);
  }

  const action = isOpen ? "closes" : "opens";
  return {
    isOpen,
    nextTransitionAtUtc: transition,
    nextTransitionLabel: `${action} ${localTransitionFormatter.format(transition)} UTC+7`,
    nextTransitionLocalLabel: `${action} ${localTransitionFormatter.format(transition)} UTC+7`,
    weeklyOpenLabel: `Open Mon ${summer ? "05:05" : "06:05"} UTC+7`,
    weeklyCloseLabel: `Close Sat ${summer ? "03:58" : "04:58"} UTC+7`,
    nextTradingSessionLabel: getNextTradingSessionLabel(now),
    season: summer ? "summer" : "winter",
  };
}

export function formatSessionCountdown(now: Date, transition: Date) {
  const totalMinutes = Math.max(0, Math.ceil((transition.getTime() - now.getTime()) / 60_000));
  const days = Math.floor(totalMinutes / 1_440);
  const hours = Math.floor((totalMinutes % 1_440) / 60);
  const minutes = totalMinutes % 60;
  if (days > 0) return `${days}d ${hours}h`;
  if (hours > 0) return `${hours}h ${minutes}m`;
  return `${minutes}m`;
}

export function getActiveTradingSessions(now: Date): TradingSessionName[] {
  return tradingSessions
    .filter((session) => isSessionOpen(now, session.timeZone, session.opensAtHour, session.closesAtHour))
    .map((session) => session.name);
}

export function getNextTradingSessionLabel(now: Date): string {
  const minute = Math.floor(now.getTime() / 60_000);
  if (minute === nextSessionCacheMinute) return nextSessionCacheValue;

  const firstCandidate = new Date(now);
  firstCandidate.setUTCMinutes(0, 0, 0);
  firstCandidate.setUTCHours(firstCandidate.getUTCHours() + 1);

  for (let hourOffset = 0; hourOffset <= 8 * 24; hourOffset += 1) {
    const candidate = new Date(firstCandidate.getTime() + hourOffset * 3_600_000);
    const previousHour = new Date(candidate.getTime() - 3_600_000);
    const opening = tradingSessions
      .filter((session) => (
        isSessionOpen(candidate, session.timeZone, session.opensAtHour, session.closesAtHour)
        && !isSessionOpen(previousHour, session.timeZone, session.opensAtHour, session.closesAtHour)
      ))
      .map((session) => session.name);
    if (opening.length > 0) {
      nextSessionCacheMinute = minute;
      nextSessionCacheValue = `Next ${opening.join(" / ")} ${nextSessionFormatter.format(candidate)} UTC+7`;
      return nextSessionCacheValue;
    }
  }

  nextSessionCacheMinute = minute;
  nextSessionCacheValue = "Next session unavailable";
  return nextSessionCacheValue;
}

function atUtcTime(reference: Date, dayOffset: number, minutes: number) {
  return new Date(Date.UTC(
    reference.getUTCFullYear(),
    reference.getUTCMonth(),
    reference.getUTCDate() + dayOffset,
    Math.floor(minutes / 60),
    minutes % 60,
  ));
}

function isSessionOpen(now: Date, timeZone: string, opensAtHour: number, closesAtHour: number) {
  const parts = new Intl.DateTimeFormat("en-US", {
    weekday: "short",
    hour: "2-digit",
    hourCycle: "h23",
    timeZone,
  }).formatToParts(now);
  const weekday = parts.find((part) => part.type === "weekday")?.value;
  const hour = Number(parts.find((part) => part.type === "hour")?.value);
  return weekday !== "Sat"
    && weekday !== "Sun"
    && Number.isFinite(hour)
    && hour >= opensAtHour
    && hour < closesAtHour;
}

function isUsDaylightSavingSeason(now: Date) {
  const year = now.getUTCFullYear();
  const starts = nthSundayUtc(year, 2, 2);
  const ends = nthSundayUtc(year, 10, 1);
  return now >= starts && now < ends;
}

function nthSundayUtc(year: number, month: number, occurrence: number) {
  const first = new Date(Date.UTC(year, month, 1));
  const firstSunday = 1 + ((7 - first.getUTCDay()) % 7);
  return new Date(Date.UTC(year, month, firstSunday + (occurrence - 1) * 7));
}
