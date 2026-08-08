import { describe, expect, it } from 'vitest';
import { getClinicDateString } from './clinicTime';

describe('getClinicDateString', () => {
  it('uses the Amman clinic date ahead of UTC near midnight', () => {
    expect(getClinicDateString(new Date('2026-08-08T22:30:00Z'))).toBe('2026-08-09');
  });

  it('preserves the clinic date during daytime hours', () => {
    expect(getClinicDateString(new Date('2026-01-15T09:00:00Z'))).toBe('2026-01-15');
  });
});
