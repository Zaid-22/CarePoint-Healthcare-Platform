import { useEffect, useState } from 'react';
import api from '../../api/client';
import type { AppointmentDto, AppointmentSummaryDto, DoctorDto, ApiResponse } from '../../types';
import doctorPortrait from '../../assets/doctor_portrait.png';
import { ClockIcon } from '../../components/common/Icons';
import { getClinicDateString } from '../../utils/clinicTime';

export default function DoctorDashboard() {
  const [appointments, setAppointments] = useState<AppointmentDto[]>([]);
  const [profile, setProfile] = useState<DoctorDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [summary, setSummary] = useState<AppointmentSummaryDto>({
    totalCount: 0, pendingCount: 0, upcomingCount: 0, todayCount: 0,
  });

  useEffect(() => {
    async function loadData() {
      try {
        const clinicDate = getClinicDateString();
        const [appsRes, summaryRes, profileRes] = await Promise.all([
          api.get<ApiResponse<AppointmentDto[]>>(`/appointments/my-appointments?statusGroup=active&date=${clinicDate}&take=100`),
          api.get<ApiResponse<AppointmentSummaryDto>>('/appointments/summary'),
          api.get<ApiResponse<DoctorDto>>('/doctors/me').catch(() => null),
        ]);
        setAppointments(appsRes.data.data || []);
        setSummary(summaryRes.data.data);
        if (profileRes?.data.data) {
          setProfile(profileRes.data.data);
        }
      } catch (e) {
        console.error('Failed to load doctor dashboard', e);
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, []);

  const todayStr = getClinicDateString();
  const todayApps = appointments.filter((a) => a.appointmentDate && a.appointmentDate.startsWith(todayStr));

  return (
    <div className="page-enter" style={{ display: 'flex', flexDirection: 'column', gap: 32 }}>
      {/* Header Banner */}
      <div className="card" style={{ padding: 32, display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: 'linear-gradient(135deg, #ffffff 0%, var(--accent-light) 100%)' }}>
        <div>
          <span className="badge badge-amber" style={{ marginBottom: 8 }}>Doctor Portal</span>
          <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 700 }}>
            {profile?.firstName ? `Welcome back, Dr. ${profile.firstName} ${profile.lastName}` : 'Practitioner Overview'}
          </h1>
          <p style={{ color: 'var(--text-secondary)', marginTop: 4 }}>Manage patient schedules and clinical consultations efficiently.</p>
        </div>
        <img
          src={profile?.profilePictureUrl && profile.profilePictureUrl.trim() !== '' ? profile.profilePictureUrl : doctorPortrait}
          alt="Doctor Avatar"
          style={{ width: 80, height: 80, borderRadius: 99, objectFit: 'cover', border: '3px solid var(--accent)', boxShadow: '0 4px 12px rgba(0,0,0,0.1)' }}
          onError={(e) => { (e.target as HTMLImageElement).src = doctorPortrait; }}
        />
      </div>

      {/* Metrics */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: 20 }}>
        <div className="card" style={{ padding: 24 }}>
          <span style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', textTransform: 'uppercase' }}>
            Today's Patients
          </span>
          <div style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 700, marginTop: 8 }}>
            {summary.todayCount}
          </div>
        </div>

        <div className="card" style={{ padding: 24 }}>
          <span style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', textTransform: 'uppercase' }}>
            Pending Confirmations
          </span>
          <div style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 700, marginTop: 8, color: 'var(--color-amber-500)' }}>
            {summary.pendingCount}
          </div>
        </div>

        <div className="card" style={{ padding: 24 }}>
          <span style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', textTransform: 'uppercase' }}>
            Total Consultations
          </span>
          <div style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 700, marginTop: 8 }}>
            {summary.totalCount}
          </div>
        </div>
      </div>

      {/* Today's Schedule */}
      <div className="card" style={{ padding: 28 }}>
        <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700, marginBottom: 20 }}>
          Today's Appointments
        </h2>

        {loading ? (
          <div className="skeleton" style={{ height: 100 }} />
        ) : todayApps.length === 0 ? (
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.9375rem' }}>No appointments scheduled for today.</p>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
            {todayApps.map((app) => (
              <div key={app.id} style={{ padding: 16, border: '1px solid var(--border-default)', borderRadius: 'var(--radius-md)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div>
                  <div style={{ fontWeight: 600 }}>{app.patientName}</div>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: 4, marginTop: 2 }}>
                    <ClockIcon size={14} color="var(--accent)" />
                    {app.startTime} - {app.endTime}
                  </div>
                </div>
                <span className="badge badge-teal">Scheduled</span>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
