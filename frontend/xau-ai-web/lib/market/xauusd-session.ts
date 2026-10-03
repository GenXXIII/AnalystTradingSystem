export type XauUsdMarketSession = {
  isOpen: boolean;
  nextTransitionAtUtc: Date;
  nextTransitionLabel: string;
  nextTransitionLocalLabel: string;
  season: "summer" | "winter";
};

const transitionFormatter = new Intl.DateTimeFormat("en-GB", {
  weekday: "short",
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
  timeZone: "UTC",
});
const localTransitionFormatter = new Intl.DateTimeFormat("en-GB", {
  weekday: "short",
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
  timeZone: "Asia/Bangkok",
});

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
    nextTransitionLabel: `${action} ${transitionFormatter.format(transition)} UTC`,
    nextTransitionLocalLabel: `${action} ${localTransitionFormatter.format(transition)} UTC+7`,
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

function atUtcTime(reference: Date, dayOffset: number, minutes: number) {
  return new Date(Date.UTC(
    reference.getUTCFullYear(),
    reference.getUTCMonth(),
    reference.getUTCDate() + dayOffset,
    Math.floor(minutes / 60),
    minutes % 60,
  ));
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
