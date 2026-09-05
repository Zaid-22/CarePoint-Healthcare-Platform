// @vitest-environment jsdom
import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import api from '../../api/client';
import DoctorAppointments from './DoctorAppointments';

vi.mock('../../api/client', () => ({ default: { get: vi.fn(), post: vi.fn() } }));
const appointments = ['Alice', 'Bob'].map((name, index) => ({
  id: `appointment-${index}`, patientProfileId: `patient-${index}`, patientName: name,
  appointmentDate: '2026-09-10T00:00:00', startTime: '10:00:00', endTime: '10:30:00', status: 1,
}));
const reply = (data: unknown) => ({ data: { data } });
const deferred = () => {
  let resolve!: (value: ReturnType<typeof reply>) => void;
  const promise = new Promise<ReturnType<typeof reply>>(done => { resolve = done; });
  return { promise, resolve };
};
beforeEach(() => {
  vi.mocked(api.get).mockReset();
  vi.mocked(api.get).mockResolvedValue(reply(appointments));
  vi.mocked(api.post).mockReset();
  vi.mocked(api.post).mockResolvedValue(reply({}));
});
afterEach(cleanup);

it('starts a clean record for Bob after closing an Alice draft and saves against Bob', async () => {
  render(<DoctorAppointments />);
  const buttons = await screen.findAllByRole('button', { name: '+ Add Record' });
  fireEvent.click(buttons[0]);
  fireEvent.change(screen.getByLabelText('Diagnosis'), { target: { value: 'Alice diagnosis' } });
  fireEvent.change(screen.getByLabelText('Treatment Plan'), { target: { value: 'Alice treatment' } });
  fireEvent.change(screen.getByLabelText('Clinical Notes'), { target: { value: 'Alice notes' } });
  fireEvent.click(screen.getByRole('button', { name: 'Close consultation dialog' }));
  fireEvent.click(buttons[1]);
  expect(screen.getByRole('heading', { name: 'New Medical Record: Bob' })).toBeDefined();
  for (const field of ['Diagnosis', 'Treatment Plan', 'Clinical Notes'])
    expect((screen.getByLabelText(field) as HTMLInputElement).value).toBe('');
  fireEvent.change(screen.getByLabelText('Diagnosis'), { target: { value: 'Bob diagnosis' } });
  fireEvent.click(screen.getByRole('button', { name: 'Save Record' }));
  await waitFor(() => expect(api.post).toHaveBeenCalledWith('/medicalrecords', {
    appointmentId: 'appointment-1', diagnosis: 'Bob diagnosis', treatment: '', notes: '',
  }, expect.objectContaining({ signal: expect.any(AbortSignal) })));
});

it('ignores Alice history arriving after Bob history', async () => {
  const alice = deferred();
  const bob = deferred();
  vi.mocked(api.get).mockImplementation(async url => {
    if (url === '/medicalrecords/patient/patient-0') return alice.promise;
    if (url === '/medicalrecords/patient/patient-1') return bob.promise;
    return reply(appointments);
  });
  render(<DoctorAppointments />);
  const buttons = await screen.findAllByRole('button', { name: 'Patient History' });
  fireEvent.click(buttons[0]);
  fireEvent.click(screen.getByRole('button', { name: 'Close consultation dialog' }));
  fireEvent.click(buttons[1]);
  await act(async () => bob.resolve(reply([{ id: 'bob-record', diagnosis: 'Bob diagnosis' }])));
  await act(async () => alice.resolve(reply([{ id: 'alice-record', diagnosis: 'Alice diagnosis' }])));
  expect(screen.getByRole('heading', { name: 'Medical History: Bob' })).toBeDefined();
  expect(screen.getByText('Diagnosis: Bob diagnosis')).toBeDefined();
  expect(screen.queryByText('Diagnosis: Alice diagnosis')).toBeNull();
});

it('does not close a different consultation when a previously submitted record completes', async () => {
  const save = deferred();
  vi.mocked(api.post).mockReturnValue(save.promise);
  render(<DoctorAppointments />);
  const buttons = await screen.findAllByRole('button', { name: '+ Add Record' });
  fireEvent.click(buttons[0]);
  fireEvent.change(screen.getByLabelText('Diagnosis'), { target: { value: 'Alice diagnosis' } });
  fireEvent.click(screen.getByRole('button', { name: 'Save Record' }));
  fireEvent.click(screen.getByRole('button', { name: 'Close consultation dialog' }));
  fireEvent.click(buttons[1]);
  await act(async () => save.resolve(reply({})));
  expect(screen.getByRole('heading', { name: 'New Medical Record: Bob' })).toBeDefined();
});
