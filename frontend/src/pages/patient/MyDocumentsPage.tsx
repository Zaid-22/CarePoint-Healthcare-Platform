import { useEffect, useState } from 'react';
import api from '../../api/client';
import MedicalDocumentsPanel from '../../components/common/MedicalDocumentsPanel';
import type { ApiResponse, PatientDto } from '../../types';

export default function MyDocumentsPage() {
  const [patientId, setPatientId] = useState('');
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    api.get<ApiResponse<PatientDto>>('/patients/me')
      .then((response) => setPatientId(response.data.data.id))
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
        <MedicalDocumentsPanel patientProfileId={patientId} allowDelete />
      ) : (
        <div className="skeleton" style={{ height: 220 }} />
      )}
    </div>
  );
}
