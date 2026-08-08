import { describe, expect, it } from 'vitest';
import { resolveApiAssetUrl } from './apiAssets';

describe('resolveApiAssetUrl', () => {
  it('resolves API-relative images against an absolute API origin', () => {
    expect(resolveApiAssetUrl(
      '/api/doctors/doctor-id/avatar?v=1',
      'https://api.carepoint.example/api',
      'https://app.carepoint.example',
    )).toBe('https://api.carepoint.example/api/doctors/doctor-id/avatar?v=1');
  });

  it('keeps API-relative images on the frontend origin when the API base is relative', () => {
    expect(resolveApiAssetUrl(
      '/api/doctors/doctor-id/avatar',
      '/api',
      'https://carepoint.example',
    )).toBe('https://carepoint.example/api/doctors/doctor-id/avatar');
  });

  it('does not rewrite an external HTTPS image', () => {
    expect(resolveApiAssetUrl(
      'https://images.example/doctor.png',
      'https://api.carepoint.example/api',
      'https://app.carepoint.example',
    )).toBe('https://images.example/doctor.png');
  });
});
