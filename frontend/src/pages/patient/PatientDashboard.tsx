import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api from '../../api/client';
import type { AppointmentDto, AppointmentSummaryDto, NotificationDto, PatientDto, ApiResponse } from '../../types';
import { CalendarIcon, ClockIcon } from '../../components/common/Icons';

export default function PatientDashboard() {
  const [appointments, setAppointments] = useState<AppointmentDto[]>([]);
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [profile, setProfile] = useState<PatientDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [summary, setSummary] = useState<AppointmentSummaryDto>({
    totalCount: 0, pendingCount: 0, upcomingCount: 0, todayCount: 0,
  });

  useEffect(() => {
    async function loadData() {
      try {
        const [appRes, summaryRes, notifRes, profileRes] = await Promise.all([
          api.get<ApiResponse<AppointmentDto[]>>('/appointments/my-appointments?statusGroup=upcoming&take=10'),
          api.get<ApiResponse<AppointmentSummaryDto>>('/appointments/summary'),
          api.get<ApiResponse<NotificationDto[]>>('/notifications'),
          api.get<ApiResponse<PatientDto>>('/patients/me').catch(() => null),
        ]);
        setAppointments(appRes.data.data || []);
        setSummary(summaryRes.data.data);
        setNotifications(notifRes.data.data || []);
        if (profileRes?.data.data) {
          setProfile(profileRes.data.data);
        }
      } catch (e) {
        console.error('Failed to load dashboard data', e);
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, []);

  const upcoming = appointments.filter((a) => a.status === 0 || a.status === 1);

  return (
    <div className="page-enter" style={{ display: 'flex', flexDirection: 'column', gap: 32 }}>
      {/* Header Banner */}
      <div className="card" style={{ padding: 32, display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 20, background: 'linear-gradient(135deg, #ffffff 0%, var(--accent-light) 100%)' }}>
        <div>
          <span className="badge badge-teal" style={{ marginBottom: 8 }}>Patient Portal</span>
          <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 700 }}>
            {profile?.firstName ? `Welcome back, ${profile.firstName} ${profile.lastName}` : 'Health Overview'}
          </h1>
          <p style={{ color: 'var(--text-secondary)', marginTop: 4 }}>Manage appointments, consultations, and personal health metrics.</p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
          <div style={{
            width: 52,
            height: 52,
            borderRadius: '50%',
            background: 'var(--accent)',
            color: 'white',
            fontWeight: 700,
            fontSize: '1.125rem',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: '0 4px 14px rgba(13, 148, 136, 0.3)',
            border: '2px solid white',
            flexShrink: 0
          }}>
            {profile?.firstName && profile?.lastName ? `${profile.firstName[0]}${profile.lastName[0]}` : 'P'}
          </div>

          <Link to="/find-doctors" className="btn btn-primary glow-btn" style={{ padding: '12px 24px' }}>
            + Book Appointment
          </Link>
        </div>
      </div>

      {/* Quick stats */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: 20 }}>
        <div className="card" style={{ padding: 24 }}>
          <span style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
            Upcoming Visits
          </span>
          <div style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 700, marginTop: 8 }}>
            {summary.upcomingCount}
          </div>
        </div>

        <div className="card" style={{ padding: 24 }}>
          <span style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
            Total Appointments
          </span>
          <div style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 700, marginTop: 8 }}>
            {summary.totalCount}
          </div>
        </div>

        <div className="card" style={{ padding: 24 }}>
          <span style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
            Notifications
          </span>
          <div style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 700, marginTop: 8 }}>
            {notifications.filter((n) => !n.isRead).length}
          </div>
        </div>
      </div>

      {/* Content grid */}
      <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: 28 }}>
        {/* Next Appointment Card */}
        <div className="card" style={{ padding: 28 }}>
          <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700, marginBottom: 20 }}>
            Upcoming Appointments
          </h2>

          {loading ? (
            <div className="skeleton" style={{ height: 100 }} />
          ) : upcoming.length === 0 ? (
            <div style={{ padding: '36px 24px', textAlign: 'center', background: 'var(--bg-subtle)', borderRadius: 'var(--radius-md)' }}>
              <p style={{ color: 'var(--text-secondary)', marginBottom: 12 }}>No upcoming appointments scheduled.</p>
              <Link to="/find-doctors" className="btn btn-secondary" style={{ fontSize: '0.875rem' }}>
                Find a Doctor
              </Link>
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
              {upcoming.map((app) => (
                <div
                  key={app.id}
                  style={{
                    padding: 16,
                    borderRadius: 'var(--radius-md)',
                    border: '1px solid var(--border-default)',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                  }}
                >
                  <div>
                    <div style={{ fontWeight: 600, fontSize: '1rem' }}>{app.doctorName}</div>
                    <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginTop: 2, display: 'flex', alignItems: 'center', gap: 12 }}>
                      <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                        <CalendarIcon size={14} color="var(--accent)" />
                        {new Date(app.appointmentDate).toLocaleDateString()}
                      </span>
                      <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                        <ClockIcon size={14} color="var(--accent)" />
                        {app.startTime} - {app.endTime}
                      </span>
                    </div>
                  </div>
                  <span className={`badge ${app.status === 1 ? 'badge-teal' : 'badge-amber'}`}>
                    {app.status === 1 ? 'Confirmed' : 'Pending'}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Notifications Sidebar */}
        <div className="card" style={{ padding: 28 }}>
          <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700, marginBottom: 20 }}>
            Recent Activity
          </h2>

          {loading ? (
            <div className="skeleton" style={{ height: 120 }} />
          ) : notifications.length === 0 ? (
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>No recent notifications.</p>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
              {notifications.slice(0, 5).map((n) => (
                <div key={n.id} style={{ borderBottom: '1px solid var(--border-default)', paddingBottom: 10 }}>
                  <div style={{ fontWeight: 600, fontSize: '0.875rem' }}>{n.title}</div>
                  <div style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', marginTop: 2 }}>
                    {n.message}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
