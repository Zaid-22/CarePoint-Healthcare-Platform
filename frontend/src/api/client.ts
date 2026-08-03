import axios, { InternalAxiosRequestConfig } from 'axios';
import type { ApiResponse, AuthResponse } from '../types';

const baseURL = import.meta.env.VITE_API_BASE_URL || '/api';

const api = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
});

type RetryableRequest = InternalAxiosRequestConfig & { _retry?: boolean };
let refreshInFlight: Promise<string> | null = null;

const clearStoredAuth = () => {
  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');
  window.dispatchEvent(new Event('carepoint:auth-cleared'));
};

const rotateRefreshToken = async (expectedRefreshToken: string): Promise<string> => {
  const currentRefreshToken = localStorage.getItem('refreshToken');
  const currentAccessToken = localStorage.getItem('accessToken');
  if (currentRefreshToken && currentRefreshToken !== expectedRefreshToken && currentAccessToken) {
    return currentAccessToken;
  }

  const { data } = await axios.post<ApiResponse<AuthResponse>>(
    `${baseURL}/auth/refresh-token`,
    { refreshToken: expectedRefreshToken },
  );
  localStorage.setItem('accessToken', data.data.accessToken);
  localStorage.setItem('refreshToken', data.data.refreshToken);
  return data.data.accessToken;
};

const refreshAccessToken = (refreshToken: string): Promise<string> => {
  if (!refreshInFlight) {
    refreshInFlight = (navigator.locks
      ? navigator.locks.request('carepoint-refresh-token', () => rotateRefreshToken(refreshToken))
      : rotateRefreshToken(refreshToken))
      .finally(() => {
        refreshInFlight = null;
      });
  }

  return refreshInFlight;
};

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem('accessToken');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config as RetryableRequest | undefined;
    if (!original) return Promise.reject(error);

    if (error.response?.status === 401 && !original._retry) {
      original._retry = true;
      const refreshToken = localStorage.getItem('refreshToken');
      if (refreshToken) {
        try {
          const accessToken = await refreshAccessToken(refreshToken);
          original.headers.Authorization = `Bearer ${accessToken}`;
          return api(original);
        } catch {
          // Another browser tab may have rotated the token while this request was in flight.
          const currentRefreshToken = localStorage.getItem('refreshToken');
          const currentAccessToken = localStorage.getItem('accessToken');
          if (currentRefreshToken && currentRefreshToken !== refreshToken && currentAccessToken) {
            original.headers.Authorization = `Bearer ${currentAccessToken}`;
            return api(original);
          }

          clearStoredAuth();
          window.location.href = '/login';
        }
      } else {
        clearStoredAuth();
        if (window.location.pathname !== '/login') window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

export default api;
