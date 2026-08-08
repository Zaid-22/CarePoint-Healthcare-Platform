import {
  BuildingIcon,
  CalendarIcon,
  DashboardIcon,
  PillIcon,
  ShieldIcon,
  ShieldLockIcon,
  UserIcon,
} from '../components/common/Icons';
import PortalLayout from './PortalLayout';

const navItems = [
  { to: '/admin/dashboard', label: 'Doctor approvals', icon: DashboardIcon },
  { to: '/admin/users', label: 'User access', icon: ShieldIcon },
  { to: '/admin/patients', label: 'Patients', icon: UserIcon },
  { to: '/admin/appointments', label: 'Appointments', icon: CalendarIcon },
  { to: '/admin/clinics', label: 'Clinics', icon: BuildingIcon },
  { to: '/admin/specialties', label: 'Specialties', icon: PillIcon },
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
