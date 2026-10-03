"use client";

import { useCallback, useEffect, useState } from "react";
import { getSystemStatus } from "@/features/system/api/get-system-status";
import { ApiClientError } from "@/lib/api/errors";
import type { ConnectionState, SystemStatus } from "@/types/system";

export function useApiStatus() {
  const [state, setState] = useState<ConnectionState>("checking");
  const [status, setStatus] = useState<SystemStatus | null>(null);
  const [error, setError] = useState<string | null>(null);

  const connect = useCallback(async () => {
    try {
      const result = await getSystemStatus();
      setStatus(result);
      setError(null);
      setState("online");
    } catch (reason: unknown) {
      setStatus(null);
      setState("offline");
      setError(
        reason instanceof ApiClientError
          ? reason.message
          : "The API status could not be checked.",
      );
    }
  }, []);

  useEffect(() => {
    let active = true;

    void getSystemStatus().then(
      (result) => {
        if (active) {
          setStatus(result);
          setError(null);
          setState("online");
        }
      },
      (reason: unknown) => {
        if (active) {
          setStatus(null);
          setState("offline");
          setError(toErrorMessage(reason));
        }
      },
    );

    return () => {
      active = false;
    };
  }, []);

  const refresh = useCallback(() => {
    setState("checking");
    setError(null);
    void connect();
  }, [connect]);

  return { state, status, error, refresh };
}

function toErrorMessage(reason: unknown): string {
  return reason instanceof ApiClientError
    ? reason.message
    : "The API status could not be checked.";
}
