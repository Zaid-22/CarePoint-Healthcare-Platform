import { useEffect, useState } from 'react';
import api from '../../api/client';
import type { DoctorDto, SpecialtyDto, DoctorAvailabilityDto, UpdateDoctorRequest, ApiResponse } from '../../types';
import doctorPortrait from '../../assets/doctor_portrait.png';
import { DoctorIcon, CheckIcon, MailIcon, ActivityIcon, UserIcon, ClockIcon, FolderIcon, EyeIcon, CheckCircleIcon, XCircleIcon, XIcon, PlusIcon } from '../../components/common/Icons';

const AVATAR_PRESETS = [
  'https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&w=300&q=80',
  'https://images.unsplash.com/photo-1559839734-2b71ea197ec2?auto=format&fit=crop&w=300&q=80',
  'https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?auto=format&fit=crop&w=300&q=80',
  'https://images.unsplash.com/photo-1594824813566-88855ce78905?auto=format&fit=crop&w=300&q=80',
];

const DAYS_OF_WEEK = [
  { value: 0, label: 'Sunday' },
  { value: 1, label: 'Monday' },
  { value: 2, label: 'Tuesday' },
  { value: 3, label: 'Wednesday' },
  { value: 4, label: 'Thursday' },
  { value: 5, label: 'Friday' },
  { value: 6, label: 'Saturday' },
];

