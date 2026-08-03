import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import api from '../../api/client';

interface ForgotPasswordForm {
  email: string;
}

export default function ForgotPasswordPage() {
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<ForgotPasswordForm>();

  const onSubmit = async ({ email }: ForgotPasswordForm) => {
    setError(null);
    try {
      await api.post('/auth/forgot-password', { email });
      setSubmitted(true);
    } catch {
      setError('We could not start a password reset. Please try again shortly.');
    }
  };

  return (
    <main style={{ minHeight: '100vh', display: 'grid', placeItems: 'center', padding: 24, background: 'linear-gradient(145deg, var(--color-teal-950), var(--color-teal-800))' }}>
      <section className="card page-enter" style={{ width: '100%', maxWidth: 440, padding: 36 }}>
        <div style={{ width: 44, height: 44, display: 'grid', placeItems: 'center', borderRadius: 14, color: 'var(--color-teal-950)', background: 'var(--color-teal-200)', fontWeight: 800, marginBottom: 20 }}>✦</div>
        <p style={{ fontSize: '0.75rem', fontWeight: 700, letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--accent)', marginBottom: 8 }}>Account recovery</p>
        <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '1.9rem', lineHeight: 1.15, marginBottom: 12 }}>Reset your password</h1>
        <p style={{ color: 'var(--text-secondary)', marginBottom: 28 }}>Enter your email address and we’ll send a secure reset link if an account exists.</p>

        {submitted ? (
          <div style={{ padding: 16, borderRadius: 'var(--radius-md)', color: 'var(--color-teal-900)', background: 'var(--color-teal-50)', border: '1px solid var(--color-teal-200)' }}>
            Check your inbox for a password-reset link. For local development without SMTP, the link is written to the API log.
          </div>
        ) : (
          <form onSubmit={handleSubmit(onSubmit)} style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
            <div className="form-group">
              <label className="form-label">Email address</label>
              <input className="form-input" type="email" autoComplete="email" placeholder="you@example.com" {...register('email', { required: 'Email is required' })} />
              {errors.email && <span className="form-error">{errors.email.message}</span>}
            </div>
            {error && <p className="form-error">{error}</p>}
            <button className="btn btn-primary" type="submit" disabled={isSubmitting} style={{ padding: '12px' }}>
              {isSubmitting ? 'Sending link…' : 'Send reset link'}
            </button>
          </form>
        )}

        <p style={{ marginTop: 24, textAlign: 'center', color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
          <Link to="/login" style={{ color: 'var(--accent)', fontWeight: 600, textDecoration: 'none' }}>Return to sign in</Link>
        </p>
      </section>
    </main>
  );
}
