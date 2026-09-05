import { useEffect, useRef, useState, type FormEvent } from 'react';
import api from '../../api/client';
import type { AppointmentDto } from '../../types';

interface Props {
  appointment: AppointmentDto;
  onSaved: () => void;
}

export default function DoctorMedicalRecordForm({ appointment, onSaved }: Props) {
  const [diagnosis, setDiagnosis] = useState('');
  const [treatment, setTreatment] = useState('');
  const [notes, setNotes] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const request = useRef<AbortController | null>(null);

  useEffect(() => () => request.current?.abort(), []);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (saving || !diagnosis.trim()) return;
    const controller = new AbortController();
    request.current = controller;
    setSaving(true);
    setError('');
    try {
      await api.post('/medicalrecords', {
        appointmentId: appointment.id,
        diagnosis: diagnosis.trim(),
        treatment,
        notes,
      }, { signal: controller.signal });
      if (!controller.signal.aborted) onSaved();
    } catch (requestError: any) {
      if (!controller.signal.aborted)
        setError(requestError.response?.data?.message || 'Failed to create medical record.');
    } finally {
      if (!controller.signal.aborted) setSaving(false);
    }
  };

  return (
    <form onSubmit={submit} style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <label className="form-group">
        <span className="form-label">Diagnosis</span>
        <input className="form-input" required maxLength={2000} value={diagnosis} onChange={(event) => setDiagnosis(event.target.value)} placeholder="e.g. Acute Rhinitis" />
      </label>
      <label className="form-group">
        <span className="form-label">Treatment Plan</span>
        <input className="form-input" maxLength={4000} value={treatment} onChange={(event) => setTreatment(event.target.value)} placeholder="e.g. Hydration, OTC antihistamines" />
      </label>
      <label className="form-group">
        <span className="form-label">Clinical Notes</span>
        <textarea className="form-input" rows={3} maxLength={4000} value={notes} onChange={(event) => setNotes(event.target.value)} placeholder="Additional clinical details…" />
      </label>
      {error && <div className="alert alert-error" role="alert">{error}</div>}
      <button type="submit" className="btn btn-primary" disabled={saving} style={{ marginTop: 8 }}>
        {saving ? 'Saving…' : 'Save Record'}
      </button>
    </form>
  );
}