export default function DoctorProfilePage() {
  const [profile, setProfile] = useState<DoctorDto | null>(null);
  const [allSpecialties, setAllSpecialties] = useState<SpecialtyDto[]>([]);
  const [selectedSpecialtyIds, setSelectedSpecialtyIds] = useState<string[]>([]);

  const [bio, setBio] = useState('');
  const [consultationFee, setConsultationFee] = useState<number | ''>('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [gender, setGender] = useState('');
  const [profilePictureUrl, setProfilePictureUrl] = useState('');

  // Schedule / Availability state
  const [availabilities, setAvailabilities] = useState<DoctorAvailabilityDto[]>([]);
  const [shiftDay, setShiftDay] = useState<number>(0);
  const [shiftStart, setShiftStart] = useState('09:00');
  const [shiftEnd, setShiftEnd] = useState('17:00');
  const [shiftDuration, setShiftDuration] = useState<number>(30);
  const [addingShift, setAddingShift] = useState(false);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [uploadingAvatar, setUploadingAvatar] = useState(false);
  const [activeTab, setActiveTab] = useState<'practice' | 'specialties' | 'schedule' | 'avatar' | 'preview'>('practice');
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const fetchAvailability = async (doctorId: string) => {
    try {
      const res = await api.get<ApiResponse<DoctorAvailabilityDto[]>>(`/doctors/${doctorId}/availability`);
      setAvailabilities(res.data.data || []);
    } catch (e) {
      console.error('Failed to load doctor availability', e);
    }
  };

  useEffect(() => {
    async function loadData() {
      try {
        setLoading(true);
        const [profileRes, specialtiesRes] = await Promise.all([
          api.get<ApiResponse<DoctorDto>>('/doctors/me'),
          api.get<ApiResponse<SpecialtyDto[]>>('/specialties'),
        ]);

        const doc = profileRes.data.data;
        if (doc) {
          setProfile(doc);
          setBio(doc.bio || '');
          setConsultationFee(doc.consultationFee ?? 0);
          setPhoneNumber(doc.phoneNumber || '');
          setGender(doc.gender || '');
          setProfilePictureUrl(doc.profilePictureUrl || '');
          setSelectedSpecialtyIds(doc.specialties ? doc.specialties.map((s) => s.id) : []);
          fetchAvailability(doc.id);
        }

        setAllSpecialties(specialtiesRes.data.data || []);
      } catch (err: any) {
        console.error('Failed to load doctor profile', err);
        setErrorMsg(err.response?.data?.message || 'Failed to load profile details.');
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, []);

  const handleAddShift = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!profile) return;
    setSuccessMsg(null);
    setErrorMsg(null);

    try {
      setAddingShift(true);
      await api.post(`/doctors/${profile.id}/availability`, {
        dayOfWeek: Number(shiftDay),
        startTime: shiftStart.length === 5 ? `${shiftStart}:00` : shiftStart,
        endTime: shiftEnd.length === 5 ? `${shiftEnd}:00` : shiftEnd,
        slotDurationMinutes: Number(shiftDuration),
      });
      fetchAvailability(profile.id);
      setSuccessMsg('Working hours shift added successfully!');
    } catch (err: any) {
      setErrorMsg(err.response?.data?.message || 'Failed to add working shift.');
    } finally {
      setAddingShift(false);
    }
  };

  const handleDeleteShift = async (slotId: string) => {
    if (!profile) return;
    try {
      await api.delete(`/doctors/${profile.id}/availability/${slotId}`);
      setAvailabilities((prev) => prev.filter((s) => s.id !== slotId));
      setSuccessMsg('Working shift removed.');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to remove shift.');
    }
  };

  const toggleSpecialty = (id: string) => {
    setSelectedSpecialtyIds((prev) =>
      prev.includes(id) ? prev.filter((sId) => sId !== id) : [...prev, id]
    );
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setSuccessMsg(null);
    setErrorMsg(null);

    try {
      const validSpecialtyIds = selectedSpecialtyIds.filter(
        (id) => typeof id === 'string' && id.length === 36
      );

      const payload: UpdateDoctorRequest = {
        bio: bio || '',
        consultationFee: consultationFee === '' || isNaN(Number(consultationFee)) ? 0 : Number(consultationFee),
        phoneNumber: phoneNumber || '',
        gender: gender || '',
        profilePictureUrl: profilePictureUrl || '',
        specialtyIds: validSpecialtyIds,
      };

      const res = await api.put<ApiResponse<DoctorDto>>('/doctors/me', payload);
      if (res.data.data) {
        setProfile(res.data.data);
        setSuccessMsg('Practitioner profile updated successfully!');
      }
    } catch (err: any) {
      console.error('Failed to update profile', err);
      setErrorMsg(err.response?.data?.message || 'Failed to update profile.');
    } finally {
      setSaving(false);
    }
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > 1024 * 1024) {
      setErrorMsg('Profile images must be 1 MB or smaller.');
      return;
    }

    if (!['image/jpeg', 'image/png'].includes(file.type)) {
      setErrorMsg('Only JPG and PNG profile images are supported.');
      return;
    }

    try {
      setUploadingAvatar(true);
      setErrorMsg(null);
      setSuccessMsg(null);
      const form = new FormData();
      form.append('file', file);
      const response = await api.post<ApiResponse<DoctorDto>>('/doctors/me/avatar', form);
      if (response.data.data) {
        setProfile(response.data.data);
        setProfilePictureUrl(response.data.data.profilePictureUrl || '');
        setSuccessMsg('Profile image uploaded securely.');
      }
    } catch (err: any) {
      setErrorMsg(err.response?.data?.message || 'Failed to upload profile image.');
    } finally {
      setUploadingAvatar(false);
      e.target.value = '';
    }
  };

  if (loading) {
    return (
      <div className="page-enter" style={{ display: 'flex', flexDirection: 'column', gap: 24, maxWidth: 1040, margin: '0 auto' }}>
        <div className="skeleton" style={{ height: 160 }} />
        <div className="skeleton" style={{ height: 480 }} />
      </div>
    );
  }

  const statusBadge = profile?.approvalStatus === 1 ? (
    <span className="badge badge-teal-hero" style={{ padding: '6px 14px', fontSize: '0.8125rem' }}>
      <CheckCircleIcon size={15} color="white" /> Verified Practitioner
    </span>
  ) : profile?.approvalStatus === 2 ? (
    <span className="badge badge-rose-hero" style={{ padding: '6px 14px', fontSize: '0.8125rem' }}>
      <XCircleIcon size={15} color="white" /> Rejected
    </span>
  ) : (
    <span className="badge badge-amber-hero" style={{ padding: '6px 14px', fontSize: '0.8125rem' }}>
      <ClockIcon size={15} color="#451a03" /> Pending Approval
    </span>
  );

  const selectedSpecialtiesList = allSpecialties.filter((s) => selectedSpecialtyIds.includes(s.id));

  return (
    <div className="page-enter" style={{ maxWidth: 1040, margin: '0 auto', display: 'flex', flexDirection: 'column', gap: 32 }}>
      {/* Premium Hero Banner */}
      <div className="hero-mesh-bg card" style={{
        padding: '36px 40px',
        background: 'linear-gradient(135deg, var(--color-teal-950) 0%, var(--color-teal-900) 60%, #064e4b 100%)',
        color: 'white',
        borderRadius: 'var(--radius-xl)',
        boxShadow: '0 20px 40px -15px rgba(2, 15, 14, 0.4)',
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 24 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 24 }}>
            <div style={{ position: 'relative' }}>
              <img
                src={profilePictureUrl && profilePictureUrl.trim() !== '' ? profilePictureUrl : doctorPortrait}
                alt="Doctor Avatar"
                style={{
                  width: 100,
                  height: 100,
                  borderRadius: 99,
                  objectFit: 'cover',
                  border: '3.5px solid rgba(255, 255, 255, 0.3)',
                  boxShadow: '0 10px 25px rgba(0, 0, 0, 0.3)',
                }}
                onError={(e) => { (e.target as HTMLImageElement).src = doctorPortrait; }}
              />
              <div style={{
                position: 'absolute',
                bottom: 4,
                right: 4,
                width: 18,
                height: 18,
                borderRadius: 99,
                background: '#10b981',
                border: '3px solid var(--color-teal-950)',
              }} />
            </div>

            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 8 }}>
                {statusBadge}
                <span style={{ fontSize: '0.8125rem', color: 'rgba(255, 255, 255, 0.75)', display: 'flex', alignItems: 'center', gap: 6 }}>
                  <MailIcon size={14} color="var(--color-teal-200)" /> {profile?.email}
                </span>
              </div>
              <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 700, color: 'white', margin: 0, lineHeight: 1.2 }}>
                {profile?.firstName ? `Dr. ${profile.firstName} ${profile.lastName}` : 'Practitioner Profile'}
              </h1>
              <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginTop: 10 }}>
                {selectedSpecialtiesList.length > 0 ? (
                  selectedSpecialtiesList.map((s) => (
                    <span key={s.id} style={{
                      padding: '3px 10px',
                      borderRadius: 99,
                      background: 'rgba(255, 255, 255, 0.12)',
                      backdropFilter: 'blur(8px)',
                      color: 'var(--color-teal-200)',
                      fontSize: '0.78125rem',
                      fontWeight: 500,
                      border: '1px solid rgba(255, 255, 255, 0.15)',
                    }}>
                      {s.name}
                    </span>
                  ))
                ) : (
                  <span style={{ color: 'rgba(255, 255, 255, 0.6)', fontSize: '0.875rem' }}>No specialties selected</span>
                )}
              </div>
            </div>
          </div>

          {/* Quick Metrics Bar */}
          <div style={{
            display: 'flex',
            gap: 20,
            background: 'rgba(255, 255, 255, 0.08)',
            backdropFilter: 'blur(16px)',
            padding: '16px 24px',
            borderRadius: 'var(--radius-lg)',
            border: '1px solid rgba(255, 255, 255, 0.15)',
          }}>
            <div>
              <div style={{ fontSize: '0.75rem', color: 'rgba(255, 255, 255, 0.7)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Consultation Fee</div>
              <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--color-teal-200)' }}>
                ${consultationFee || '0'}
              </div>
            </div>
            <div style={{ width: 1, height: 36, background: 'rgba(255, 255, 255, 0.18)' }} />
            <div>
              <div style={{ fontSize: '0.75rem', color: 'rgba(255, 255, 255, 0.7)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Specialties</div>
              <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'white' }}>
                {selectedSpecialtyIds.length}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Success / Error Banners */}
      {successMsg && (
        <div style={{ padding: '14px 18px', background: 'var(--color-teal-100)', color: 'var(--color-teal-800)', borderRadius: 'var(--radius-md)', display: 'flex', alignItems: 'center', gap: 10, fontSize: '0.9375rem', fontWeight: 500 }}>
          <CheckIcon size={18} color="currentColor" /> {successMsg}
        </div>
      )}

      {errorMsg && (
        <div style={{ padding: '14px 18px', background: 'var(--color-rose-100)', color: 'var(--color-rose-700)', borderRadius: 'var(--radius-md)', fontSize: '0.9375rem' }}>
          {errorMsg}
        </div>
      )}

      {/* Navigation Tabs */}
      <div style={{ display: 'flex', gap: 12, borderBottom: '1px solid var(--border-default)', paddingBottom: 12, overflowX: 'auto' }}>
        <button
          type="button"
          className={`btn ${activeTab === 'practice' ? 'btn-primary' : 'btn-ghost'}`}
          onClick={() => setActiveTab('practice')}
          style={{ gap: 8 }}
        >
          <DoctorIcon size={18} /> Practice Details
        </button>
        <button
          type="button"
          className={`btn ${activeTab === 'specialties' ? 'btn-primary' : 'btn-ghost'}`}
          onClick={() => setActiveTab('specialties')}
          style={{ gap: 8 }}
        >
          <ActivityIcon size={18} /> Specialties ({selectedSpecialtyIds.length})
        </button>
        <button
          type="button"
          className={`btn ${activeTab === 'schedule' ? 'btn-primary' : 'btn-ghost'}`}
          onClick={() => setActiveTab('schedule')}
          style={{ gap: 8 }}
        >
          <ClockIcon size={18} /> Working Hours & Availability ({availabilities.length})
        </button>
        <button
          type="button"
          className={`btn ${activeTab === 'avatar' ? 'btn-primary' : 'btn-ghost'}`}
          onClick={() => setActiveTab('avatar')}
          style={{ gap: 8 }}
        >
          <UserIcon size={18} /> Photo & Branding
        </button>
        <button
          type="button"
          className={`btn ${activeTab === 'preview' ? 'btn-primary' : 'btn-ghost'}`}
          onClick={() => setActiveTab('preview')}
          style={{ gap: 8 }}
        >
          <EyeIcon size={18} /> Patient View Preview
        </button>
      </div>

      {/* Main Content Form */}
      <form onSubmit={handleSave} style={{ display: 'flex', flexDirection: 'column', gap: 28 }}>
        {/* Practice Details Tab */}
        {activeTab === 'practice' && (
          <div className="card glass-card" style={{ padding: 32, display: 'flex', flexDirection: 'column', gap: 24 }}>
            <div>
              <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700 }}>
                Practice & Clinical Information
              </h2>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                Set your consultation pricing, contact phone, and professional bio for patient discovery.
              </p>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>
              <div className="form-group">
                <label className="form-label">Consultation Fee (JOD)</label>
                <input
                  className="form-input"
                  type="number"
                  min="0"
                  step="0.01"
                  placeholder="e.g. 75.00"
                  value={consultationFee}
                  onChange={(e) => setConsultationFee(e.target.value === '' ? '' : Number(e.target.value))}
                />
              </div>

              <div className="form-group">
                <label className="form-label">Practice Direct Phone Number</label>
                <input
                  className="form-input"
                  type="tel"
                  maxLength={20}
                  placeholder="+962 7 9100 1111"
                  value={phoneNumber}
                  onChange={(e) => setPhoneNumber(e.target.value)}
                />
              </div>
            </div>

            <div className="form-group">
              <label className="form-label">Gender</label>
              <select
                className="form-input"
                value={gender}
                onChange={(e) => setGender(e.target.value)}
              >
                <option value="">Select gender…</option>
                <option value="Male">Male</option>
                <option value="Female">Female</option>
                <option value="Other">Other</option>
              </select>
            </div>

            <div className="form-group">
              <label className="form-label">Professional Biography</label>
              <textarea
                className="form-input"
                rows={5}
                maxLength={2000}
                placeholder="Detail your medical degree, hospital affiliations, specialized procedures, and patient care philosophy..."
                value={bio}
                onChange={(e) => setBio(e.target.value)}
              />
            </div>
          </div>
        )}

        {/* Specialties Tab */}
        {activeTab === 'specialties' && (
          <div className="card glass-card" style={{ padding: 32, display: 'flex', flexDirection: 'column', gap: 24 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 16 }}>
              <div>
                <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700 }}>
                  Clinical Specialties
                </h2>
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                  Select your medical fields of expertise from the dropdown list below.
                </p>
              </div>
              <span className="badge badge-teal" style={{ fontSize: '0.875rem', padding: '6px 14px' }}>
                {selectedSpecialtyIds.length} Selected
              </span>
            </div>

            {/* Selected Specialties Active Badges */}
            {selectedSpecialtiesList.length > 0 && (
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, padding: '14px 18px', background: 'var(--bg-subtle)', borderRadius: 'var(--radius-md)', border: '1px solid var(--border-default)' }}>
                <span style={{ fontSize: '0.8125rem', fontWeight: 600, color: 'var(--text-secondary)', alignSelf: 'center', marginRight: 4 }}>Active Specialties:</span>
                {selectedSpecialtiesList.map((s) => (
                  <span
                    key={s.id}
                    className="badge badge-teal"
                    style={{ padding: '6px 12px', fontSize: '0.8125rem', gap: 8, display: 'inline-flex', alignItems: 'center' }}
                  >
                    {s.name}
                    <button
                      type="button"
                      onClick={() => toggleSpecialty(s.id)}
                      style={{ border: 'none', background: 'transparent', cursor: 'pointer', display: 'flex', alignItems: 'center', color: 'currentColor', padding: 0 }}
                    >
                      <XIcon size={12} />
                    </button>
                  </span>
                ))}
              </div>
            )}

            {/* Multi-Select Checkbox List Field */}
            <div className="form-group">
              <label className="form-label" style={{ fontWeight: 600 }}>Select Specialties (Click any item to toggle)</label>
              <div style={{
                border: '1.5px solid var(--border-default)',
                borderRadius: 'var(--radius-md)',
                background: 'var(--bg-surface)',
                maxHeight: 280,
                overflowY: 'auto',
                padding: 8,
                display: 'flex',
                flexDirection: 'column',
                gap: 4,
              }}>
                {allSpecialties.map((specialty) => {
                  const isSelected = selectedSpecialtyIds.includes(specialty.id);
                  return (
                    <button
                      key={specialty.id}
                      type="button"
                      onClick={() => toggleSpecialty(specialty.id)}
                      style={{
                        padding: '10px 14px',
                        borderRadius: 'var(--radius-sm)',
                        border: 'none',
                        background: isSelected ? 'var(--accent-light)' : 'transparent',
                        cursor: 'pointer',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                        textAlign: 'left',
                        width: '100%',
                        fontFamily: 'inherit',
                        transition: 'all 120ms ease',
                      }}
                    >
                      <span style={{ fontSize: '0.9375rem', fontWeight: isSelected ? 600 : 400, color: isSelected ? 'var(--accent-hover)' : 'var(--text-primary)' }}>
                        {specialty.name}
                        {specialty.description && (
                          <span style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', fontWeight: 400, marginLeft: 8 }}>
                            — {specialty.description}
                          </span>
                        )}
                      </span>
                      <input
                        type="checkbox"
                        checked={isSelected}
                        onChange={() => {}}
                        style={{ width: 18, height: 18, accentColor: 'var(--accent)', cursor: 'pointer', flexShrink: 0 }}
                      />
                    </button>
                  );
                })}
              </div>
            </div>
          </div>
        )}

        {/* Working Hours & Schedule Tab */}
        {activeTab === 'schedule' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
            {/* Add Shift Form */}
            <div className="card glass-card" style={{ padding: 32, display: 'flex', flexDirection: 'column', gap: 20 }}>
              <div>
                <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700 }}>
                  Configure Working Shift Hours
                </h2>
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                  Add your weekly consultation availability shifts. Patients can book appointments strictly during configured shifts.
                </p>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))', gap: 16, alignItems: 'flex-end' }}>
                <div className="form-group">
                  <label className="form-label">Day of Week</label>
                  <select
                    className="form-input"
                    value={shiftDay}
                    onChange={(e) => setShiftDay(Number(e.target.value))}
                  >
                    {DAYS_OF_WEEK.map((d) => (
                      <option key={d.value} value={d.value}>{d.label}</option>
                    ))}
                  </select>
                </div>

                <div className="form-group">
                  <label className="form-label">Start Time</label>
                  <input
                    className="form-input"
                    type="time"
                    value={shiftStart}
                    onChange={(e) => setShiftStart(e.target.value)}
                    required
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">End Time</label>
                  <input
                    className="form-input"
                    type="time"
                    value={shiftEnd}
                    onChange={(e) => setShiftEnd(e.target.value)}
                    required
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">Slot Duration</label>
                  <select
                    className="form-input"
                    value={shiftDuration}
                    onChange={(e) => setShiftDuration(Number(e.target.value))}
                  >
                    <option value={15}>15 Minutes</option>
                    <option value={20}>20 Minutes</option>
                    <option value={30}>30 Minutes</option>
                    <option value={45}>45 Minutes</option>
                    <option value={60}>60 Minutes</option>
                  </select>
                </div>

                <button
                  type="button"
                  className="btn btn-primary"
                  disabled={addingShift}
                  onClick={handleAddShift}
                  style={{ padding: '10px 20px', gap: 6, height: 42 }}
                >
                  <PlusIcon size={16} color="white" /> {addingShift ? 'Adding...' : 'Add Shift'}
                </button>
              </div>
            </div>

            {/* Active Schedules List */}
            <div className="card glass-card" style={{ padding: 32 }}>
              <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.125rem', fontWeight: 700, marginBottom: 16 }}>
                Active Consultation Schedules ({availabilities.length})
              </h3>

              {availabilities.length === 0 ? (
                <div style={{ padding: '36px 0', textAlign: 'center', color: 'var(--text-secondary)' }}>
                  <ClockIcon size={40} style={{ opacity: 0.3, marginBottom: 8 }} />
                  <p style={{ fontSize: '0.9375rem', fontWeight: 500 }}>No working shifts configured yet.</p>
                  <p style={{ fontSize: '0.8125rem', marginTop: 4 }}>Add a shift above so patients can book appointments with you.</p>
                </div>
              ) : (
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))', gap: 16 }}>
                  {availabilities.map((slot) => {
                    const dayName = DAYS_OF_WEEK.find((d) => d.value === slot.dayOfWeek)?.label || `Day ${slot.dayOfWeek}`;
                    return (
                      <div
                        key={slot.id}
                        style={{
                          padding: '16px 20px',
                          borderRadius: 'var(--radius-lg)',
                          border: '1px solid var(--border-default)',
                          background: 'var(--bg-surface)',
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'center',
                        }}
                      >
                        <div>
                          <div style={{ fontWeight: 700, fontSize: '0.9375rem', color: 'var(--text-primary)' }}>
                            {dayName}
                          </div>
                          <div style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', marginTop: 2, display: 'flex', alignItems: 'center', gap: 4 }}>
                            <ClockIcon size={14} color="var(--accent)" />
                            {slot.startTime} – {slot.endTime} ({slot.slotDurationMinutes} min)
                          </div>
                        </div>

                        <button
                          type="button"
                          className="btn btn-ghost"
                          onClick={() => handleDeleteShift(slot.id)}
                          style={{ padding: 6, color: 'var(--color-rose-600)' }}
                          title="Remove shift"
                        >
                          <XIcon size={16} color="currentColor" />
                        </button>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          </div>
        )}

        {/* Photo & Branding Tab */}
        {activeTab === 'avatar' && (
          <div className="card glass-card" style={{ padding: 32, display: 'flex', flexDirection: 'column', gap: 24 }}>
            <div>
              <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700 }}>
                Profile Photo & Branding
              </h2>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                High-quality portraits increase patient booking conversion rates.
              </p>
            </div>

            <div style={{ display: 'flex', gap: 32, alignItems: 'center', flexWrap: 'wrap' }}>
              <img
                src={profilePictureUrl || doctorPortrait}
                alt="Avatar Large"
                style={{
                  width: 140,
                  height: 140,
                  borderRadius: 99,
                  objectFit: 'cover',
                  border: '4px solid var(--accent)',
                  boxShadow: 'var(--shadow-md)',
                }}
                onError={(e) => { (e.target as HTMLImageElement).src = doctorPortrait; }}
              />

              <div style={{ flex: 1, minWidth: 280, display: 'flex', flexDirection: 'column', gap: 16 }}>
                <div className="form-group">
                  <label className="form-label">Upload New Photo</label>
                  <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
                    <label
                      className="btn btn-primary glow-btn"
                      style={{ cursor: uploadingAvatar ? 'wait' : 'pointer', display: 'inline-flex', alignItems: 'center', gap: 8, opacity: uploadingAvatar ? 0.65 : 1 }}
                    >
                      <FolderIcon size={18} /> {uploadingAvatar ? 'Uploading...' : 'Select Image File'}
                      <input
                        type="file"
                        accept="image/jpeg,image/png"
                        onChange={handleFileUpload}
                        disabled={uploadingAvatar}
                        style={{ display: 'none' }}
                      />
                    </label>

                    {profilePictureUrl && (
                      <button
                        type="button"
                        className="btn btn-ghost"
                        onClick={() => setProfilePictureUrl('')}
                        style={{ color: 'var(--color-rose-600)' }}
                      >
                        Reset Avatar
                      </button>
                    )}
                  </div>
                </div>

                <div>
                  <div style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', marginBottom: 8, fontWeight: 500 }}>
                    Sample Professional Presets:
                  </div>
                  <div style={{ display: 'flex', gap: 12 }}>
                    {AVATAR_PRESETS.map((preset, idx) => (
                      <img
                        key={idx}
                        src={preset}
                        alt={`Preset ${idx + 1}`}
                        onClick={() => setProfilePictureUrl(preset)}
                        style={{
                          width: 44,
                          height: 44,
                          borderRadius: 99,
                          objectFit: 'cover',
                          cursor: 'pointer',
                          border: profilePictureUrl === preset ? '3px solid var(--accent)' : '2px solid transparent',
                          transition: 'all 150ms ease',
                          boxShadow: 'var(--shadow-sm)',
                        }}
                      />
                    ))}
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Live Patient Card Preview Tab */}
        {activeTab === 'preview' && (
          <div className="card glass-card" style={{ padding: 32, display: 'flex', flexDirection: 'column', gap: 20 }}>
            <div>
              <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700 }}>
                Live Patient View Preview
              </h2>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                This is exactly how your card appears to patients on the platform search directory.
              </p>
            </div>

            <div style={{ display: 'flex', justifyContent: 'center', padding: '20px 0' }}>
              <div className="card glass-card-hover" style={{
                width: '100%',
                maxWidth: 480,
                padding: 24,
                borderRadius: 'var(--radius-xl)',
                border: '1px solid var(--border-default)',
                background: 'var(--bg-surface)',
                boxShadow: 'var(--shadow-md)',
              }}>
                <div style={{ display: 'flex', gap: 18, alignItems: 'center', marginBottom: 16 }}>
                  <img
                    src={profilePictureUrl || doctorPortrait}
                    alt="Doctor"
                    style={{ width: 72, height: 72, borderRadius: 99, objectFit: 'cover', border: '2px solid var(--accent)' }}
                    onError={(e) => { (e.target as HTMLImageElement).src = doctorPortrait; }}
                  />
                  <div>
                    <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700, margin: 0 }}>
                      {profile?.firstName ? `Dr. ${profile.firstName} ${profile.lastName}` : 'Dr. Practitioner'}
                    </h3>
                    <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', marginTop: 4 }}>
                      {selectedSpecialtiesList.slice(0, 2).map((s) => (
                        <span key={s.id} className="badge badge-teal">{s.name}</span>
                      ))}
                    </div>
                  </div>
                </div>

                <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: 16, lineHeight: 1.5 }}>
                  {bio || 'Board-certified specialist dedicated to providing high-quality patient care and preventative medical consultation.'}
                </p>

                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderTop: '1px solid var(--border-default)', paddingTop: 16 }}>
                  <div>
                    <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Consultation Fee</span>
                    <div style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--accent)' }}>
                      {consultationFee || 0} JOD
                    </div>
                  </div>
                  <button type="button" className="btn btn-primary glow-btn" style={{ padding: '8px 18px', fontSize: '0.875rem' }}>
                    Book Appointment
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Global Save Changes Button */}
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 12 }}>
          <button
            type="submit"
            className="btn btn-primary glow-btn"
            disabled={saving}
            style={{ padding: '12px 36px', fontSize: '1rem', fontWeight: 600 }}
          >
            {saving ? 'Saving changes...' : 'Save Profile Changes'}
          </button>
        </div>
      </form>
    </div>
  );
}
