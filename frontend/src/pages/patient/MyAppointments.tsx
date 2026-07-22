import { useEffect, useState } from 'react';
import api from '../../api/client';
import type { AppointmentDto, ApiResponse } from '../../types';
import { CalendarIcon, ClockIcon } from '../../components/common/Icons';

export default function MyAppointments() {
  const [appointments, setAppointments] = useState<AppointmentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<'all' | 'upcoming' | 'completed' | 'cancelled'>('all');
  const [cancellingId, setCancellingId] = useState<string | null>(null);

  const fetchAppointments = async () => {
    try {
      const res = await api.get<ApiResponse<AppointmentDto[]>>('/appointments/my-appointments');
      setAppointments(res.data.data || []);
    } catch (e) {
      console.error('Failed to fetch appointments', e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchAppointments();
  }, []);

  const handleCancel = async (id: string) => {
    const reason = prompt('Please enter a cancellation reason:');
    if (!reason) return;
    setCancellingId(id);
    try {
      await api.put(`/appointments/${id}/cancel`, { cancellationReason: reason });
      await fetchAppointments();
    } catch {
      alert('Failed to cancel appointment');
    } finally {
      setCancellingId(null);
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

  const filtered = appointments.filter((app) => {
    if (filter === 'upcoming') return app.status === 0 || app.status === 1;
    if (filter === 'completed') return app.status === 4;
    if (filter === 'cancelled') return app.status === 2 || app.status === 5 || app.status === 6;
    return true;
  });

  return (
    <div className="page-enter" style={{ display: 'flex', flexDirection: 'column', gap: 28 }}>
      <div>
        <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '2rem', fontWeight: 700 }}>
          My Appointments
        </h1>
        <p style={{ color: 'var(--text-secondary)' }}>Track and manage your upcoming and past medical visits.</p>
      </div>

      {/* Filter Tabs */}
      <div style={{ display: 'flex', gap: 8, borderBottom: '1px solid var(--border-default)', paddingBottom: 12 }}>
        {(['all', 'upcoming', 'completed', 'cancelled'] as const).map((tab) => (
          <button
            key={tab}
            className={`btn ${filter === tab ? 'btn-primary' : 'btn-ghost'}`}
            style={{ textTransform: 'capitalize', fontSize: '0.875rem' }}
            onClick={() => setFilter(tab)}
          >
            {tab}
          </button>
        ))}
      </div>

      {/* List */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
        {loading ? (
          <div className="skeleton" style={{ height: 140 }} />
        ) : filtered.length === 0 ? (
          <div className="card" style={{ padding: 48, textAlign: 'center', color: 'var(--text-secondary)' }}>
            No appointments found for this filter.
          </div>
        ) : (
          filtered.map((app) => {
            const st = statusMap[app.status] || { label: 'Unknown', badge: 'badge-stone' };
            return (
              <div key={app.id} className="card" style={{ padding: 24, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                    <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.2rem', fontWeight: 700 }}>
                      {app.doctorName}
                    </h3>
                    <span className={`badge ${st.badge}`}>{st.label}</span>
                  </div>

                  <div style={{ fontSize: '0.9rem', color: 'var(--text-secondary)', marginTop: 8, display: 'flex', alignItems: 'center', gap: 12 }}>
                    <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                      <CalendarIcon size={15} color="var(--accent)" />
                      {new Date(app.appointmentDate).toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' })}
                    </span>
                    <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                      <ClockIcon size={15} color="var(--accent)" />
                      {app.startTime} - {app.endTime}
                    </span>
                  </div>

                  {app.notes && (
                    <div style={{ fontSize: '0.85rem', color: 'var(--text-primary)', marginTop: 6, fontStyle: 'italic' }}>
                      " {app.notes} "
                    </div>
                  )}

                  {app.cancellationReason && (
                    <div style={{ fontSize: '0.8125rem', color: 'var(--color-rose-600)', marginTop: 4 }}>
                      Reason for cancellation: {app.cancellationReason}
                    </div>
                  )}
                </div>

                {(app.status === 0 || app.status === 1 || app.status === 3) && (
                  <button
                    className="btn btn-danger"
                    disabled={cancellingId === app.id}
                    onClick={() => handleCancel(app.id)}
                  >
                    Cancel Visit
                  </button>
                )}
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}
