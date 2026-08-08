import { useCallback, useEffect, useRef, useState } from 'react';
import api from '../../api/client';
import type { AppointmentDto, ApiResponse, AvailableSlotDto } from '../../types';
import { CalendarIcon, ClockIcon } from '../../components/common/Icons';
import PaginationControls from '../../components/common/PaginationControls';
import { getClinicDateString } from '../../utils/clinicTime';

const PAGE_SIZE = 20;

export default function MyAppointments() {
  const [appointments, setAppointments] = useState<AppointmentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<'all' | 'upcoming' | 'completed' | 'cancelled'>('all');
  const [cancellingId, setCancellingId] = useState<string | null>(null);
  const [skip, setSkip] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [reschedulingId, setReschedulingId] = useState<string | null>(null);
  const [rescheduleDate, setRescheduleDate] = useState(getClinicDateString());
  const [rescheduleSlots, setRescheduleSlots] = useState<AvailableSlotDto[]>([]);
  const [selectedSlot, setSelectedSlot] = useState<AvailableSlotDto | null>(null);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [savingReschedule, setSavingReschedule] = useState(false);
  const [rescheduleError, setRescheduleError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const slotRequest = useRef<AbortController | null>(null);

  const fetchAppointments = useCallback(async () => {
    try {
      setLoading(true);
      const res = await api.get<ApiResponse<AppointmentDto[]>>(
        `/appointments/my-appointments?statusGroup=${filter}&skip=${skip}&take=${PAGE_SIZE}`,
      );
      setAppointments(res.data.data || []);
      setTotalCount(res.data.pagination?.totalCount ?? res.data.data?.length ?? 0);
    } catch (error) {
      console.error('Failed to fetch appointments', error);
    } finally {
      setLoading(false);
    }
  }, [filter, skip]);

  useEffect(() => {
    fetchAppointments();
    return () => slotRequest.current?.abort();
  }, [fetchAppointments]);

  const fetchRescheduleSlots = async (doctorId: string, date: string) => {
    slotRequest.current?.abort();
    const controller = new AbortController();
    slotRequest.current = controller;
    try {
      setLoadingSlots(true);
      setRescheduleError(null);
      const response = await api.get<ApiResponse<AvailableSlotDto[]>>(
        `/doctors/${doctorId}/slots?date=${date}`,
        { signal: controller.signal },
      );
      setRescheduleSlots((response.data.data || []).filter((slot) => slot.isAvailable));
    } catch (error) {
      if (controller.signal.aborted) return;
      console.error('Failed to load rescheduling slots', error);
      setRescheduleSlots([]);
      setRescheduleError('Available times could not be loaded. Please try another date.');
    } finally {
      if (!controller.signal.aborted) setLoadingSlots(false);
    }
  };

  const openReschedule = (appointment: AppointmentDto) => {
    const today = getClinicDateString();
    const appointmentDate = appointment.appointmentDate.slice(0, 10);
    const initialDate = appointmentDate >= today ? appointmentDate : today;
    setReschedulingId(appointment.id);
    setRescheduleDate(initialDate);
    setSelectedSlot(null);
    setRescheduleSlots([]);
    setRescheduleError(null);
    setSuccessMessage(null);
    void fetchRescheduleSlots(appointment.doctorProfileId, initialDate);
  };

  const closeReschedule = () => {
    slotRequest.current?.abort();
    setReschedulingId(null);
    setSelectedSlot(null);
    setRescheduleSlots([]);
    setRescheduleError(null);
  };

  const handleReschedule = async (appointment: AppointmentDto) => {
    if (!selectedSlot) return;
    try {
      setSavingReschedule(true);
      setRescheduleError(null);
      await api.put(`/appointments/${appointment.id}/reschedule`, {
        newAppointmentDate: rescheduleDate,
        newStartTime: selectedSlot.startTime,
        newEndTime: selectedSlot.endTime,
      });
      closeReschedule();
      setSuccessMessage('Appointment rescheduled and sent to the doctor for review.');
      await fetchAppointments();
    } catch (error: any) {
      setRescheduleError(error.response?.data?.message || 'Failed to reschedule appointment.');
    } finally {
      setSavingReschedule(false);
    }
  };

  const handleCancel = async (id: string) => {
    const reason = prompt('Please enter a cancellation reason:');
    if (!reason) return;
    setCancellingId(id);
    try {
      await api.put(`/appointments/${id}/cancel`, { cancellationReason: reason });
      setSuccessMessage('Appointment cancelled.');
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

  return (
    <div className="page-enter" style={{ display: 'flex', flexDirection: 'column', gap: 28 }}>
      <div>
        <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '2rem', fontWeight: 700 }}>
          My Appointments
        </h1>
        <p style={{ color: 'var(--text-secondary)' }}>Track and manage your upcoming and past medical visits.</p>
      </div>

      {successMessage && (
        <div className="alert" style={{ color: 'var(--color-teal-800)', background: 'var(--color-teal-50)', borderColor: 'var(--color-teal-200)' }}>
          {successMessage}
        </div>
      )}

      <div className="appointment-filter-tabs">
        {(['all', 'upcoming', 'completed', 'cancelled'] as const).map((tab) => (
          <button
            key={tab}
            className={`btn ${filter === tab ? 'btn-primary' : 'btn-ghost'}`}
            style={{ textTransform: 'capitalize', fontSize: '0.875rem' }}
            onClick={() => { setFilter(tab); setSkip(0); closeReschedule(); }}
          >
            {tab}
          </button>
        ))}
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
        {loading ? (
          <div className="skeleton" style={{ height: 140 }} />
        ) : appointments.length === 0 ? (
          <div className="card" style={{ padding: 48, textAlign: 'center', color: 'var(--text-secondary)' }}>
            No appointments found for this filter.
          </div>
        ) : (
          appointments.map((appointment) => {
            const status = statusMap[appointment.status] || { label: 'Unknown', badge: 'badge-stone' };
            const canManage = appointment.status === 0 || appointment.status === 1;
            const isEditing = reschedulingId === appointment.id;
            return (
              <div key={appointment.id} className="card appointment-card">
                <div className="appointment-card-main">
                  <div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
                      <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.2rem', fontWeight: 700 }}>
                        {appointment.doctorName}
                      </h3>
                      <span className={`badge ${status.badge}`}>{status.label}</span>
                    </div>

                    <div className="appointment-meta">
                      <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                        <CalendarIcon size={15} color="var(--accent)" />
                        {new Date(appointment.appointmentDate).toLocaleDateString(undefined, {
                          weekday: 'short', month: 'short', day: 'numeric', year: 'numeric',
                        })}
                      </span>
                      <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                        <ClockIcon size={15} color="var(--accent)" />
                        {appointment.startTime} - {appointment.endTime}
                      </span>
                    </div>

                    {appointment.notes && (
                      <div style={{ fontSize: '0.85rem', color: 'var(--text-primary)', marginTop: 6, fontStyle: 'italic' }}>
                        “{appointment.notes}”
                      </div>
                    )}

                    {appointment.cancellationReason && (
                      <div style={{ fontSize: '0.8125rem', color: 'var(--color-rose-600)', marginTop: 4 }}>
                        Reason for cancellation: {appointment.cancellationReason}
                      </div>
                    )}
                  </div>

                  {canManage && (
                    <div className="appointment-actions">
                      <button
                        type="button"
                        className="btn btn-secondary"
                        onClick={() => isEditing ? closeReschedule() : openReschedule(appointment)}
                      >
                        {isEditing ? 'Close' : 'Reschedule'}
                      </button>
                      <button
                        type="button"
                        className="btn btn-danger"
                        disabled={cancellingId === appointment.id}
                        onClick={() => handleCancel(appointment.id)}
                      >
                        Cancel Visit
                      </button>
                    </div>
                  )}
                </div>

                {isEditing && (
                  <div className="reschedule-editor">
                    <div className="reschedule-editor-copy">
                      <h4>Choose a new available time</h4>
                      <p>The appointment returns to pending status so the doctor can review the new request.</p>
                    </div>

                    <div className="form-group" style={{ maxWidth: 280 }}>
                      <label className="form-label">New date</label>
                      <input
                        className="form-input"
                        type="date"
                        min={getClinicDateString()}
                        value={rescheduleDate}
                        onChange={(event) => {
                          const date = event.target.value;
                          setRescheduleDate(date);
                          setSelectedSlot(null);
                          void fetchRescheduleSlots(appointment.doctorProfileId, date);
                        }}
                      />
                    </div>

                    <div>
                      <span className="form-label" style={{ display: 'block', marginBottom: 8 }}>Available times</span>
                      <div className="reschedule-slot-grid">
                        {loadingSlots ? (
                          <div className="skeleton" style={{ height: 40, gridColumn: '1 / -1' }} />
                        ) : rescheduleSlots.length === 0 ? (
                          <span style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', gridColumn: '1 / -1' }}>
                            No open times on this date.
                          </span>
                        ) : (
                          rescheduleSlots.map((slot) => {
                            const chosen = selectedSlot?.startTime === slot.startTime &&
                              selectedSlot?.endTime === slot.endTime;
                            return (
                              <button
                                key={`${slot.startTime}-${slot.endTime}`}
                                type="button"
                                className={`btn ${chosen ? 'btn-primary' : 'btn-secondary'}`}
                                onClick={() => setSelectedSlot(slot)}
                              >
                                {slot.startTime} - {slot.endTime}
                              </button>
                            );
                          })
                        )}
                      </div>
                    </div>

                    {rescheduleError && <div className="alert alert-error">{rescheduleError}</div>}

                    <div className="reschedule-editor-actions">
                      <button type="button" className="btn btn-ghost" onClick={closeReschedule}>Keep current time</button>
                      <button
                        type="button"
                        className="btn btn-primary"
                        disabled={!selectedSlot || savingReschedule}
                        onClick={() => handleReschedule(appointment)}
                      >
                        {savingReschedule ? 'Rescheduling…' : 'Confirm new time'}
                      </button>
                    </div>
                  </div>
                )}
              </div>
            );
          })
        )}
        <PaginationControls
          skip={skip}
          take={PAGE_SIZE}
          totalCount={totalCount}
          onPageChange={(nextSkip) => { setSkip(nextSkip); closeReschedule(); }}
        />
      </div>
    </div>
  );
}
