import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../../hooks/useRedux';
import { register as registerUser, clearError } from '../../store/slices/authSlice';
import type { RegisterRequest, SpecialtyDto, ApiResponse } from '../../types';
import api from '../../api/client';
import heroImage from '../../assets/hero_medical.png';
import { LogoIcon, CalendarIcon, FileTextIcon, PillIcon, EyeIcon, EyeOffIcon } from '../../components/common/Icons';

export default function RegisterPage() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const [showPassword, setShowPassword] = useState(false);
  const { loading, error, isAuthenticated, user } = useAppSelector((s) => s.auth);

  const [specialties, setSpecialties] = useState<SpecialtyDto[]>([]);
  const [selectedSpecialties, setSelectedSpecialties] = useState<string[]>([]);
  const [loadingSpecialties, setLoadingSpecialties] = useState(false);

  const { register, handleSubmit, watch, formState: { errors } } = useForm<RegisterRequest>();

  const selectedRole = watch('role');
  const password = watch('password');
  const profilePicUrl = watch('profilePictureUrl');

  useEffect(() => {
    if (isAuthenticated && user) {
      const userRoles = user.roles || (user.role ? [user.role] : []);
      let dest = '/dashboard';
      if (userRoles.includes('Admin')) {
        dest = '/admin/dashboard';
      } else if (userRoles.includes('Doctor')) {
        dest = '/doctor/dashboard';
      }
      navigate(dest, { replace: true });
    }
  }, [isAuthenticated, user, navigate]);

  useEffect(() => () => { dispatch(clearError()); }, [dispatch]);

  useEffect(() => {
    if (selectedRole === 'Doctor') {
      setLoadingSpecialties(true);
      api.get<ApiResponse<SpecialtyDto[]>>('/specialties')
        .then((res) => setSpecialties(res.data.data || []))
        .catch((err) => console.error('Failed to load specialties', err))
        .finally(() => setLoadingSpecialties(false));
    }
  }, [selectedRole]);

  const toggleSpecialty = (id: string) => {
    setSelectedSpecialties((prev) =>
      prev.includes(id) ? prev.filter((sId) => sId !== id) : [...prev, id]
    );
  };

  const onSubmit = (data: RegisterRequest) => {
    const payload: RegisterRequest = {
      ...data,
      specialtyIds: selectedRole === 'Doctor' ? selectedSpecialties : undefined,
      consultationFee: selectedRole === 'Doctor' && data.consultationFee ? Number(data.consultationFee) : undefined,
    };
    dispatch(registerUser(payload));
  };

  return (
    <div style={{ minHeight: '100vh', display: 'grid', gridTemplateColumns: '1fr 1fr' }}>
      {/* Left — brand & visual hero panel */}
      <div style={{
        position: 'relative',
        background: 'linear-gradient(160deg, var(--color-teal-950) 0%, var(--color-teal-900) 50%, var(--color-teal-800) 100%)',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        padding: '48px',
        color: 'white',
        overflow: 'hidden',
      }}>
        {/* Background image asset overlay */}
        <img
          src={heroImage}
          alt="Healthcare background"
          style={{
            position: 'absolute',
            top: 0, left: 0, width: '100%', height: '100%',
            objectFit: 'cover',
            opacity: 0.35,
            mixBlendMode: 'luminosity',
            pointerEvents: 'none',
          }}
        />

        <Link to="/" style={{ position: 'relative', zIndex: 2, display: 'flex', alignItems: 'center', gap: 10, textDecoration: 'none', color: 'white' }}>
          <div style={{
            width: 36, height: 36, borderRadius: 10,
            background: 'var(--color-teal-400)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            color: 'var(--color-teal-950)',
          }}>
            <LogoIcon size={22} color="var(--color-teal-950)" />
          </div>
          <span style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: '1.25rem' }}>
            CarePoint
          </span>
        </Link>

        <div style={{ position: 'relative', zIndex: 2, margin: 'auto 0' }}>
          <p style={{ fontSize: '0.875rem', opacity: 0.8, letterSpacing: '0.08em', textTransform: 'uppercase', marginBottom: 16 }}>
            Join thousands of active users
          </p>
          <h1 style={{ fontFamily: 'var(--font-display)', fontSize: 'clamp(2rem, 3vw, 2.75rem)', fontWeight: 700, lineHeight: 1.2, marginBottom: 24 }}>
            Your care journey<br />starts here.
          </h1>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
            {[
              [CalendarIcon, 'Book appointments in seconds'],
              [FileTextIcon, 'Access your health records anytime'],
              [PillIcon, 'Get digital prescriptions'],
            ].map(([Icon, text]: any) => (
              <div key={text} style={{ display: 'flex', gap: 12, alignItems: 'center', opacity: 0.9 }}>
                <div style={{ padding: 6, borderRadius: 8, background: 'rgba(255,255,255,0.12)', display: 'flex' }}>
                  <Icon size={18} color="white" />
                </div>
                <span style={{ fontSize: '0.9375rem' }}>{text}</span>
              </div>
            ))}
          </div>
        </div>

        <div style={{ position: 'relative', zIndex: 2, display: 'flex', gap: 40, paddingTop: 24, borderTop: '1px solid rgba(255,255,255,0.12)' }}>
          {[['10k+', 'Patients'], ['500+', 'Specialists'], ['24/7', 'Support']].map(([n, l]) => (
            <div key={l}>
              <div style={{ fontFamily: 'var(--font-display)', fontSize: '1.5rem', fontWeight: 700 }}>{n}</div>
              <div style={{ fontSize: '0.8125rem', opacity: 0.7 }}>{l}</div>
            </div>
          ))}
        </div>
      </div>

      {/* Right — form panel */}
      <div style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '48px 64px',
        background: 'var(--bg-page)',
        overflowY: 'auto',
      }}>
        <div style={{ width: '100%', maxWidth: 440 }} className="page-enter">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
            <div>
              <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.875rem', fontWeight: 700, margin: 0 }}>
                Create account
              </h2>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginTop: 4, marginBottom: 0 }}>
                Fill in your details to get started.
              </p>
            </div>
            <Link to="/login" className="btn btn-ghost" style={{ fontSize: '0.875rem', fontWeight: 600, padding: '8px 14px' }}>
              Sign In →
            </Link>
          </div>

          {error && (
            <div style={{
              padding: '12px 16px',
              background: 'var(--color-rose-100)',
              color: 'var(--color-rose-600)',
              borderRadius: 'var(--radius-md)',
              fontSize: '0.875rem',
              marginBottom: 20,
            }}>
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit(onSubmit)} style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
              <div className="form-group">
                <label className="form-label">First name</label>
                <input className="form-input" placeholder="Jane"
                  {...register('firstName', { required: 'Required' })} />
                {errors.firstName && <span className="form-error">{errors.firstName.message}</span>}
              </div>
              <div className="form-group">
                <label className="form-label">Last name</label>
                <input className="form-input" placeholder="Smith"
                  {...register('lastName', { required: 'Required' })} />
                {errors.lastName && <span className="form-error">{errors.lastName.message}</span>}
              </div>
            </div>

            <div className="form-group">
              <label className="form-label">Email address</label>
              <input className="form-input" type="email" placeholder="you@example.com"
                {...register('email', { required: 'Email is required' })} />
              {errors.email && <span className="form-error">{errors.email.message}</span>}
            </div>

            <div className="form-group">
              <label className="form-label">I am a</label>
              <select className="form-input" {...register('role', { required: 'Select a role' })}>
                <option value="">Select role…</option>
                <option value="Patient">Patient</option>
                <option value="Doctor">Doctor</option>
              </select>
              {errors.role && <span className="form-error">{errors.role.message}</span>}
            </div>

            {/* Doctor-Specific Onboarding Fields */}
            {selectedRole === 'Doctor' && (
              <div style={{
                padding: 16,
                borderRadius: 'var(--radius-lg)',
                background: 'var(--color-teal-50)',
                border: '1px solid var(--color-teal-200)',
                display: 'flex',
                flexDirection: 'column',
                gap: 14
              }}>
                <div style={{ fontWeight: 600, fontSize: '0.9375rem', color: 'var(--color-teal-900)' }}>
                  Practitioner Profile Information
                </div>

                {/* Specialties Multi-select */}
                <div className="form-group">
                  <label className="form-label" style={{ marginBottom: 4 }}>Clinical Specialties</label>
                  <p style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', marginBottom: 12 }}>
                    Select all clinical specialties that apply to your practice. Patients can discover you based on these filters.
                  </p>
                  {loadingSpecialties ? (
                    <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>Loading specialties...</div>
                  ) : specialties.length === 0 ? (
                    <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>No specialties found</div>
                  ) : (
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                      {specialties.map((s) => {
                        const active = selectedSpecialties.includes(s.id);
                        return (
                          <button
                            key={s.id}
                            type="button"
                            onClick={() => toggleSpecialty(s.id)}
                            style={{
                              padding: '6px 14px',
                              borderRadius: 20,
                              fontSize: '0.8125rem',
                              fontWeight: 500,
                              border: active ? '1.5px solid var(--accent)' : '1px solid var(--border-default)',
                              background: active ? 'var(--accent)' : 'white',
                              color: active ? 'white' : 'var(--text-primary)',
                              cursor: 'pointer',
                              transition: 'all 120ms ease'
                            }}
                          >
                            {active ? '✓ ' : '+ '}{s.name}
                          </button>
                        );
                      })}
                    </div>
                  )}
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                  <div className="form-group">
                    <label className="form-label">Consultation Fee (JOD)</label>
                    <input className="form-input" type="number" step="0.01" placeholder="50.00"
                      {...register('consultationFee')} />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Phone Number</label>
                    <input className="form-input" placeholder="+1 234 567 890"
                      {...register('phoneNumber')} />
                  </div>
                </div>

                <div className="form-group">
                  <label className="form-label">Gender</label>
                  <select className="form-input" {...register('gender')}>
                    <option value="">Select gender…</option>
                    <option value="Male">Male</option>
                    <option value="Female">Female</option>
                    <option value="Other">Other</option>
                  </select>
                </div>

                <div className="form-group">
                  <label className="form-label">Profile Picture URL</label>
                  <input className="form-input" placeholder="https://example.com/my-photo.jpg"
                    {...register('profilePictureUrl')} />
                  {profilePicUrl && (
                    <div style={{ marginTop: 8, display: 'flex', alignItems: 'center', gap: 10 }}>
                      <img
                        src={profilePicUrl}
                        alt="Avatar preview"
                        style={{ width: 44, height: 44, borderRadius: 99, objectFit: 'cover', border: '2px solid var(--accent)' }}
                        onError={(e) => { (e.target as HTMLElement).style.display = 'none'; }}
                      />
                      <span style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)' }}>Avatar Preview</span>
                    </div>
                  )}
                </div>

                <div className="form-group">
                  <label className="form-label">Professional Bio / Summary</label>
                  <textarea className="form-input" rows={2} placeholder="Brief summary of your clinical background..."
                    {...register('bio')} />
                </div>
              </div>
            )}

            <div className="form-group">
              <label className="form-label">Password</label>
              <div style={{ position: 'relative' }}>
                <input
                  className="form-input"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="Min. 8 characters"
                  style={{ paddingRight: 40 }}
                  {...register('password', { required: 'Password is required', minLength: { value: 8, message: 'Minimum 8 characters' } })}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((prev) => !prev)}
                  style={{
                    position: 'absolute', right: 12, top: '50%', transform: 'translateY(-50%)',
                    background: 'none', border: 'none', padding: 0, cursor: 'pointer',
                    color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', justifyContent: 'center',
                  }}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? <EyeOffIcon size={18} /> : <EyeIcon size={18} />}
                </button>
              </div>
              {errors.password && <span className="form-error">{errors.password.message}</span>}
            </div>

            <div className="form-group">
              <label className="form-label">Confirm password</label>
              <div style={{ position: 'relative' }}>
                <input
                  className="form-input"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="••••••••"
                  style={{ paddingRight: 40 }}
                  {...register('confirmPassword', {
                    required: 'Please confirm',
                    validate: (val) => val === password || 'Passwords do not match',
                  })}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((prev) => !prev)}
                  style={{
                    position: 'absolute', right: 12, top: '50%', transform: 'translateY(-50%)',
                    background: 'none', border: 'none', padding: 0, cursor: 'pointer',
                    color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', justifyContent: 'center',
                  }}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? <EyeOffIcon size={18} /> : <EyeIcon size={18} />}
                </button>
              </div>
              {errors.confirmPassword && <span className="form-error">{errors.confirmPassword.message}</span>}
            </div>

            <button
              type="submit"
              className="btn btn-primary"
              disabled={loading}
              style={{ width: '100%', padding: '12px', marginTop: 4 }}
            >
              {loading ? 'Creating account…' : 'Create account'}
            </button>
          </form>

          <p style={{ marginTop: 24, textAlign: 'center', fontSize: '0.8125rem', color: 'var(--text-secondary)' }}>
            By signing up you agree to our{' '}
            <span style={{ color: 'var(--accent)', cursor: 'pointer' }}>Terms of Service</span>
            {' '}and{' '}
            <span style={{ color: 'var(--accent)', cursor: 'pointer' }}>Privacy Policy</span>.
          </p>

          <p style={{ marginTop: 16, textAlign: 'center', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
            Already have an account?{' '}
            <Link to="/login" style={{ color: 'var(--accent)', fontWeight: 600, textDecoration: 'none' }}>
              Sign In
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
