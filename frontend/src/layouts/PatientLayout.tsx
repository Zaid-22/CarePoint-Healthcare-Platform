import { DashboardIcon, SearchIcon, CalendarIcon, FileTextIcon, LogoIcon, UserIcon } from '../components/common/Icons';
import PortalLayout from './PortalLayout';

const navItems = [
  { to: '/dashboard', label: 'Dashboard', icon: DashboardIcon },
  { to: '/find-doctors', label: 'Find Doctors', icon: SearchIcon },
  { to: '/my-appointments', label: 'Appointments', icon: CalendarIcon },
  { to: '/medical-history', label: 'Medical History', icon: FileTextIcon },
  { to: '/my-prescriptions', label: 'Prescriptions', icon: FileTextIcon },
  { to: '/my-documents', label: 'Documents', icon: FileTextIcon },
  { to: '/my-profile', label: 'My Profile', icon: UserIcon },
];

export default function PatientLayout() {
  return (
    <PortalLayout
      brand="CarePoint"
      roleLabel="Patient"
      tone="patient"
      brandIcon={LogoIcon}
      navItems={navItems}
    />
  );
}
