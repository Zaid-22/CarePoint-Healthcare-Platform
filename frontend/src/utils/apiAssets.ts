export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api';

export const resolveApiAssetUrl = (
  value: string,
  apiBaseUrl = API_BASE_URL,
  browserOrigin = globalThis.location?.origin ?? 'http://localhost',
): string => {
  if (!value || /^(?:data|blob):/i.test(value)) return value;

  try {
    const configuredApiUrl = new URL(apiBaseUrl, browserOrigin);
    return value.startsWith('/')
      ? new URL(value, configuredApiUrl.origin).toString()
      : new URL(value, configuredApiUrl).toString();
  } catch {
    return value;
  }
};
