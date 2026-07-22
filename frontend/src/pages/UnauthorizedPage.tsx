import { Link, useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../hooks/useRedux';
import { logout } from '../store/slices/authSlice';
import { LogoIcon, ShieldLockIcon, DashboardIcon, LogoutIcon } from '../components/common/Icons';

export default function UnauthorizedPage() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { isAuthenticated, user } = useAppSelector((s) => s.auth);

  const handleLogout = () => {
    dispatch(logout());
    navigate('/login', { replace: true });
  };

  const userRole = user?.roles?.[0] || user?.role || 'User';
  const targetDashboard = userRole === 'Admin' ? '/admin/dashboard' : userRole === 'Doctor' ? '/doctor/dashboard' : '/dashboard';

  return (
    <div style={{
      minHeight: '100vh',
      display: 'flex',
      flexDirection: 'column',
      justifyContent: 'space-between',
      background: 'radial-gradient(circle at 50% 20%, rgba(26, 143, 133, 0.08) 0%, rgba(244, 242, 239, 1) 70%)',
      padding: '24px 32px',
      position: 'relative',
      overflow: 'hidden',
    }}>
      {/* Header */}
      <header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', width: '100%', maxWidth: 1200, margin: '0 auto' }}>
        <Link to="/" style={{ display: 'flex', alignItems: 'center', gap: 10, textDecoration: 'none', color: 'var(--text-primary)' }}>
          <div style={{
            width: 38,
            height: 38,
            borderRadius: 10,
            background: 'var(--color-teal-900)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: 'var(--shadow-sm)',
          }}>
            <LogoIcon size={22} color="white" />
          </div>
          <span style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: '1.25rem' }}>
            CarePoint
          </span>
        </Link>
        <span style={{
          fontSize: '0.75rem',
          fontWeight: 600,
          letterSpacing: '0.08em',
          textTransform: 'uppercase',
          padding: '4px 10px',
          borderRadius: 99,
          background: 'rgba(224, 63, 90, 0.1)',
          color: 'var(--color-rose-600)',
          border: '1px solid rgba(224, 63, 90, 0.2)',
        }}>
          403 • Access Restricted
        </span>
      </header>

      {/* Content */}
      <main style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        flex: 1,
        padding: '32px 0',
      }}>
        <div className="card page-enter" style={{
          width: '100%',
          maxWidth: 460,
          padding: '44px 36px',
          textAlign: 'center',
          background: 'rgba(255, 255, 255, 0.88)',
          backdropFilter: 'blur(16px)',
          border: '1px solid rgba(229, 226, 222, 0.8)',
          boxShadow: '0 20px 40px -15px rgba(0,0,0,0.07)',
          borderRadius: 'var(--radius-xl)',
        }}>
          {/* Badge Icon */}
          <div style={{
            width: 72,
            height: 72,
            borderRadius: 20,
            background: 'linear-gradient(135deg, #fff0f3 0%, #ffe4e9 100%)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            margin: '0 auto 24px',
            color: 'var(--color-rose-600)',
            boxShadow: '0 8px 20px -6px rgba(224, 63, 90, 0.25)',
          }}>
            <ShieldLockIcon size={34} />
          </div>

          <h1 style={{
            fontFamily: 'var(--font-display)',
            fontSize: '1.75rem',
            fontWeight: 700,
            marginBottom: 10,
            color: 'var(--text-primary)',
            letterSpacing: '-0.02em',
          }}>
            Access Restricted
          </h1>

          <p style={{
            color: 'var(--text-secondary)',
            fontSize: '0.9375rem',
            lineHeight: 1.6,
            marginBottom: 24,
          }}>
            You don't have permission to view this page. This section is reserved for another account role.
          </p>

          {/* User badge if authenticated */}
          {isAuthenticated && user && (
            <div style={{
              background: 'var(--bg-subtle)',
              border: '1px solid var(--border-default)',
              borderRadius: 'var(--radius-md)',
              padding: '12px 16px',
              fontSize: '0.8125rem',
              color: 'var(--text-secondary)',
              marginBottom: 28,
              textAlign: 'left',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
            }}>
              <div>
                <span style={{ display: 'block', fontSize: '0.75rem', opacity: 0.7 }}>Signed in as</span>
                <strong style={{ color: 'var(--text-primary)', fontWeight: 600 }}>{user.email}</strong>
              </div>
              <span style={{
                background: 'var(--accent-light)',
                color: 'var(--accent)',
                fontWeight: 600,
                padding: '2px 8px',
                borderRadius: 6,
                fontSize: '0.75rem',
                textTransform: 'capitalize',
              }}>
                {userRole}
              </span>
            </div>
          )}

          {/* Actions */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            {isAuthenticated ? (
              <>
                <Link to={targetDashboard} className="btn btn-primary" style={{
                  justifyContent: 'center',
                  padding: '12px 20px',
                  fontWeight: 600,
                  fontSize: '0.9375rem',
                  gap: 8,
                }}>
                  <DashboardIcon size={18} />
                  Go to My Dashboard
                </Link>
                <button
                  type="button"
                  onClick={handleLogout}
                  className="btn btn-ghost"
                  style={{
                    justifyContent: 'center',
                    padding: '10px 20px',
                    fontSize: '0.875rem',
                    gap: 8,
                    color: 'var(--text-secondary)',
                  }}
                >
                  <LogoutIcon size={16} />
                  Sign Out & Switch Account
                </button>
              </>
            ) : (
              <Link to="/login" className="btn btn-primary" style={{
                justifyContent: 'center',
                padding: '12px 20px',
                fontWeight: 600,
                fontSize: '0.9375rem',
              }}>
                Sign In to Your Account
              </Link>
            )}
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer style={{ textAlign: 'center', fontSize: '0.8125rem', color: 'var(--text-secondary)', opacity: 0.7 }}>
        © {new Date().getFullYear()} CarePoint Health Systems. All rights reserved.
      </footer>
    </div>
  );
}
