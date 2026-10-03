const defaultApiUrl = "http://localhost:5081";

function normalizeApiUrl(value: string): string {
  const url = new URL(value);

  if (url.protocol !== "http:" && url.protocol !== "https:") {
    throw new Error("NEXT_PUBLIC_API_URL must use http or https.");
  }

  return url.toString().replace(/\/$/, "");
}

export const environment = Object.freeze({
  apiBaseUrl: normalizeApiUrl(process.env.NEXT_PUBLIC_API_URL ?? defaultApiUrl),
});
