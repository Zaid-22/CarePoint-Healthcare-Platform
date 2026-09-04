import { useCallback, useEffect, useState } from 'react';
import api from '../../api/client';
import type {
  ApiResponse,
  AppointmentDto,
  PrescriptionDto,
  PrescriptionItemDto,
  UpdatePrescriptionRequest,
} from '../../types';
import PrescriptionRevisionHistory from './PrescriptionRevisionHistory';

type MedicationInput = Omit<PrescriptionItemDto, 'id'>;

const emptyMedication = (): MedicationInput => ({
  medicationName: '',
  dosage: '',
  frequency: '',
  duration: '',
  instructions: '',
});

interface Props {
  appointment: AppointmentDto;
}

export default function DoctorPrescriptionPanel({ appointment }: Props) {
  const [prescriptions, setPrescriptions] = useState<PrescriptionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [editing, setEditing] = useState<PrescriptionDto | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [notes, setNotes] = useState('');
  const [changeReason, setChangeReason] = useState('');
  const [items, setItems] = useState<MedicationInput[]>([emptyMedication()]);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

  const loadPrescriptions = useCallback(async (signal?: AbortSignal) => {
    setLoading(true);
    try {
      const response = await api.get<ApiResponse<PrescriptionDto[]>>(
        `/prescriptions/appointment/${appointment.id}?take=100`,
        { signal },
      );
      const loaded = response.data.data ?? [];
      setPrescriptions(loaded);
      setShowForm(loaded.length === 0);
      setError('');
    } catch (requestError: any) {
      if (signal?.aborted) return;
      setError(requestError.response?.data?.message ?? 'Prescriptions could not be loaded.');
    } finally {
      if (!signal?.aborted) setLoading(false);
    }
  }, [appointment.id]);

  useEffect(() => {
    const controller = new AbortController();
    void loadPrescriptions(controller.signal);
    return () => controller.abort();
  }, [loadPrescriptions]);

  const resetForm = () => {
    setEditing(null);
    setNotes('');
    setChangeReason('');
    setItems([emptyMedication()]);
    setShowForm(false);
  };

  const startCreate = () => {
    setEditing(null);
    setNotes('');
    setChangeReason('');
    setItems([emptyMedication()]);
    setMessage('');
    setError('');
    setShowForm(true);
  };

  const startCorrection = (prescription: PrescriptionDto) => {
    setEditing(prescription);
    setNotes(prescription.notes ?? '');
    setChangeReason('');
    setItems(prescription.items.map((item) => ({
      medicationName: item.medicationName,
      dosage: item.dosage,
      frequency: item.frequency,
      duration: item.duration,
      instructions: item.instructions,
    })));
    setMessage('');
    setError('');
    setShowForm(true);
  };

  const updateItem = (index: number, patch: Partial<MedicationInput>) => {
    setItems((current) => current.map((item, itemIndex) => (
      itemIndex === index ? { ...item, ...patch } : item
    )));
  };

  const submit = async () => {
    const normalizedItems = items.map((item) => ({
      medicationName: item.medicationName.trim(),
      dosage: item.dosage.trim(),
      frequency: item.frequency.trim(),
      duration: item.duration?.trim() || undefined,
      instructions: item.instructions?.trim() || undefined,
    }));
    if (normalizedItems.some((item) => !item.medicationName || !item.dosage || !item.frequency)) {
      setError('Medication name, dosage, and frequency are required for every item.');
      return;
    }
    if (editing && !changeReason.trim()) {
      setError('Explain why this prescription is being corrected.');
      return;
    }

    setSubmitting(true);
    setError('');
    setMessage('');
    try {
      if (editing) {
        const payload: UpdatePrescriptionRequest = {
          notes: notes.trim() || undefined,
          items: normalizedItems,
          changeReason: changeReason.trim(),
          rowVersion: editing.rowVersion,
        };
        await api.put(`/prescriptions/${editing.id}`, payload);
        setMessage('Prescription corrected and the previous version was preserved.');
      } else {
        await api.post('/prescriptions', {
          appointmentId: appointment.id,
          notes: notes.trim() || undefined,
          items: normalizedItems,
        });
        setMessage('Prescription issued successfully.');
      }
      resetForm();
      await loadPrescriptions();
    } catch (requestError: any) {
      setError(requestError.response?.data?.message ?? 'The prescription could not be saved.');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) return <div className="skeleton" style={{ height: 160 }} />;

  return (
    <div style={{ display: 'grid', gap: 18 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, alignItems: 'center' }}>
        <div>
          <strong>Prescription ledger</strong>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.8125rem' }}>
            Corrections preserve the previous medication list and require a clinical reason.
          </p>
        </div>
        {!showForm && (
          <button type="button" className="btn btn-primary" onClick={startCreate}>
            Issue prescription
          </button>
        )}
      </div>

      {message && <div className="alert alert-success">{message}</div>}
      {error && <div className="alert alert-error">{error}</div>}

      {showForm && (
        <section style={{ border: '1px solid var(--border-default)', padding: 18, background: 'var(--bg-page)' }}>
          <h3 style={{ fontFamily: 'var(--font-display)', marginBottom: 14 }}>
            {editing ? 'Correct prescription' : 'New prescription'}
          </h3>
          <div style={{ display: 'grid', gap: 14 }}>
            {items.map((item, index) => (
              <div key={index} style={{ display: 'grid', gap: 10, borderBottom: '1px dashed var(--border-default)', paddingBottom: 14 }}>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))', gap: 10 }}>
                  <label className="form-group">
                    <span className="form-label">Medication</span>
                    <input className="form-input" value={item.medicationName} maxLength={200} onChange={(event) => updateItem(index, { medicationName: event.target.value })} />
                  </label>
                  <label className="form-group">
                    <span className="form-label">Dosage</span>
                    <input className="form-input" value={item.dosage} maxLength={100} onChange={(event) => updateItem(index, { dosage: event.target.value })} />
                  </label>
                  <label className="form-group">
                    <span className="form-label">Frequency</span>
                    <input className="form-input" value={item.frequency} maxLength={100} onChange={(event) => updateItem(index, { frequency: event.target.value })} />
                  </label>
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 10, alignItems: 'end' }}>
                  <label className="form-group">
                    <span className="form-label">Duration</span>
                    <input className="form-input" value={item.duration ?? ''} maxLength={100} onChange={(event) => updateItem(index, { duration: event.target.value })} />
                  </label>
                  <label className="form-group">
                    <span className="form-label">Instructions</span>
                    <input className="form-input" value={item.instructions ?? ''} maxLength={1000} onChange={(event) => updateItem(index, { instructions: event.target.value })} />
                  </label>
                  <button type="button" className="btn btn-ghost" disabled={items.length === 1} onClick={() => setItems((current) => current.filter((_, itemIndex) => itemIndex !== index))}>
                    Remove
                  </button>
                </div>
              </div>
            ))}
            <button type="button" className="btn btn-secondary" onClick={() => setItems((current) => [...current, emptyMedication()])}>
              Add medication
            </button>
            <label className="form-group">
              <span className="form-label">Prescription note</span>
              <textarea className="form-input" rows={3} maxLength={2000} value={notes} onChange={(event) => setNotes(event.target.value)} />
            </label>
            {editing && (
              <label className="form-group">
                <span className="form-label">Reason for correction</span>
                <textarea className="form-input" rows={2} maxLength={500} required value={changeReason} onChange={(event) => setChangeReason(event.target.value)} />
              </label>
            )}
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
              <button type="button" className="btn btn-ghost" disabled={submitting} onClick={resetForm}>Cancel</button>
              <button type="button" className="btn btn-primary" disabled={submitting} onClick={submit}>
                {submitting ? 'Saving…' : editing ? 'Save correction' : 'Issue prescription'}
              </button>
            </div>
          </div>
        </section>
      )}

      {prescriptions.length === 0 && !showForm ? (
        <p style={{ color: 'var(--text-secondary)' }}>No prescriptions have been issued for this consultation.</p>
      ) : prescriptions.map((prescription) => (
        <article key={prescription.id} style={{ border: '1px solid var(--border-default)', padding: 16 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, alignItems: 'flex-start' }}>
            <div>
              <strong>{prescription.items.map((item) => item.medicationName).join(', ')}</strong>
              <div style={{ color: 'var(--text-secondary)', fontSize: '0.75rem', marginTop: 2 }}>
                Issued {new Date(prescription.createdAt).toLocaleString()}
              </div>
            </div>
            <button type="button" className="btn btn-secondary" onClick={() => startCorrection(prescription)}>
              Correct
            </button>
          </div>
          {prescription.notes && <p style={{ marginTop: 10, fontSize: '0.875rem' }}>{prescription.notes}</p>}
          <PrescriptionRevisionHistory
            prescriptionId={prescription.id}
            revisionVersion={prescription.rowVersion}
          />
        </article>
      ))}
    </div>
  );
}
