import { useEffect, useState } from 'react';
import api from '../../api/client';
import type { PatientDto, UpdatePatientDto, ApiResponse } from '../../types';
import { UserIcon, PhoneIcon, MailIcon, ShieldIcon, CheckIcon, EditIcon } from '../../components/common/Icons';

export default function PatientProfilePage() {
  const [profile, setProfile] = useState<PatientDto | null>(null);
  const [dateOfBirth, setDateOfBirth] = useState('');
  const [gender, setGender] = useState('');
  const [bloodType, setBloodType] = useState('');
  const [address, setAddress] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [emergencyContact, setEmergencyContact] = useState('');

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [activeTab, setActiveTab] = useState<'info' | 'emergency' | 'security'>('info');
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  useEffect(() => {
    async function fetchProfile() {
      try {
        setLoading(true);
        const res = await api.get<ApiResponse<PatientDto>>('/patients/me');
        const data = res.data.data;
        if (data) {
          setProfile(data);
          setDateOfBirth(data.dateOfBirth ? data.dateOfBirth.split('T')[0] : '');
          setGender(data.gender || '');
          setBloodType(data.bloodType || '');
          setAddress(data.address || '');
          setPhoneNumber(data.phoneNumber || '');
          setEmergencyContact(data.emergencyContact || '');
        }
      } catch (err: any) {
        console.error('Failed to fetch patient profile', err);
        setErrorMsg(err.response?.data?.message || 'Failed to load profile details.');
      } finally {
        setLoading(false);
      }
    }
    fetchProfile();
  }, []);

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setSuccessMsg(null);
    setErrorMsg(null);

    try {
      let parsedDob: string | null = null;
      if (dateOfBirth) {
        const d = new Date(dateOfBirth);
        if (!isNaN(d.getTime())) {
          parsedDob = d.toISOString();
        }
      }

      const payload: UpdatePatientDto = {
        dateOfBirth: parsedDob,
        gender: gender || '',
        bloodType: bloodType || '',
        address: address || '',
        phoneNumber: phoneNumber || '',
        emergencyContact: emergencyContact || '',
      };

      const res = await api.put<ApiResponse<PatientDto>>('/patients/me', payload);
      if (res.data.data) {
        setProfile(res.data.data);
        setSuccessMsg('Medical profile updated successfully!');
      }
    } catch (err: any) {
      console.error('Failed to update patient profile', err);
      setErrorMsg(err.response?.data?.message || 'Failed to save changes.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="page-enter" style={{ display: 'flex', flexDirection: 'column', gap: 24, maxWidth: 960, margin: '0 auto' }}>
        <div className="skeleton" style={{ height: 160 }} />
        <div className="skeleton" style={{ height: 420 }} />
      </div>
    );
  }

  const initials = profile?.firstName && profile?.lastName
    ? `${profile.firstName[0]}${profile.lastName[0]}`
    : 'P';

  return (
    <div className="page-enter" style={{ maxWidth: 960, margin: '0 auto', display: 'flex', flexDirection: 'column', gap: 28 }}>
      {/* Top Banner Hero */}
      <div className="hero-mesh-bg card" style={{
        padding: '36px 40px',
        background: 'linear-gradient(135deg, var(--color-teal-950) 0%, var(--color-teal-900) 50%, #0d4f49 100%)',
        color: 'white',
        borderRadius: 'var(--radius-xl)',
        boxShadow: '0 20px 40px -15px rgba(2, 15, 14, 0.4)',
        position: 'relative',
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 24 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 24 }}>
            {/* Patient Avatar Badge */}
            <div style={{
              width: 84,
              height: 84,
              borderRadius: 99,
              background: 'linear-gradient(135deg, var(--color-teal-400) 0%, var(--accent) 100%)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '2rem',
              fontWeight: 700,
              color: 'white',
              boxShadow: '0 8px 24px rgba(46, 196, 182, 0.35)',
              border: '3px solid rgba(255, 255, 255, 0.2)',
            }}>
              {initials}
            </div>

            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6 }}>
                <span className="badge" style={{ background: 'rgba(46, 196, 182, 0.2)', color: 'var(--color-teal-200)', border: '1px solid rgba(46, 196, 182, 0.4)' }}>
                  Patient Account
                </span>
                <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6, fontSize: '0.8125rem', color: 'rgba(255, 255, 255, 0.8)' }}>
                  <div className="pulse-dot" /> Active Member
                </span>
              </div>
              <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 700, color: 'white', margin: 0, lineHeight: 1.2 }}>
                {profile?.firstName ? `${profile.firstName} ${profile.lastName}` : 'Patient Profile'}
              </h1>
              <p style={{ color: 'rgba(255, 255, 255, 0.75)', marginTop: 6, fontSize: '0.9375rem', display: 'flex', alignItems: 'center', gap: 16 }}>
                <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
                  <MailIcon size={16} color="var(--color-teal-200)" /> {profile?.email}
                </span>
                {phoneNumber && (
                  <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
                    <PhoneIcon size={16} color="var(--color-teal-200)" /> {phoneNumber}
                  </span>
                )}
              </p>
            </div>
          </div>

          {/* Quick Blood Type & ID Card */}
          <div style={{
            background: 'rgba(255, 255, 255, 0.1)',
            backdropFilter: 'blur(12px)',
            border: '1px solid rgba(255, 255, 255, 0.18)',
            padding: '16px 24px',
            borderRadius: 'var(--radius-lg)',
            display: 'flex',
            alignItems: 'center',
            gap: 20,
          }}>
            <div>
              <div style={{ fontSize: '0.75rem', textTransform: 'uppercase', letterSpacing: '0.05em', color: 'rgba(255, 255, 255, 0.7)' }}>Blood Type</div>
              <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--color-teal-200)' }}>
                {bloodType || 'N/A'}
              </div>
            </div>
            <div style={{ width: 1, height: 36, background: 'rgba(255, 255, 255, 0.2)' }} />
            <div>
              <div style={{ fontSize: '0.75rem', textTransform: 'uppercase', letterSpacing: '0.05em', color: 'rgba(255, 255, 255, 0.7)' }}>Gender</div>
              <div style={{ fontSize: '1rem', fontWeight: 600, color: 'white' }}>
                {gender || 'Not specified'}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Notifications */}
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
      <div style={{ display: 'flex', gap: 12, borderBottom: '1px solid var(--border-default)', paddingBottom: 12 }}>
        <button
          type="button"
          className={`btn ${activeTab === 'info' ? 'btn-primary' : 'btn-ghost'}`}
          onClick={(e) => {
            e.preventDefault();
            setActiveTab('info');
          }}
          style={{ gap: 8 }}
        >
          <UserIcon size={18} /> Personal Details
        </button>
        <button
          type="button"
          className={`btn ${activeTab === 'emergency' ? 'btn-primary' : 'btn-ghost'}`}
          onClick={(e) => {
            e.preventDefault();
            setActiveTab('emergency');
          }}
          style={{ gap: 8 }}
        >
          <PhoneIcon size={18} /> Emergency Contact
        </button>
        <button
          type="button"
          className={`btn ${activeTab === 'security' ? 'btn-primary' : 'btn-ghost'}`}
          onClick={(e) => {
            e.preventDefault();
            setActiveTab('security');
          }}
          style={{ gap: 8 }}
        >
          <ShieldIcon size={18} /> Security & Account
        </button>
      </div>

      {/* Main Content Area */}
      <form onSubmit={handleSave} style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
        {activeTab === 'info' && (
          <div className="card glass-card" style={{ padding: 32, display: 'flex', flexDirection: 'column', gap: 24 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700 }}>
                  Personal Information
                </h2>
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                  Update your contact details, demographics, and clinical preferences.
                </p>
              </div>
              <span className="badge badge-stone" style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                <EditIcon size={14} /> Editable Profile
              </span>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>
              <div className="form-group">
                <label className="form-label">First Name</label>
                <input className="form-input" type="text" value={profile?.firstName || ''} disabled style={{ background: 'var(--bg-subtle)', opacity: 0.8 }} />
              </div>
              <div className="form-group">
                <label className="form-label">Last Name</label>
                <input className="form-input" type="text" value={profile?.lastName || ''} disabled style={{ background: 'var(--bg-subtle)', opacity: 0.8 }} />
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>
              <div className="form-group">
                <label className="form-label">Date of Birth</label>
                <input
                  className="form-input"
                  type="date"
                  value={dateOfBirth}
                  onChange={(e) => setDateOfBirth(e.target.value)}
                />
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
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>
              <div className="form-group">
                <label className="form-label">Blood Type</label>
                <select
                  className="form-input"
                  value={bloodType}
                  onChange={(e) => setBloodType(e.target.value)}
                >
                  <option value="">Select blood type…</option>
                  <option value="A+">A+</option>
                  <option value="A-">A-</option>
                  <option value="B+">B+</option>
                  <option value="B-">B-</option>
                  <option value="AB+">AB+</option>
                  <option value="AB-">AB-</option>
                  <option value="O+">O+</option>
                  <option value="O-">O-</option>
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">Phone Number</label>
                <input
                  className="form-input"
                  type="tel"
                  placeholder="+962 7 9000 0000"
                  value={phoneNumber}
                  onChange={(e) => setPhoneNumber(e.target.value)}
                />
              </div>
            </div>

            <div className="form-group">
              <label className="form-label">Residential Address</label>
              <textarea
                className="form-input"
                rows={3}
                placeholder="Street address, city, region..."
                value={address}
                onChange={(e) => setAddress(e.target.value)}
              />
            </div>
          </div>
        )}

        {activeTab === 'emergency' && (
          <div className="card glass-card" style={{ padding: 32, display: 'flex', flexDirection: 'column', gap: 20 }}>
            <div>
              <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700 }}>
                Emergency Contact Details
              </h2>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                Designate a primary contact for urgent medical communications or emergency situations.
              </p>
            </div>

            <div className="form-group">
              <label className="form-label">Emergency Contact Name & Phone</label>
              <input
                className="form-input"
                type="text"
                placeholder="e.g. Jane Doe (+962 7 9000 0000) - Spouse"
                value={emergencyContact}
                onChange={(e) => setEmergencyContact(e.target.value)}
              />
              <p style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', marginTop: 4 }}>
                This contact will be accessible by attending physicians and hospital administrative staff during appointments.
              </p>
            </div>
          </div>
        )}

        {activeTab === 'security' && (
          <div className="card glass-card" style={{ padding: 32, display: 'flex', flexDirection: 'column', gap: 20 }}>
            <div>
              <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700 }}>
                Security & Authentication
              </h2>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                Manage your credentials and login protection settings.
              </p>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '16px 20px', background: 'var(--bg-subtle)', borderRadius: 'var(--radius-md)' }}>
                <div>
                  <div style={{ fontWeight: 600, fontSize: '0.9375rem' }}>Registered Account Email</div>
                  <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginTop: 2 }}>{profile?.email}</div>
                </div>
                <span className="badge badge-teal">Verified</span>
              </div>

              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '16px 20px', background: 'var(--bg-subtle)', borderRadius: 'var(--radius-md)' }}>
                <div>
                  <div style={{ fontWeight: 600, fontSize: '0.9375rem' }}>Data Privacy & Compliance</div>
                  <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginTop: 2 }}>HIPAA & GDPR Compliant Encrypted Medical Vault</div>
                </div>
                <span className="badge badge-teal" style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                  <ShieldIcon size={14} /> Active Protection
                </span>
              </div>
            </div>
          </div>
        )}

        {/* Submit */}
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 12 }}>
          <button
            type="submit"
            className="btn btn-primary glow-btn"
            disabled={saving}
            style={{ padding: '12px 32px', fontSize: '1rem', fontWeight: 600 }}
          >
            {saving ? 'Saving changes...' : 'Save Profile Changes'}
          </button>
        </div>
      </form>
    </div>
  );
}
