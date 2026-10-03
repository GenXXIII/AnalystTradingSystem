"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { getLatestNews, getNewsStatus } from "@/features/news/api/get-news";
import { ApiClientError } from "@/lib/api/errors";
import type { NewsSystemStatus, PagedNewsArticles } from "@/types/news";

export function useNewsModule(enabled: boolean) {
  const [articles, setArticles] = useState<PagedNewsArticles | null>(null);
  const [status, setStatus] = useState<NewsSystemStatus | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const requestSequence = useRef(0);

  const refresh = useCallback(async () => {
    const sequence = ++requestSequence.current;
    setLoading(true);
    setError(null);
    try {
      const [latest, systemStatus] = await Promise.all([getLatestNews(), getNewsStatus()]);
      if (sequence !== requestSequence.current) return;
      setArticles(latest);
      setStatus(systemStatus);
    } catch (reason) {
      if (sequence !== requestSequence.current) return;
      setError(reason instanceof ApiClientError ? reason.message : "News is currently unavailable.");
    } finally {
      if (sequence === requestSequence.current) setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!enabled) return;
    const timer = window.setTimeout(() => void refresh(), 0);
    return () => window.clearTimeout(timer);
  }, [enabled, refresh]);

  return { articles, status, loading, error, refresh };
}
