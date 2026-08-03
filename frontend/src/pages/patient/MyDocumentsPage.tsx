import { useEffect, useState } from 'react';
import api from '../../api/client';
import MedicalDocumentsPanel from '../../components/common/MedicalDocumentsPanel';
import type { ApiResponse, AppointmentDto, PatientDto } from '../../types';

export default function MyDocumentsPage() {
  const [patientId, setPatientId] = useState('');
  const [failed, setFailed] = useState(false);
  const [appointmentOptions, setAppointmentOptions] = useState<Array<{ id: string; label: string }>>([]);

  useEffect(() => {
    Promise.all([
      api.get<ApiResponse<PatientDto>>('/patients/me'),
      api.get<ApiResponse<AppointmentDto[]>>('/appointments/my-appointments?take=100'),
    ])
      .then(([patientResponse, appointmentResponse]) => {
        setPatientId(patientResponse.data.data.id);
        setAppointmentOptions((appointmentResponse.data.data ?? [])
          .filter((appointment) => [1, 3, 4].includes(appointment.status))
          .map((appointment) => ({
            id: appointment.id,
            label: `${appointment.doctorName} · ${new Date(appointment.appointmentDate).toLocaleDateString()}`,
          })));
      })
      .catch(() => setFailed(true));
  }, []);

  return (
    <div className="page-enter" style={{ display: 'flex', flexDirection: 'column', gap: 28 }}>
      <div>
        <div className="badge badge-teal" style={{ marginBottom: 10 }}>Private health vault</div>
        <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '2rem', fontWeight: 700 }}>My Documents</h1>
        <p style={{ color: 'var(--text-secondary)', maxWidth: 660 }}>
          Keep lab results, scans, referrals, and discharge notes with your CarePoint record.
        </p>
      </div>
      {failed ? (
        <div className="alert alert-error">Your patient profile could not be loaded.</div>
      ) : patientId ? (
        <MedicalDocumentsPanel
          patientProfileId={patientId}
          appointmentOptions={appointmentOptions}
          allowDelete
        />
      ) : (
        <div className="skeleton" style={{ height: 220 }} />
      )}
    </div>
  );
}
