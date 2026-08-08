export interface AppointmentStatusPresentation {
  label: string;
  badge: string;
}

export const appointmentStatusMap: Record<number, AppointmentStatusPresentation> = {
  0: { label: 'Pending', badge: 'badge-amber' },
  1: { label: 'Accepted', badge: 'badge-teal' },
  2: { label: 'Rejected', badge: 'badge-rose' },
  3: { label: 'In progress', badge: 'badge-teal' },
  4: { label: 'Completed', badge: 'badge-stone' },
  5: { label: 'Cancelled', badge: 'badge-rose' },
  6: { label: 'No show', badge: 'badge-stone' },
};

const allowedTransitions: Readonly<Record<number, readonly number[]>> = {
  0: [1, 2, 5],
  1: [3, 4, 5, 6],
  3: [4, 5, 6],
};

export const getAllowedAdminAppointmentTransitions = (status: number): readonly number[] =>
  allowedTransitions[status] ?? [];
