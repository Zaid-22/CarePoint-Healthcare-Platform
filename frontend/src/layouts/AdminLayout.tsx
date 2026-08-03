import { DashboardIcon, PillIcon, ShieldLockIcon } from '../components/common/Icons';
import PortalLayout from './PortalLayout';

const navItems = [
  { to: '/admin/dashboard', label: 'Verifications Queue', icon: DashboardIcon },
  { to: '/admin/specialties', label: 'Medical Specialties', icon: PillIcon },
];

export default function AdminLayout() {
  return (
    <PortalLayout
      brand="CarePoint Admin"
      roleLabel="Administrator"
      tone="admin"
      brandIcon={ShieldLockIcon}
      navItems={navItems}
    />
  );
}
