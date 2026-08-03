import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import api from '../../api/client';

interface ResetPasswordForm {
  email: string;
  newPassword: string;
  confirmNewPassword: string;
}

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const [completed, setCompleted] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const token = searchParams.get('token') ?? '';
  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<ResetPasswordForm>({
    defaultValues: { email: searchParams.get('email') ?? '' }
  });
  const newPassword = watch('newPassword');

  const onSubmit = async (form: ResetPasswordForm) => {
    setError(null);
    if (!token) {
      setError('This reset link is invalid or incomplete. Request a new link to continue.');
      return;
    }

    try {
      await api.post('/auth/reset-password', { ...form, token });
      setCompleted(true);
    } catch (requestError: any) {
      setError(requestError.response?.data?.message || 'This reset link is invalid or has expired.');
    }
  };

  return (
    <main style={{ minHeight: '100vh', display: 'grid', placeItems: 'center', padding: 24, background: 'linear-gradient(145deg, var(--color-teal-950), var(--color-teal-800))' }}>
      <section className="card page-enter" style={{ width: '100%', maxWidth: 440, padding: 36 }}>
        <p style={{ fontSize: '0.75rem', fontWeight: 700, letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--accent)', marginBottom: 8 }}>Secure account recovery</p>
        <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '1.9rem', lineHeight: 1.15, marginBottom: 12 }}>Choose a new password</h1>
        <p style={{ color: 'var(--text-secondary)', marginBottom: 28 }}>Use at least eight characters, including uppercase, lowercase, a number, and a symbol.</p>

        {completed ? (
          <div style={{ padding: 16, borderRadius: 'var(--radius-md)', color: 'var(--color-teal-900)', background: 'var(--color-teal-50)', border: '1px solid var(--color-teal-200)' }}>
            Your password has been reset. You can now sign in with your new password.
          </div>
        ) : (
          <form onSubmit={handleSubmit(onSubmit)} style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
            <div className="form-group">
              <label className="form-label">Email address</label>
              <input className="form-input" type="email" autoComplete="email" {...register('email', { required: 'Email is required' })} />
              {errors.email && <span className="form-error">{errors.email.message}</span>}
            </div>
            <div className="form-group">
              <label className="form-label">New password</label>
              <input className="form-input" type="password" autoComplete="new-password" {...register('newPassword', { required: 'A new password is required', minLength: { value: 8, message: 'Use at least 8 characters' } })} />
              {errors.newPassword && <span className="form-error">{errors.newPassword.message}</span>}
            </div>
            <div className="form-group">
              <label className="form-label">Confirm new password</label>
              <input className="form-input" type="password" autoComplete="new-password" {...register('confirmNewPassword', { required: 'Please confirm your password', validate: (value) => value === newPassword || 'Passwords do not match' })} />
              {errors.confirmNewPassword && <span className="form-error">{errors.confirmNewPassword.message}</span>}
            </div>
            {error && <p className="form-error">{error}</p>}
            <button className="btn btn-primary" type="submit" disabled={isSubmitting} style={{ padding: '12px' }}>
              {isSubmitting ? 'Resetting password…' : 'Reset password'}
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
