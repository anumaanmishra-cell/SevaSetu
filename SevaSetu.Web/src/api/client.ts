import axios, { type AxiosError } from "axios";
import type { AuthResponse } from "./types";

const baseURL = `${import.meta.env.VITE_API_URL ?? "http://localhost:5300"}/api/v1`;

export const api = axios.create({ baseURL });

api.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  if (typeof FormData !== "undefined" && config.data instanceof FormData) {
    delete config.headers["Content-Type"];
  }
  return config;
});

let refreshing: Promise<string | null> | null = null;

api.interceptors.response.use(
  (r) => r,
  async (error: AxiosError) => {
    const original = error.config;
    const url = original?.url ?? "";
    if (
      error.response?.status !== 401 ||
      !original ||
      (original as { _retry?: boolean })._retry ||
      url.includes("/auth/login") ||
      url.includes("/auth/logout") ||
      url.includes("/auth/refresh")
    ) {
      const message = (error.response?.data as { error?: string })?.error ?? error.message;
      return Promise.reject(new Error(message));
    }
    (original as { _retry?: boolean })._retry = true;
    refreshing ??= refreshAccess().finally(() => {
      refreshing = null;
    });
    const token = await refreshing;
    if (!token) {
      localStorage.clear();
      window.location.href = "/login";
      return Promise.reject(error);
    }
    original.headers.Authorization = `Bearer ${token}`;
    return api(original);
  }
);

async function refreshAccess(): Promise<string | null> {
  const refreshToken = localStorage.getItem("refreshToken");
  if (!refreshToken) return null;
  const { data } = await axios.post<AuthResponse>(`${baseURL}/auth/refresh`, { refreshToken });
  localStorage.setItem("accessToken", data.accessToken);
  localStorage.setItem("refreshToken", data.refreshToken);
  return data.accessToken;
}
