import { useCallback, useEffect, useState } from 'react';
import api from '../../api/client';
import AdminPageHeader from '../../components/admin/AdminPageHeader';
import PaginationControls from '../../components/common/PaginationControls';
import { CalendarIcon } from '../../components/common/Icons';
import type { ApiResponse, AppointmentDto } from '../../types';

const PAGE_SIZE = 20;

const statusMap: Record<number, { label: string; badge: string }> = {
  0: { label: 'Pending', badge: 'badge-amber' },
  1: { label: 'Accepted', badge: 'badge-teal' },
  2: { label: 'Rejected', badge: 'badge-rose' },
  3: { label: 'In progress', badge: 'badge-teal' },
  4: { label: 'Completed', badge: 'badge-stone' },
  5: { label: 'Cancelled', badge: 'badge-rose' },
  6: { label: 'No show', badge: 'badge-stone' },
};

const allowedTransitions: Record<number, number[]> = {
  0: [1, 2, 5],
  1: [3, 4, 5, 6],
  3: [4, 5, 6],
};

function displayDate(value: string) {
  const datePart = value.split('T')[0];
  return new Intl.DateTimeFormat('en-JO', { dateStyle: 'medium' }).format(new Date(`${datePart}T00:00:00`));
}

export default function AdminAppointmentsPage() {
  const [appointments, setAppointments] = useState<AppointmentDto[]>([]);
  const [statusGroup, setStatusGroup] = useState('active');
  const [date, setDate] = useState('');
  const [skip, setSkip] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [updatingId, setUpdatingId] = useState<string | null>(null);
  const [selectedStatuses, setSelectedStatuses] = useState<Record<string, string>>({});
  const [reasons, setReasons] = useState<Record<string, string>>({});
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fetchAppointments = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const params = new URLSearchParams({
        statusGroup,
        skip: String(skip),
        take: String(PAGE_SIZE),
      });
      if (date) params.set('date', date);
      const response = await api.get<ApiResponse<AppointmentDto[]>>(`/appointments?${params}`);
      setAppointments(response.data.data ?? []);
      setTotalCount(response.data.pagination?.totalCount ?? response.data.data?.length ?? 0);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load appointments.');
    } finally {
      setLoading(false);
    }
  }, [date, skip, statusGroup]);

  useEffect(() => {
    fetchAppointments();
  }, [fetchAppointments]);

  const updateStatus = async (appointment: AppointmentDto) => {
    const selected = selectedStatuses[appointment.id];
    if (!selected) return;
    const status = Number(selected);
    setUpdatingId(appointment.id);
    setMessage(null);
    setError(null);
    try {
      await api.put(`/appointments/${appointment.id}/status`, {
        status,
        cancellationReason: status === 5 ? reasons[appointment.id]?.trim() || null : null,
      });
      setMessage(`Appointment status changed to ${statusMap[status].label}.`);
      setSelectedStatuses((current) => ({ ...current, [appointment.id]: '' }));
      await fetchAppointments();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to update appointment status.');
    } finally {
      setUpdatingId(null);
    }
  };

  return (
    <div className="page-enter admin-page-stack">
      <AdminPageHeader
        eyebrow="Scheduling oversight"
        title="Appointment Operations"
        description="Review clinic-wide scheduling and apply only valid status transitions when operational intervention is required."
      />

      {message && <div className="admin-notice admin-notice--success">{message}</div>}
      {error && <div className="admin-notice admin-notice--error">{error}</div>}

      <section className="card admin-resource-card">
        <div className="admin-resource-toolbar">
          <div><h2>Appointment register</h2><p>{totalCount} matching appointment{totalCount === 1 ? '' : 's'}</p></div>
          <div className="admin-filter-form">
            <select
              className="form-input"
              aria-label="Filter appointments by status"
              value={statusGroup}
              onChange={(event) => { setStatusGroup(event.target.value); setSkip(0); }}
            >
              <option value="all">All statuses</option>
              <option value="active">Active</option>
              <option value="upcoming">Upcoming</option>
              <option value="completed">Completed</option>
              <option value="cancelled">Closed / cancelled</option>
            </select>
            <input
              className="form-input"
              type="date"
              aria-label="Filter appointments by date"
              value={date}
              onChange={(event) => { setDate(event.target.value); setSkip(0); }}
            />
            {date && <button className="btn btn-ghost" type="button" onClick={() => { setDate(''); setSkip(0); }}>Clear date</button>}
          </div>
        </div>

        {loading ? (
          <div className="skeleton" style={{ height: 280 }} />
        ) : appointments.length === 0 ? (
          <div className="admin-empty-state"><CalendarIcon size={38} /><p>No appointments match these filters.</p></div>
        ) : (
          <div className="admin-resource-list" role="list">
            {appointments.map((appointment) => {
              const status = statusMap[appointment.status] ?? { label: 'Unknown', badge: 'badge-stone' };
              const transitions = allowedTransitions[appointment.status] ?? [];
              const selectedStatus = selectedStatuses[appointment.id] ?? '';
              return (
                <article className="admin-appointment-row" key={appointment.id} role="listitem">
                  <div className="admin-appointment-summary">
                    <div className="admin-date-tile">
                      <strong>{displayDate(appointment.appointmentDate)}</strong>
                      <span>{appointment.startTime.slice(0, 5)}–{appointment.endTime.slice(0, 5)}</span>
                    </div>
                    <div className="admin-primary-cell">
                      <strong>{appointment.patientName}</strong>
                      <span>with Dr. {appointment.doctorName}</span>
                    </div>
                    <span className={`badge ${status.badge}`}>{status.label}</span>
                  </div>

                  {transitions.length > 0 ? (
                    <div className="admin-status-editor">
                      <select
                        className="form-input"
                        aria-label={`New status for ${appointment.patientName}`}
                        value={selectedStatus}
                        onChange={(event) => setSelectedStatuses((current) => ({ ...current, [appointment.id]: event.target.value }))}
                      >
                        <option value="">Select next status</option>
                        {transitions.map((nextStatus) => <option key={nextStatus} value={nextStatus}>{statusMap[nextStatus].label}</option>)}
                      </select>
                      {selectedStatus === '5' && (
                        <input
                          className="form-input"
                          maxLength={500}
                          placeholder="Cancellation reason (optional)"
                          value={reasons[appointment.id] ?? ''}
                          onChange={(event) => setReasons((current) => ({ ...current, [appointment.id]: event.target.value }))}
                        />
                      )}
                      <button className="btn btn-secondary" type="button" disabled={!selectedStatus || updatingId === appointment.id} onClick={() => updateStatus(appointment)}>
                        {updatingId === appointment.id ? 'Updating…' : 'Apply status'}
                      </button>
                    </div>
                  ) : (
                    <p className="admin-terminal-note">This appointment is in a terminal state.</p>
                  )}
                </article>
              );
            })}
          </div>
        )}

        <PaginationControls skip={skip} take={PAGE_SIZE} totalCount={totalCount} onPageChange={setSkip} />
      </section>
    </div>
  );
}
