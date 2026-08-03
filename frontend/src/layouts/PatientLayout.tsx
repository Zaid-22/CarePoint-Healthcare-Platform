import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../hooks/useRedux';
import { logoutFromServer } from '../store/slices/authSlice';
import { DashboardIcon, SearchIcon, CalendarIcon, FileTextIcon, LogoIcon, LogoutIcon, UserIcon } from '../components/common/Icons';
import NotificationDrawer from '../components/common/NotificationDrawer';

const navItems = [
  { to: '/dashboard', label: 'Dashboard', icon: DashboardIcon },
  { to: '/find-doctors', label: 'Find Doctors', icon: SearchIcon },
  { to: '/my-appointments', label: 'Appointments', icon: CalendarIcon },
  { to: '/medical-history', label: 'Medical History', icon: FileTextIcon },
  { to: '/my-prescriptions', label: 'Prescriptions', icon: FileTextIcon },
  { to: '/my-profile', label: 'My Profile', icon: UserIcon },
];

export default function PatientLayout() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { user } = useAppSelector((s) => s.auth);

  const handleLogout = async () => {
    await dispatch(logoutFromServer());
    navigate('/login', { replace: true });
  };

  return (
    <div style={{ display: 'flex', minHeight: '100vh', background: 'var(--bg-page)' }}>
      {/* Sidebar */}
      <aside style={{
        width: 240,
        background: 'var(--bg-surface)',
        borderRight: '1px solid var(--border-default)',
        display: 'flex',
        flexDirection: 'column',
        padding: '24px 16px',
        position: 'fixed',
        top: 0, left: 0, bottom: 0,
        zIndex: 100,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '0 8px', marginBottom: 36 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <div style={{
              width: 34, height: 34, borderRadius: 8,
              background: 'var(--accent)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              color: 'white',
            }}>
              <LogoIcon size={20} color="white" />
            </div>
            <span style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: '1.125rem' }}>
              CarePoint
            </span>
          </div>
          <NotificationDrawer />
        </div>

        <nav style={{ display: 'flex', flexDirection: 'column', gap: 4, flex: 1 }}>
          {navItems.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              style={({ isActive }) => ({
                display: 'flex',
                alignItems: 'center',
                gap: 10,
                padding: '10px 12px',
                borderRadius: 'var(--radius-md)',
                textDecoration: 'none',
                fontSize: '0.9375rem',
                fontWeight: 500,
                color: isActive ? 'var(--accent)' : 'var(--text-secondary)',
                background: isActive ? 'var(--accent-light)' : 'transparent',
                transition: 'all 120ms ease',
              })}
            >
              <Icon size={18} color="currentColor" />
              {label}
            </NavLink>
          ))}
        </nav>

        <div style={{ borderTop: '1px solid var(--border-default)', paddingTop: 16 }}>
          <div style={{ padding: '8px 12px', marginBottom: 8 }}>
            <div style={{ fontSize: '0.875rem', fontWeight: 500, color: 'var(--text-primary)' }}>
              {user?.email}
            </div>
            <div className="badge badge-teal" style={{ marginTop: 4 }}>Patient</div>
          </div>
          <button
            className="btn btn-ghost"
            onClick={handleLogout}
            style={{ width: '100%', justifyContent: 'flex-start', gap: 8 }}
          >
            <LogoutIcon size={16} /> Sign out
          </button>
        </div>
      </aside>

      {/* Main */}
      <main style={{ marginLeft: 240, flex: 1, padding: '40px 48px', minHeight: '100vh' }}>
        <Outlet />
      </main>
    </div>
  );
}
