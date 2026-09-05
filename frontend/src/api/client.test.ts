// @vitest-environment jsdom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import api, { clearAuthSession, requestSessionRefresh, setAuthSession } from './client';

const deferred = <T,>() => {
  let resolve!: (value: T | PromiseLike<T>) => void;
  const promise = new Promise<T>(done => { resolve = done; });
  return { promise, resolve };
};
const session = (userId: string, accessToken = `${userId}-token`) => ({ userId, email: `${userId}@example.test`, accessToken });
const response = (config: InternalAxiosRequestConfig, data: unknown = {}) => ({ config, data, status: 200, statusText: 'OK', headers: {} });
const expired = (config: InternalAxiosRequestConfig) => new AxiosError('Expired', 'ERR_BAD_REQUEST', config, undefined, { ...response(config), status: 401 });
const adapter = api.defaults.adapter;

beforeEach(() => { clearAuthSession(); });
afterEach(() => { api.defaults.adapter = adapter; vi.restoreAllMocks(); });

describe('session-bound requests', () => {
  it('never replays an Alice write using a Bob refresh cookie', async () => {
    setAuthSession(session('Alice'));
    const send = vi.fn(async (config: InternalAxiosRequestConfig) => { throw expired(config); });
    api.defaults.adapter = send;
    vi.spyOn(axios, 'post').mockResolvedValue({ data: { data: session('Bob') } });

    await expect(api.put('/patients/me', { address: 'Alice draft' })).rejects.toThrow('session changed');
    expect(send).toHaveBeenCalledTimes(1);
  });

  it('rotates and retries normally for the same account', async () => {
    setAuthSession(session('Alice'));
    const send = vi.fn(async (config: InternalAxiosRequestConfig) => {
      if (config.headers.Authorization === 'Bearer Alice-token') throw expired(config);
      return response(config, { saved: true });
    });
    api.defaults.adapter = send;
    vi.spyOn(axios, 'post').mockResolvedValue({ data: { data: session('Alice', 'Alice-renewed') } });

    expect((await api.put('/patients/me', { address: 'Alice draft' })).data).toEqual({ saved: true });
    expect(send).toHaveBeenCalledTimes(2);
    expect(send.mock.calls[1][0].headers.Authorization).toBe('Bearer Alice-renewed');
  });

  it('discards a successful response from an account that signed out', async () => {
    setAuthSession(session('Alice'));
    let finish!: () => void;
    const started = deferred<void>();
    api.defaults.adapter = config => new Promise(resolve => {
      finish = () => resolve(response(config, { privateData: 'Alice' }));
      started.resolve(undefined);
    });
    const pending = api.get('/patients/me');
    await started.promise;
    clearAuthSession();
    setAuthSession(session('Bob'));
    finish();
    await expect(pending).rejects.toThrow('session changed');
  });

  it('invalidates the local session when another tab changes accounts', async () => {
    setAuthSession(session('Alice'));
    const cleared = vi.fn();
    window.addEventListener('carepoint:auth-cleared', cleared);
    window.dispatchEvent(new StorageEvent('storage', { key: 'carepoint:session-change', newValue: 'another-login' }));
    api.defaults.adapter = async config => response(config, { authorization: config.headers.Authorization });
    expect(cleared).toHaveBeenCalledOnce();
    expect((await api.get('/probe')).data.authorization).toBeUndefined();
    window.removeEventListener('carepoint:auth-cleared', cleared);
  });

  it('cannot restore a session after logout while refresh is in flight', async () => {
    setAuthSession(session('Alice'));
    const refresh = deferred<{ data: { data: ReturnType<typeof session> } }>();
    vi.spyOn(axios, 'post').mockReturnValue(refresh.promise);
    const pending = requestSessionRefresh();
    clearAuthSession();
    refresh.resolve({ data: { data: session('Alice') } });
    await expect(pending).rejects.toThrow('session changed');
    api.defaults.adapter = async config => response(config, { authorization: config.headers.Authorization });
    expect((await api.get('/probe')).data.authorization).toBeUndefined();
  });
});
