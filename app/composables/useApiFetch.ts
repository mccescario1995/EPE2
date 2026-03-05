// composables/useApiFetch.ts
import { accessToken, useAuth } from "./useAuth";
import { showToast } from "~/stores/globalvar";

/**
 * Prevent multiple refresh calls at the same time.
 * If 5 requests get 401 → only ONE refresh runs.
 */
let refreshPromise: Promise<string | null> | null = null;

/**
 * Universal API Fetch (Nuxt 3)
 *
 * FEATURES:
 * - JSON + FormData support
 * - Access token auto-attach
 * - Token refresh w/ queue
 * - Safe retry logic
 * - Optional auto JSON parsing
 * - Centralized error handling
 * - SSR safe
 */
export async function apiFetch<T = any>(
  input: RequestInfo,
  init: RequestInit & {
    /**
     * Optional flags (ALL OPTIONAL)
     */
    parseJson?: boolean; // auto parse JSON response
    throwOnError?: boolean; // throw if response not ok
  } = {},
): Promise<Response | T> {
  const auth = useAuth();
  const config = useRuntimeConfig();

  /* ------------------------------------------------------------------
   * 1. Resolve Base URL (SSR safe)
   * ------------------------------------------------------------------ */
  const baseURL = import.meta.env.PROD
    ? config.public.liveApi
    : config.public.localApi;

  /* ------------------------------------------------------------------
   * 2. Build headers safely
   * ------------------------------------------------------------------ */
  const buildHeaders = (token?: string) => {
    const headers = new Headers(init.headers);

    if (token) {
      headers.set("Token", `${token}`);
    }

    // Optional but recommended
    headers.set("Accept", "application/json");

    /**
     * ⚠️ NEVER set Content-Type here
     * - JSON sets it manually
     * - FormData must NOT set it
     */

    return headers;
  };

  /* ------------------------------------------------------------------
   * 3. Core fetch executor
   * ------------------------------------------------------------------ */
  const executeFetch = async (token?: string) => {
    return fetch(`${baseURL}${input}`, {
      ...init,
      headers: buildHeaders(token),
      // credentials: 'include'
    });
  };

  /* ------------------------------------------------------------------
   * 4. Initial request
   * ------------------------------------------------------------------ */
  let response = await executeFetch(accessToken.value ?? undefined);

  /* ------------------------------------------------------------------
   * 5. Exit early if authorized
   * ------------------------------------------------------------------ */
  if (response.status !== 401) {
    return handleResponse<T>(response, init);
  }

  /* ------------------------------------------------------------------
   * 6. Retry safety (avoid double POST / upload)
   * ------------------------------------------------------------------ */
  const method = (init.method ?? "GET").toUpperCase();
  const canRetry = method === "GET";

  if (!canRetry) {
    auth.logout();
    showToast({
      title: "Unauthorized",
      message:
        response.status === 401
          ? "Session Expired. Please log in again."
          : "Access Denied. Please check your credentials.",
      type: "error",
    });

    throw new Error("Unauthorized");
  }

  /* ------------------------------------------------------------------
   * 7. Refresh token (queued)
   * ------------------------------------------------------------------ */
  try {
    if (!refreshPromise) {
      refreshPromise = auth.refresh().finally(() => {
        refreshPromise = null;
      });
    }

    const newToken = await refreshPromise;

    response = await executeFetch(newToken ?? undefined);
    return handleResponse<T>(response, init);
  } catch (err) {
    auth.logout();
    showToast({
      title: "Unauthorized",
      message: `Error: ${err instanceof Error ? err.message : "Unknown error"}`,
      type: "error",
      animation: "bounce",
    });
    throw err;
  }
}

/* ======================================================================
 * RESPONSE HANDLER (Optional upgrades live here)
 * ====================================================================== */
async function handleResponse<T>(
  response: Response,
  init: {
    parseJson?: boolean;
    throwOnError?: boolean;
  },
): Promise<Response | T> {
  /* --------------------------------------------------------------
   * A. Global error handling
   * -------------------------------------------------------------- */
  if (init.throwOnError && !response.ok) {
    let errorBody: any = null;

    try {
      errorBody = await response.json();
    } catch {
      errorBody = await response.text();
    }

    throw {
      status: response.status,
      body: errorBody,
    };
  }

  /* --------------------------------------------------------------
   * B. Auto JSON parsing
   * -------------------------------------------------------------- */
  if (init.parseJson) {
    // Empty responses (204)
    if (response.status === 204) {
      return null as T;
    }

    return (await response.json()) as T;
  }

  /* --------------------------------------------------------------
   * C. Default: return raw Response
   * -------------------------------------------------------------- */
  return response;
}
