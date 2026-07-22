import { useEffect, useState } from 'react';
import api from '../../api/client';
import type { AppointmentDto, MedicalRecordDto, ApiResponse } from '../../types';
import { FileTextIcon } from '../../components/common/Icons';

export default function DoctorAppointments() {
  const [appointments, setAppointments] = useState<AppointmentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeModal, setActiveModal] = useState<'record' | 'prescription' | 'history' | null>(null);
  const [selectedApp, setSelectedApp] = useState<AppointmentDto | null>(null);

  // Patient history modal state
  const [patientHistory, setPatientHistory] = useState<MedicalRecordDto[]>([]);
  const [loadingHistory, setLoadingHistory] = useState(false);

  // Form states
  const [diagnosis, setDiagnosis] = useState('');
  const [treatment, setTreatment] = useState('');
  const [notes, setNotes] = useState('');

  const [medName, setMedName] = useState('');
  const [dosage, setDosage] = useState('');
  const [freq, setFreq] = useState('');

  const fetchAppointments = async () => {
    try {
      const res = await api.get<ApiResponse<AppointmentDto[]>>('/appointments/my-appointments');
      setAppointments(res.data.data || []);
    } catch (e) {
      console.error('Failed to fetch doctor appointments', e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchAppointments();
  }, []);

  const openHistoryModal = async (app: AppointmentDto) => {
    setSelectedApp(app);
    setActiveModal('history');
    setLoadingHistory(true);
    try {
      const res = await api.get<ApiResponse<MedicalRecordDto[]>>(`/medicalrecords/patient/${app.patientProfileId}`);
      setPatientHistory(res.data.data || []);
    } catch (e) {
      console.error('Failed to fetch patient history', e);
      setPatientHistory([]);
    } finally {
      setLoadingHistory(false);
    }
  };

  const handleUpdateStatus = async (id: string, status: number) => {
    try {
      await api.put(`/appointments/${id}/status`, { status });
      fetchAppointments();
    } catch {
      alert('Failed to update appointment status');
    }
  };

  const handleCreateRecord = async () => {
    if (!selectedApp) return;
    try {
      await api.post('/medicalrecords', {
        appointmentId: selectedApp.id,
        diagnosis,
        treatment,
        notes,
      });
      alert('Medical record saved successfully!');
      setActiveModal(null);
      setDiagnosis('');
      setTreatment('');
      setNotes('');
    } catch {
      alert('Failed to create medical record.');
    }
  };

  const handleCreatePrescription = async () => {
    if (!selectedApp) return;
    try {
      await api.post('/prescriptions', {
        appointmentId: selectedApp.id,
        notes,
        items: [
          { medicationName: medName, dosage, frequency: freq }
        ]
      });
      alert('Prescription created successfully!');
      setActiveModal(null);
      setMedName('');
      setDosage('');
      setFreq('');
    } catch {
      alert('Failed to create prescription.');
    }
  };

  const statusMap: Record<number, { label: string; badge: string }> = {
    0: { label: 'Pending', badge: 'badge-amber' },
    1: { label: 'Accepted', badge: 'badge-teal' },
    2: { label: 'Rejected', badge: 'badge-rose' },
    3: { label: 'In Progress', badge: 'badge-teal' },
    4: { label: 'Completed', badge: 'badge-stone' },
    5: { label: 'Cancelled', badge: 'badge-rose' },
    6: { label: 'No Show', badge: 'badge-stone' },
  };

  return (
    <div className="page-enter" style={{ display: 'flex', flexDirection: 'column', gap: 28 }}>
      <div>
        <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '2rem', fontWeight: 700 }}>
          Patient Consultations
        </h1>
        <p style={{ color: 'var(--text-secondary)' }}>Review requests, inspect patient history, and issue medical records & prescriptions.</p>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
        {loading ? (
          <div className="skeleton" style={{ height: 160 }} />
        ) : appointments.length === 0 ? (
          <div className="card" style={{ padding: 48, textAlign: 'center', color: 'var(--text-secondary)' }}>
            No patient appointments found.
          </div>
        ) : (
          appointments.map((app) => {
            const st = statusMap[app.status] || { label: 'Unknown', badge: 'badge-stone' };
            return (
              <div key={app.id} className="card" style={{ padding: 24, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                    <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.2rem', fontWeight: 700 }}>
                      Patient: {app.patientName}
                    </h3>
                    <span className={`badge ${st.badge}`}>{st.label}</span>
                  </div>

                  <div style={{ fontSize: '0.9rem', color: 'var(--text-secondary)', marginTop: 8 }}>
                    📅 {new Date(app.appointmentDate).toLocaleDateString()} • ⏰ {app.startTime} - {app.endTime}
                  </div>

                  {app.notes && (
                    <div style={{ fontSize: '0.85rem', marginTop: 6, fontStyle: 'italic', color: 'var(--text-secondary)' }}>
                      Patient note: "{app.notes}"
                    </div>
                  )}
                </div>

                <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                  <button
                    className="btn btn-ghost"
                    onClick={() => openHistoryModal(app)}
                    style={{ fontSize: '0.875rem', gap: 6 }}
                  >
                    <FileTextIcon size={16} /> Patient History
                  </button>

                  {app.status === 0 && (
                    <button className="btn btn-primary" onClick={() => handleUpdateStatus(app.id, 1)}>
                      Confirm Visit
                    </button>
                  )}
                  {app.status === 1 && (
                    <>
                      <button className="btn btn-secondary" onClick={() => { setSelectedApp(app); setActiveModal('record'); }}>
                        + Add Record
                      </button>
                      <button className="btn btn-secondary" onClick={() => { setSelectedApp(app); setActiveModal('prescription'); }}>
                        + Rx Prescription
                      </button>
                      <button className="btn btn-primary" onClick={() => handleUpdateStatus(app.id, 4)}>
                        Mark Completed
                      </button>
                    </>
                  )}
                </div>
              </div>
            );
          })
        )}
      </div>

      {/* Modal overlays */}
      {activeModal && selectedApp && (
        <div style={{
          position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
          background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000,
        }}>
          <div className="card page-enter" style={{ width: activeModal === 'history' ? 640 : 480, padding: 32, background: 'var(--bg-surface)', maxHeight: '85vh', overflowY: 'auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 20, alignItems: 'center' }}>
              <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700 }}>
                {activeModal === 'record'
                  ? 'New Medical Record'
                  : activeModal === 'prescription'
                  ? 'Issue Prescription'
                  : `Medical History: ${selectedApp.patientName}`}
              </h2>
              <button className="btn btn-ghost" onClick={() => setActiveModal(null)}>✕</button>
            </div>

            {activeModal === 'record' ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                <div className="form-group">
                  <label className="form-label">Diagnosis</label>
                  <input className="form-input" value={diagnosis} onChange={(e) => setDiagnosis(e.target.value)} placeholder="e.g. Acute Rhinitis" />
                </div>
                <div className="form-group">
                  <label className="form-label">Treatment Plan</label>
                  <input className="form-input" value={treatment} onChange={(e) => setTreatment(e.target.value)} placeholder="e.g. Hydration, OTC antihistamines" />
                </div>
                <div className="form-group">
                  <label className="form-label">Clinical Notes</label>
                  <textarea className="form-input" rows={3} value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="Additional clinical details…" />
                </div>
                <button className="btn btn-primary" onClick={handleCreateRecord} style={{ marginTop: 8 }}>
                  Save Record
                </button>
              </div>
            ) : activeModal === 'prescription' ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                <div className="form-group">
                  <label className="form-label">Medication Name</label>
                  <input className="form-input" value={medName} onChange={(e) => setMedName(e.target.value)} placeholder="e.g. Amoxicillin" />
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                  <div className="form-group">
                    <label className="form-label">Dosage</label>
                    <input className="form-input" value={dosage} onChange={(e) => setDosage(e.target.value)} placeholder="e.g. 500mg" />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Frequency</label>
                    <input className="form-input" value={freq} onChange={(e) => setFreq(e.target.value)} placeholder="e.g. Twice daily" />
                  </div>
                </div>
                <button className="btn btn-primary" onClick={handleCreatePrescription} style={{ marginTop: 8 }}>
                  Issue Prescription
                </button>
              </div>
            ) : (
              /* Patient History Modal */
              <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                {loadingHistory ? (
                  <div className="skeleton" style={{ height: 120 }} />
                ) : patientHistory.length === 0 ? (
                  <div style={{ padding: 24, textAlign: 'center', color: 'var(--text-secondary)' }}>
                    No previous medical records found for this patient.
                  </div>
                ) : (
                  patientHistory.map((rec) => (
                    <div key={rec.id} style={{
                      padding: 18,
                      borderRadius: 'var(--radius-md)',
                      border: '1px solid var(--border-default)',
                      background: 'var(--bg-page)',
                      display: 'flex',
                      flexDirection: 'column',
                      gap: 8
                    }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ fontWeight: 700, color: 'var(--color-teal-900)', fontSize: '1rem' }}>
                          Diagnosis: {rec.diagnosis}
                        </span>
                        <span style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)' }}>
                          📅 {new Date(rec.appointmentDate || rec.createdAt).toLocaleDateString()}
                        </span>
                      </div>

                      {rec.doctorName && (
                        <div style={{ fontSize: '0.8125rem', color: 'var(--accent)', fontWeight: 500 }}>
                          Physician: {rec.doctorName}
                        </div>
                      )}

                      {rec.treatment && (
                        <div style={{ fontSize: '0.875rem', color: 'var(--text-primary)', background: 'var(--color-teal-50)', padding: 10, borderRadius: 6 }}>
                          <strong>Treatment: </strong> {rec.treatment}
                        </div>
                      )}

                      {rec.notes && (
                        <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', fontStyle: 'italic' }}>
                          Notes: {rec.notes}
                        </div>
                      )}
                    </div>
                  ))
                )}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
