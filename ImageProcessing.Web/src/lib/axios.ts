import axios, { AxiosHeaders } from "axios";
import type { InternalAxiosRequestConfig, AxiosRequestConfig } from "axios";
import { useAuthStore } from "../features/auth/authStore";
import { toastError } from "@/lib/toast-bridge";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE,
  withCredentials: false,
});

// --- Request: attach Authorization header safely ---
api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    // ensure headers is AxiosHeaders, then set
    if (!config.headers || !(config.headers instanceof AxiosHeaders)) {
      config.headers = new AxiosHeaders(config.headers);
    }
    (config.headers as AxiosHeaders).set("Authorization", `Bearer ${token}`);
  }
  return config;
});

let refreshing: Promise<void> | null = null;

// --- Response: handle 401 refresh, plus toasts ---
api.interceptors.response.use(
  (r) => r,
  async (error) => {
    const response = error?.response;
    const original = error?.config as
      | (AxiosRequestConfig & { _retry?: boolean })
      | undefined;

    if (!response) {
      // network/CORS/offline
      toastError("Cannot reach the server. Please check your connection.");
      throw error;
    }

    // try refresh exactly once
    if (response.status === 401 && original && !original._retry) {
      original._retry = true;
      const store = useAuthStore.getState();
      try {
        if (!refreshing) {
          refreshing = store.refresh().finally(() => (refreshing = null));
        }
        await refreshing;

        const token = useAuthStore.getState().accessToken;
        if (token) {
          if (
            !original.headers ||
            !((original.headers as any) instanceof AxiosHeaders)
          ) {
            original.headers = new AxiosHeaders(original.headers as any);
          }
          (original.headers as AxiosHeaders).set(
            "Authorization",
            `Bearer ${token}`
          );
          return api(original);
        }
      } catch {
        // fall through to toasts below
      }
    }

    // toasts for error statuses
    if (response.status === 401) {
      toastError("Unauthorized. Please sign in to continue.");
    } else if (response.status === 403) {
      toastError("You don’t have permission to perform this action.");
    } else if (response.status >= 500) {
      toastError("A server error occurred. Please try again later.");
    } else {
      const msg =
        response.data?.message ||
        response.data?.title ||
        error.message ||
        "Request failed.";
      toastError(msg);
    }

    throw error;
  }
);

export default api;
