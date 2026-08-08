import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../../hooks/useRedux';
import { login, clearError } from '../../store/slices/authSlice';
import type { LoginRequest } from '../../types';
import heroImage from '../../assets/hero_medical.png';
import { EyeIcon, EyeOffIcon } from '../../components/common/Icons';

export default function LoginPage() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const [showPassword, setShowPassword] = useState(false);
  const { loading, error, isAuthenticated, user } = useAppSelector((s) => s.auth);

  const { register, handleSubmit, formState: { errors } } = useForm<LoginRequest>();

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

  const onSubmit = (data: LoginRequest) => {
    dispatch(login(data));
  };

  return (
    <div className="auth-shell">
      {/* Left — brand & visual hero panel */}
      <div className="auth-hero" style={{
        position: 'relative',
        background: 'linear-gradient(160deg, var(--color-teal-950) 0%, var(--color-teal-900) 50%, var(--color-teal-800) 100%)',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        color: 'white',
        overflow: 'hidden',
      }}>
        {/* Background image overlay */}
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
            width: 40, height: 40, borderRadius: 12,
            background: 'var(--color-teal-400)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            color: 'var(--color-teal-950)', fontSize: 20, fontWeight: 700,
          }}>✦</div>
          <span style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: '1.5rem', letterSpacing: '-0.02em' }}>
            CarePoint
          </span>
        </Link>

        <div className="auth-hero-main" style={{ position: 'relative', zIndex: 2, margin: 'auto 0' }}>
          <span style={{
            fontSize: '0.8125rem',
            fontWeight: 600,
            letterSpacing: '0.1em',
            textTransform: 'uppercase',
            color: 'var(--color-teal-200)',
            background: 'rgba(46, 196, 182, 0.15)',
            padding: '6px 14px',
            borderRadius: 99,
            display: 'inline-block',
            marginBottom: 20,
          }}>
            Next-Gen Connected Health
          </span>
          <h1 style={{ fontFamily: 'var(--font-display)', fontSize: 'clamp(2.2rem, 3.5vw, 3rem)', fontWeight: 700, lineHeight: 1.15, marginBottom: 20 }}>
            Precision Care.<br />Zero Friction.
          </h1>
          <p style={{ opacity: 0.8, maxWidth: 420, fontSize: '1.05rem', lineHeight: 1.6 }}>
            Experience real-time scheduling, secure medical record access, and instant digital prescriptions.
          </p>
        </div>

        <div className="auth-hero-stats" style={{ position: 'relative', zIndex: 2, gap: 40, paddingTop: 24, borderTop: '1px solid rgba(255,255,255,0.12)' }}>
          {[['10k+', 'Patients'], ['500+', 'Specialists'], ['99.8%', 'Uptime']].map(([n, l]) => (
            <div key={l}>
              <div style={{ fontFamily: 'var(--font-display)', fontSize: '1.5rem', fontWeight: 700, color: 'var(--color-teal-200)' }}>{n}</div>
              <div style={{ fontSize: '0.8125rem', opacity: 0.7 }}>{l}</div>
            </div>
          ))}
        </div>
      </div>

      {/* Right — form panel */}
      <div className="auth-form-panel" style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'var(--bg-page)',
      }}>
        <div style={{ width: '100%', maxWidth: 400 }} className="page-enter">
          <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.875rem', fontWeight: 700, marginBottom: 8 }}>
            Welcome back
          </h2>
          <p style={{ color: 'var(--text-secondary)', marginBottom: 36 }}>
            Sign in to your CarePoint account.
          </p>

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

          <form onSubmit={handleSubmit(onSubmit)} style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
            <div className="form-group">
              <label className="form-label">Email address</label>
              <input
                className="form-input"
                type="email"
                placeholder="you@example.com"
                {...register('email', { required: 'Email is required' })}
              />
              {errors.email && <span className="form-error">{errors.email.message}</span>}
            </div>

            <div className="form-group">
              <label className="form-label">Password</label>
              <div style={{ position: 'relative' }}>
                <input
                  className="form-input"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="••••••••"
                  style={{ paddingRight: 40 }}
                  {...register('password', { required: 'Password is required' })}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((prev) => !prev)}
                  style={{
                    position: 'absolute',
                    right: 12,
                    top: '50%',
                    transform: 'translateY(-50%)',
                    background: 'none',
                    border: 'none',
                    padding: 0,
                    cursor: 'pointer',
                    color: 'var(--text-secondary)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                  }}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? <EyeOffIcon size={18} /> : <EyeIcon size={18} />}
                </button>
              </div>
              {errors.password && <span className="form-error">{errors.password.message}</span>}
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: -10 }}>
              <Link to="/forgot-password" style={{ color: 'var(--accent)', fontSize: '0.875rem', fontWeight: 600, textDecoration: 'none' }}>
                Forgot your password?
              </Link>
            </div>

            <button
              type="submit"
              className="btn btn-primary"
              disabled={loading}
              style={{ width: '100%', padding: '12px', marginTop: 4 }}
            >
              {loading ? 'Signing in…' : 'Sign in'}
            </button>
          </form>

          <p style={{ marginTop: 28, textAlign: 'center', fontSize: '0.9rem', color: 'var(--text-secondary)' }}>
            New to CarePoint?{' '}
            <Link to="/register" style={{ color: 'var(--accent)', fontWeight: 600, textDecoration: 'none' }}>
              Create an account
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
