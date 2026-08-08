import axios, { InternalAxiosRequestConfig } from 'axios';
import type { ApiResponse, AuthResponse } from '../types';

const baseURL = import.meta.env.VITE_API_BASE_URL || '/api';

const api = axios.create({
  baseURL,
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
});

type RetryableRequest = InternalAxiosRequestConfig & { _retry?: boolean };
let accessToken: string | null = null;
let refreshInFlight: Promise<AuthResponse> | null = null;

export const setAccessToken = (token: string | null) => {
  accessToken = token;
};

const clearAuthSession = () => {
  accessToken = null;
  window.dispatchEvent(new Event('carepoint:auth-cleared'));
};

const rotateSession = async (): Promise<AuthResponse> => {
  const { data } = await axios.post<ApiResponse<AuthResponse>>(
    `${baseURL}/auth/refresh-token`,
    undefined,
    { withCredentials: true },
  );
  setAccessToken(data.data.accessToken);
  return data.data;
};

export const requestSessionRefresh = (): Promise<AuthResponse> => {
  if (!refreshInFlight) {
    refreshInFlight = (navigator.locks
      ? navigator.locks.request('carepoint-refresh-session', rotateSession)
      : rotateSession())
      .finally(() => {
        refreshInFlight = null;
      });
  }
  return refreshInFlight;
};

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  if (accessToken) config.headers.Authorization = `Bearer ${accessToken}`;
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config as RetryableRequest | undefined;
    if (!original) return Promise.reject(error);

    if (error.response?.status === 401 && !original._retry) {
      original._retry = true;
      try {
        const session = await requestSessionRefresh();
        original.headers.Authorization = `Bearer ${session.accessToken}`;
        return api(original);
      } catch {
        clearAuthSession();
        if (window.location.pathname !== '/login') window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  },
);

export default api;
