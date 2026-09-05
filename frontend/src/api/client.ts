import axios, { type InternalAxiosRequestConfig } from 'axios';
import type { ApiResponse, AuthResponse } from '../types';
import { API_BASE_URL } from '../utils/apiAssets';

const baseURL = API_BASE_URL;
const SESSION_CHANGE_KEY = 'carepoint:session-change';
const api = axios.create({
  baseURL,
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
});

type SessionRequest = InternalAxiosRequestConfig & {
  _retry?: boolean;
  _sessionVersion?: number;
  _sessionUserId?: string | null;
};
let accessToken: string | null = null;
let sessionUserId: string | null = null;
let sessionVersion = 0;
let refreshInFlight: Promise<AuthResponse> | null = null;

const sessionChangedError = () => new Error('Your account session changed. Sign in again before continuing.');

const broadcastSessionChange = () => {
  // Only an invalidation marker is shared; credentials stay in memory / HttpOnly cookies.
  try {
    localStorage.setItem(SESSION_CHANGE_KEY, `${Date.now()}:${Math.random()}`);
  } catch {
    // Identity checks still prevent cross-account retries if storage is unavailable.
  }
};

export const setAuthSession = (session: AuthResponse, broadcast = false) => {
  if (sessionUserId !== session.userId || broadcast) sessionVersion += 1;
  sessionUserId = session.userId;
  accessToken = session.accessToken;
  if (broadcast) broadcastSessionChange();
};

export const clearAuthSession = (broadcast = false) => {
  sessionVersion += 1;
  accessToken = null;
  sessionUserId = null;
  window.dispatchEvent(new Event('carepoint:auth-cleared'));
  if (broadcast) broadcastSessionChange();
};

window.addEventListener('storage', (event) => {
  if (event.key === SESSION_CHANGE_KEY) clearAuthSession();
});

const rotateSession = async (): Promise<AuthResponse> => {
  const expectedVersion = sessionVersion;
  const expectedUserId = sessionUserId;
  const { data } = await axios.post<ApiResponse<AuthResponse>>(
    `${baseURL}/auth/refresh-token`, undefined, { withCredentials: true },
  );
  if (expectedVersion !== sessionVersion) throw sessionChangedError();
  if (expectedUserId !== null && data.data.userId !== expectedUserId) {
    clearAuthSession();
    throw sessionChangedError();
  }
  setAuthSession(data.data);
  return data.data;
};

export const requestSessionRefresh = (): Promise<AuthResponse> => {
  if (!refreshInFlight) {
    // Capture the version before waiting for another tab's refresh lock, too.
    const expectedVersion = sessionVersion;
    const refresh = () => {
      if (expectedVersion !== sessionVersion) throw sessionChangedError();
      return rotateSession();
    };
    refreshInFlight = (navigator.locks
      ? navigator.locks.request('carepoint-refresh-session', refresh)
      : refresh())
      .finally(() => { refreshInFlight = null; });
  }
  return refreshInFlight;
};

const isCurrentSession = (request: SessionRequest) =>
  request._sessionVersion === sessionVersion && request._sessionUserId === sessionUserId;

api.interceptors.request.use((config: SessionRequest) => {
  if (config._sessionVersion === undefined) {
    config._sessionVersion = sessionVersion;
    config._sessionUserId = sessionUserId;
  } else if (!isCurrentSession(config)) {
    throw sessionChangedError();
  }
  if (accessToken) config.headers.Authorization = `Bearer ${accessToken}`;
  return config;
});

api.interceptors.response.use(
  (response) => {
    if (!isCurrentSession(response.config as SessionRequest)) throw sessionChangedError();
    return response;
  },
  async (error) => {
    const original = error.config as SessionRequest | undefined;
    if (!original) return Promise.reject(error);
    if (!isCurrentSession(original)) return Promise.reject(sessionChangedError());

    if (error.response?.status === 401 && !original._retry && original._sessionUserId) {
      original._retry = true;
      try {
        const session = await requestSessionRefresh();
        if (!isCurrentSession(original) || session.userId !== original._sessionUserId)
          return Promise.reject(sessionChangedError());
        original.headers.Authorization = `Bearer ${session.accessToken}`;
        return api(original);
      } catch (refreshError) {
        // Do not clear a newer session established while this request was pending.
        if (isCurrentSession(original)) clearAuthSession();
        return Promise.reject(refreshError);
      }
    }
    return Promise.reject(error);
  },
);

export default api;
