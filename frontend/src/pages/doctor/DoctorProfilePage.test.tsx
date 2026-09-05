// @vitest-environment jsdom
import axios from 'axios';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import api from '../../api/client';
import DoctorProfilePage from './DoctorProfilePage';

vi.mock('../../api/client', () => ({ default: { get: vi.fn(), post: vi.fn() } }));
afterEach(cleanup);

it('keeps an uploaded avatar as multipart FormData despite the JSON client default', async () => {
  const profile = { id: 'doctor-1', fullName: 'Doctor Example', specialties: [], clinics: [] };
  vi.mocked(api.get).mockImplementation(async url => ({ data: { data: url === '/doctors/me' ? profile : [] } }));
  const transport = vi.fn(async config => ({ data: { data: profile }, status: 200, statusText: 'OK', headers: {}, config }));
  const jsonClient = axios.create({ headers: { 'Content-Type': 'application/json' }, adapter: transport });
  vi.mocked(api.post).mockImplementation((url, data, config) => jsonClient.post(url, data, config));
  render(<DoctorProfilePage />);
  fireEvent.click(await screen.findByRole('button', { name: /Photo & Branding/ }));
  const file = new File(['avatar'], 'avatar.png', { type: 'image/png' });
  fireEvent.change(screen.getByLabelText(/Select Image File/), { target: { files: [file] } });
  await screen.findByText('Profile image uploaded securely.');
  expect(transport).toHaveBeenCalledOnce();
  const request = transport.mock.calls[0][0];
  expect(request.url).toBe('/doctors/me/avatar');
  expect(request.data).toBeInstanceOf(FormData);
  expect(request.data.get('file')).toBe(file);
  expect(request.headers.getContentType()).toBe('multipart/form-data');
});
