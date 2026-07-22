import { useEffect, useState } from 'react';
import api from '../../api/client';
import type { DoctorDto, SpecialtyDto, AvailableSlotDto, ApiResponse } from '../../types';

export default function FindDoctors() {
  const [doctors, setDoctors] = useState<DoctorDto[]>([]);
  const [specialties, setSpecialties] = useState<SpecialtyDto[]>([]);
  const [selectedSpecialty, setSelectedSpecialty] = useState<string>('');
  const [loading, setLoading] = useState(true);
  const [selectedDoctor, setSelectedDoctor] = useState<DoctorDto | null>(null);
  const [slots, setSlots] = useState<AvailableSlotDto[]>([]);
  const [selectedDate, setSelectedDate] = useState<string>(new Date().toISOString().split('T')[0]);
  const [bookingSlot, setBookingSlot] = useState<AvailableSlotDto | null>(null);
  const [notes, setNotes] = useState('');
  const [bookingSuccess, setBookingSuccess] = useState(false);
  const [bookingError, setBookingError] = useState<string | null>(null);

  useEffect(() => {
    async function loadData() {
      try {
        const [docRes, specRes] = await Promise.all([
          api.get<ApiResponse<DoctorDto[]>>('/doctors'),
          api.get<ApiResponse<SpecialtyDto[]>>('/specialties'),
        ]);
        setDoctors(docRes.data.data || []);
        setSpecialties(specRes.data.data || []);
      } catch (e) {
        console.error('Failed to load doctors', e);
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, []);

  const handleSelectDoctor = async (doc: DoctorDto) => {
    setSelectedDoctor(doc);
    setBookingSlot(null);
    setBookingSuccess(false);
    setBookingError(null);
    await fetchSlots(doc.id, selectedDate);
  };

  const fetchSlots = async (doctorId: string, date: string) => {
    try {
      const res = await api.get<ApiResponse<AvailableSlotDto[]>>(
        `/doctors/${doctorId}/available-slots?date=${date}`
      );
      setSlots(res.data.data || []);
    } catch (e) {
      console.error('Failed to fetch slots', e);
      setSlots([]);
    }
  };

  const handleBook = async () => {
    if (!selectedDoctor || !bookingSlot) return;
    setBookingError(null);
    try {
      await api.post('/appointments', {
        doctorProfileId: selectedDoctor.id,
        appointmentDate: selectedDate,
        startTime: bookingSlot.startTime,
        endTime: bookingSlot.endTime,
        notes,
      });
      setBookingSuccess(true);
      setBookingSlot(null);
      setNotes('');
      fetchSlots(selectedDoctor.id, selectedDate);
    } catch (err: any) {
      setBookingError(err.response?.data?.message || 'Failed to book appointment.');
    }
  };

  const filteredDoctors = selectedSpecialty
    ? doctors.filter((d) => d.specialties?.some((s) => s.id === selectedSpecialty))
    : doctors;

  return (
    <div className="page-enter" style={{ display: 'flex', flexDirection: 'column', gap: 28 }}>
      <div>
        <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '2rem', fontWeight: 700 }}>
          Find a Specialist
        </h1>
        <p style={{ color: 'var(--text-secondary)' }}>Browse top verified doctors and book your consultation.</p>
      </div>

      {/* Filter bar */}
      <div className="card" style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 16 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
          <div>
            <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.125rem', fontWeight: 700 }}>
              Clinical Specialty Filters
            </h2>
            <p style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)' }}>
              Filter verified doctors by their specialized medical practice domain.
            </p>
          </div>

          {selectedSpecialty && (
            <button
              className="btn btn-ghost"
              onClick={() => setSelectedSpecialty('')}
              style={{ fontSize: '0.8125rem', color: 'var(--accent)' }}
            >
              Clear Filter
            </button>
          )}
        </div>

        {/* Quick specialty chip pills */}
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginTop: 4 }}>
          <button
            type="button"
            onClick={() => setSelectedSpecialty('')}
            className={`btn ${selectedSpecialty === '' ? 'btn-primary' : 'btn-secondary'}`}
            style={{ padding: '6px 14px', borderRadius: 20, fontSize: '0.8125rem', fontWeight: 500 }}
          >
            All Specialties ({doctors.length})
          </button>
          {specialties.map((s) => {
            const isSelected = selectedSpecialty === s.id;
            const docCount = doctors.filter(d => d.specialties?.some(sp => sp.id === s.id)).length;
            return (
              <button
                key={s.id}
                type="button"
                onClick={() => setSelectedSpecialty(isSelected ? '' : s.id)}
                style={{
                  padding: '6px 14px',
                  borderRadius: 20,
                  fontSize: '0.8125rem',
                  fontWeight: 500,
                  border: isSelected ? '1.5px solid var(--accent)' : '1px solid var(--border-default)',
                  background: isSelected ? 'var(--accent)' : 'var(--bg-surface)',
                  color: isSelected ? 'white' : 'var(--text-primary)',
                  cursor: 'pointer',
                  transition: 'all 120ms ease'
                }}
              >
                {s.name} {docCount > 0 ? `(${docCount})` : ''}
              </button>
            );
          })}
        </div>
      </div>

      {/* Doctor list & slot drawer grid */}
      <div style={{ display: 'grid', gridTemplateColumns: selectedDoctor ? '1fr 1fr' : '1fr', gap: 28 }}>
        {/* Doctor Cards */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          {loading ? (
            <div className="skeleton" style={{ height: 160 }} />
          ) : filteredDoctors.length === 0 ? (
            <div className="card" style={{ padding: 40, textAlign: 'center', color: 'var(--text-secondary)' }}>
              No doctors found matching your criteria.
            </div>
          ) : (
            filteredDoctors.map((doc) => (
              <div
                key={doc.id}
                className="card"
                style={{
                  padding: 24,
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  border: selectedDoctor?.id === doc.id ? '2px solid var(--accent)' : '1px solid var(--border-default)',
                }}
              >
                <div style={{ display: 'flex', gap: 16, alignItems: 'center' }}>
                  {doc.profilePictureUrl && (
                    <img
                      src={doc.profilePictureUrl}
                      alt={`Dr. ${doc.firstName}`}
                      style={{ width: 64, height: 64, borderRadius: 99, objectFit: 'cover', border: '2px solid var(--accent)' }}
                      onError={(e) => { (e.target as HTMLElement).style.display = 'none'; }}
                    />
                  )}
                  <div>
                    <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700 }}>
                      Dr. {doc.firstName} {doc.lastName}
                    </h3>
                  <div style={{ display: 'flex', gap: 6, margin: '6px 0 10px' }}>
                    {doc.specialties?.map((s) => (
                      <span key={s.id} className="badge badge-teal">{s.name}</span>
                    ))}
                  </div>
                  <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', maxWidth: 400 }}>
                    {doc.bio || 'Experienced medical professional.'}
                  </p>
                  <div style={{ fontSize: '0.875rem', fontWeight: 600, marginTop: 8, color: 'var(--color-teal-800)' }}>
                    Fee: {doc.consultationFee} JOD
                  </div>
                </div>
              </div>

                <button
                  className="btn btn-primary"
                  onClick={() => handleSelectDoctor(doc)}
                >
                  View Schedule
                </button>
              </div>
            ))
          )}
        </div>

        {/* Slot booking drawer */}
        {selectedDoctor && (
          <div className="card" style={{ padding: 28, position: 'sticky', top: 24 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
              <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700 }}>
                Schedule with Dr. {selectedDoctor.lastName}
              </h2>
              <button className="btn btn-ghost" onClick={() => setSelectedDoctor(null)}>✕</button>
            </div>

            {bookingSuccess && (
              <div style={{ padding: 12, background: 'var(--color-teal-50)', color: 'var(--color-teal-800)', borderRadius: 'var(--radius-md)', marginBottom: 16 }}>
                ✅ Appointment request submitted successfully!
              </div>
            )}
            {bookingError && (
              <div style={{ padding: 12, background: 'var(--color-rose-100)', color: 'var(--color-rose-600)', borderRadius: 'var(--radius-md)', marginBottom: 16 }}>
                {bookingError}
              </div>
            )}

            <div className="form-group" style={{ marginBottom: 20 }}>
              <label className="form-label">Select Date</label>
              <input
                type="date"
                className="form-input"
                value={selectedDate}
                min={new Date().toISOString().split('T')[0]}
                onChange={(e) => {
                  setSelectedDate(e.target.value);
                  fetchSlots(selectedDoctor.id, e.target.value);
                }}
              />
            </div>

            <label className="form-label" style={{ marginBottom: 8, display: 'block' }}>Available Slots</label>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10, marginBottom: 20 }}>
              {slots.length === 0 ? (
                <span style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', gridColumn: '1 / -1' }}>
                  No slots available for this date.
                </span>
              ) : (
                slots.map((slot, idx) => (
                  <button
                    key={idx}
                    type="button"
                    disabled={!slot.isAvailable}
                    className={`btn ${bookingSlot === slot ? 'btn-primary' : 'btn-secondary'}`}
                    style={{ fontSize: '0.8125rem', padding: '8px 12px' }}
                    onClick={() => setBookingSlot(slot)}
                  >
                    {slot.startTime} - {slot.endTime}
                  </button>
                ))
              )}
            </div>

            {bookingSlot && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                <div className="form-group">
                  <label className="form-label">Reason for visit (optional)</label>
                  <textarea
                    className="form-input"
                    rows={3}
                    placeholder="Briefly describe your symptoms or reason for visit…"
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                  />
                </div>

                <button className="btn btn-primary" onClick={handleBook} style={{ width: '100%', padding: '12px' }}>
                  Confirm Booking ({bookingSlot.startTime})
                </button>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
