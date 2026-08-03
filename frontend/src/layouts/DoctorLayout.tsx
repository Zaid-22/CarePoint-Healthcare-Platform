import { DashboardIcon, CalendarIcon, DoctorIcon } from '../components/common/Icons';
import PortalLayout from './PortalLayout';

const navItems = [
  { to: '/doctor/dashboard', label: 'Dashboard', icon: DashboardIcon },
  { to: '/doctor/appointments', label: 'Appointments', icon: CalendarIcon },
  { to: '/doctor/profile', label: 'My Profile', icon: DoctorIcon },
];

export default function DoctorLayout() {
  return (
    <PortalLayout
      brand="CarePoint Doctor"
      roleLabel="Doctor"
      tone="doctor"
      brandIcon={DoctorIcon}
      navItems={navItems}
    />
  );
}
