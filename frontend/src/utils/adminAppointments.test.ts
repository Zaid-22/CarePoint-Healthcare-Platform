import { describe, expect, it } from 'vitest';
import {
  appointmentStatusMap,
  getAllowedAdminAppointmentTransitions,
} from './adminAppointments';

describe('admin appointment status transitions', () => {
  it('offers only valid transitions for active appointments', () => {
    expect(getAllowedAdminAppointmentTransitions(0)).toEqual([1, 2, 5]);
    expect(getAllowedAdminAppointmentTransitions(1)).toEqual([3, 4, 5, 6]);
    expect(getAllowedAdminAppointmentTransitions(3)).toEqual([4, 5, 6]);
  });

  it.each([2, 4, 5, 6])('treats terminal status %s as immutable', (status) => {
    expect(getAllowedAdminAppointmentTransitions(status)).toEqual([]);
  });

  it('has a presentation label for every API status', () => {
    expect(Object.keys(appointmentStatusMap).map(Number).sort()).toEqual([0, 1, 2, 3, 4, 5, 6]);
  });
});
