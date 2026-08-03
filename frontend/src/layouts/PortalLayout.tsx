import type { ComponentType } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../hooks/useRedux';
import { logoutFromServer } from '../store/slices/authSlice';
import { LogoutIcon } from '../components/common/Icons';
import NotificationDrawer from '../components/common/NotificationDrawer';

type PortalIcon = ComponentType<{ size?: number; color?: string }>;

export interface PortalNavItem {
  to: string;
  label: string;
  icon: PortalIcon;
}

interface PortalLayoutProps {
  brand: string;
  roleLabel: string;
  tone: 'patient' | 'doctor' | 'admin';
  brandIcon: PortalIcon;
  navItems: PortalNavItem[];
}

export default function PortalLayout({
  brand,
  roleLabel,
  tone,
  brandIcon: BrandIcon,
  navItems,
}: PortalLayoutProps) {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const user = useAppSelector((state) => state.auth.user);

  const handleLogout = async () => {
    await dispatch(logoutFromServer());
    navigate('/login', { replace: true });
  };

  return (
    <div className={`portal-shell portal-shell--${tone}`}>
      <aside className="portal-rail">
        <div className="portal-identity">
          <div className="portal-brand">
            <span className="portal-brand-mark" aria-hidden="true">
              <BrandIcon size={20} color="white" />
            </span>
            <span className="portal-brand-name">{brand}</span>
          </div>
          <NotificationDrawer />
        </div>

        <nav className="portal-nav" aria-label={`${roleLabel} navigation`}>
          {navItems.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) => `portal-nav-link${isActive ? ' is-active' : ''}`}
            >
              <Icon size={18} color="currentColor" />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="portal-account">
          <div className="portal-account-copy">
            <span className="portal-account-email" title={user?.email}>{user?.email}</span>
            <span className="portal-role-label">{roleLabel}</span>
          </div>
          <button className="btn btn-ghost portal-sign-out" onClick={handleLogout} type="button">
            <LogoutIcon size={16} />
            <span>Sign out</span>
          </button>
        </div>
      </aside>

      <main className="portal-main">
        <Outlet />
      </main>
    </div>
  );
}
