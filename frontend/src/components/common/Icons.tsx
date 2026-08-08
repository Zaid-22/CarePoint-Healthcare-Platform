import React from 'react';

interface IconProps extends React.SVGProps<SVGSVGElement> {
  size?: number;
  color?: string;
}

export const LogoIcon: React.FC<IconProps> = ({ size = 24, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M12 2v20M2 12h20" strokeWidth="2.5" />
    <circle cx="12" cy="12" r="9" strokeWidth="1.5" opacity="0.4" />
  </svg>
);

export const BellIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
    <path d="M13.73 21a2 2 0 0 1-3.46 0" />
  </svg>
);

export const DashboardIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <rect x="3" y="3" width="7" height="7" rx="1" />
    <rect x="14" y="3" width="7" height="7" rx="1" />
    <rect x="14" y="14" width="7" height="7" rx="1" />
    <rect x="3" y="14" width="7" height="7" rx="1" />
  </svg>
);

export const SearchIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <circle cx="11" cy="11" r="8" />
    <line x1="21" y1="21" x2="16.65" y2="16.65" />
  </svg>
);

export const CalendarIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
    <line x1="16" y1="2" x2="16" y2="6" />
    <line x1="8" y1="2" x2="8" y2="6" />
    <line x1="3" y1="10" x2="21" y2="10" />
  </svg>
);

export const BuildingIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M3 21h18" />
    <path d="M6 21V4a1 1 0 0 1 1-1h10a1 1 0 0 1 1 1v17" />
    <path d="M9 7h2M13 7h2M9 11h2M13 11h2M9 15h2M13 15h2" />
  </svg>
);

export const FileTextIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
    <polyline points="14 2 14 8 20 8" />
    <line x1="16" y1="13" x2="8" y2="13" />
    <line x1="16" y1="17" x2="8" y2="17" />
    <polyline points="10 9 9 9 8 9" />
  </svg>
);

export const PillIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="m10.5 20.5 10-10a4.95 4.95 0 1 0-7-7l-10 10a4.95 4.95 0 1 0 7 7Z" />
    <path d="m8.5 8.5 7 7" />
  </svg>
);

export const LogoutIcon: React.FC<IconProps> = ({ size = 18, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
    <polyline points="16 17 21 12 16 7" />
    <line x1="21" y1="12" x2="9" y2="12" />
  </svg>
);

export const DoctorIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M4.8 2.3A.3.3 0 0 0 4.5 2.6V5A7.5 7.5 0 0 0 12 12.5A7.5 7.5 0 0 0 19.5 5V2.6a.3.3 0 0 0-.3-.3h-2.4a.3.3 0 0 0-.3.3V5a4.5 4.5 0 0 1-9 0V2.6a.3.3 0 0 0-.3-.3H4.8Z" />
    <path d="M12 12.5V17a3.5 3.5 0 0 0 7 0v-1" />
    <circle cx="19" cy="14" r="2" />
  </svg>
);

export const ShieldLockIcon: React.FC<IconProps> = ({ size = 24, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
    <rect x="9" y="11" width="6" height="5" rx="1" />
    <path d="M10 11V9a2 2 0 1 1 4 0v2" />
  </svg>
);

export const CheckIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <polyline points="20 6 9 17 4 12" />
  </svg>
);

export const ClockIcon: React.FC<IconProps> = ({ size = 18, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <circle cx="12" cy="12" r="10" />
    <polyline points="12 6 12 12 16 14" />
  </svg>
);

export const InfoIcon: React.FC<IconProps> = ({ size = 18, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <circle cx="12" cy="12" r="10" />
    <line x1="12" y1="16" x2="12" y2="12" />
    <line x1="12" y1="8" x2="12.01" y2="8" />
  </svg>
);

export const HourglassIcon: React.FC<IconProps> = ({ size = 18, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M5 22h14M5 2h14M17 22v-4.172a2 2 0 0 0-.586-1.414L12 12l-4.414 4.414A2 2 0 0 0 7 17.828V22M7 2v4.172a2 2 0 0 0 .586 1.414L12 12l4.414-4.414A2 2 0 0 0 17 6.172V2" />
  </svg>
);

export const EyeIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
    <circle cx="12" cy="12" r="3" />
  </svg>
);

export const EyeOffIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M17.94 17.94A10.07 10.07 0 0 1 12 19c-7 0-10-7-10-7a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 5c7 0 10 7 10 7a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24" />
    <line x1="1" y1="1" x2="23" y2="23" />
  </svg>
);

export const PlusIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <line x1="12" y1="5" x2="12" y2="19" />
    <line x1="5" y1="12" x2="19" y2="12" />
  </svg>
);

export const HeartIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z" />
  </svg>
);

export const SparklesIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="m12 3-1.912 5.813a2 2 0 0 1-1.275 1.275L3 12l5.813 1.912a2 2 0 0 1 1.275 1.275L12 21l1.912-5.813a2 2 0 0 1 1.275-1.275L21 12l-5.813-1.912a2 2 0 0 1-1.275-1.275L12 3Z" />
    <path d="M5 3v4M3 5h4M19 17v4M17 19h4" />
  </svg>
);

export const BabyIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <circle cx="12" cy="10" r="6" />
    <path d="M12 16a6 6 0 0 0-6 6h12a6 6 0 0 0-6-6Z" />
    <circle cx="10" cy="9" r="1" fill={color} />
    <circle cx="14" cy="9" r="1" fill={color} />
    <path d="M10.5 12a1.5 1.5 0 0 0 3 0" />
  </svg>
);

export const BrainIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M12 5a3 3 0 1 0-5.997.125 4 4 0 0 0-2.526 5.77 4 4 0 0 0 .556 6.588A4 4 0 1 0 12 18Z" />
    <path d="M12 5a3 3 0 1 1 5.997.125 4 4 0 0 1 2.526 5.77 4 4 0 0 1-.556 6.588A4 4 0 1 1 12 18Z" />
    <path d="M12 5v13" />
  </svg>
);

export const BoneIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M17 3a2.5 2.5 0 0 1 2.5 2.5c0 .76-.34 1.44-.88 1.9l-1.63 1.4a.5.5 0 0 0-.17.38c0 .24.1.47.28.64l6.1 5.3a2.5 2.5 0 0 1 0 3.76 2.5 2.5 0 0 1-3.53 0l-6.1-5.3a.5.5 0 0 0-.64-.28.5.5 0 0 0-.38.17l-1.4 1.63a2.5 2.5 0 0 1-3.8 0 2.5 2.5 0 0 1 0-3.53l1.4-1.63a.5.5 0 0 0 .17-.38.5.5 0 0 0-.28-.64l-6.1-5.3a2.5 2.5 0 0 1 0-3.76 2.5 2.5 0 0 1 3.53 0l6.1 5.3c.17.18.4.28.64.28.14 0 .27-.06.38-.17l1.4-1.63A2.5 2.5 0 0 1 17 3Z" />
  </svg>
);

export const StethoscopeIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M4.8 2.3A.3.3 0 0 0 4.5 2.6V5A7.5 7.5 0 0 0 12 12.5A7.5 7.5 0 0 0 19.5 5V2.6a.3.3 0 0 0-.3-.3h-2.4a.3.3 0 0 0-.3.3V5a4.5 4.5 0 0 1-9 0V2.6a.3.3 0 0 0-.3-.3H4.8Z" />
    <path d="M12 12.5V17a3.5 3.5 0 0 0 7 0v-1" />
    <circle cx="19" cy="14" r="2" />
  </svg>
);

export const ArrowRightIcon: React.FC<IconProps> = ({ size = 18, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <line x1="5" y1="12" x2="19" y2="12" />
    <polyline points="12 5 19 12 12 19" />
  </svg>
);

export const StarIcon: React.FC<IconProps> = ({ size = 18, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill={color} stroke={color} strokeWidth="1" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2" />
  </svg>
);

export const UserIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2" />
    <circle cx="12" cy="7" r="4" />
  </svg>
);

export const ShieldIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
  </svg>
);

export const EditIcon: React.FC<IconProps> = ({ size = 18, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
  </svg>
);

export const PhoneIcon: React.FC<IconProps> = ({ size = 18, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z" />
  </svg>
);

export const MailIcon: React.FC<IconProps> = ({ size = 18, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <rect width="20" height="16" x="2" y="4" rx="2" />
    <path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7" />
  </svg>
);

export const ActivityIcon: React.FC<IconProps> = ({ size = 20, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
  </svg>
);

export const FolderIcon: React.FC<IconProps> = ({ size = 18, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M4 20h16a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.93a2 2 0 0 1-1.66-.9l-.82-1.2A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13c0 1.1.9 2 2 2z" />
  </svg>
);

export const CheckCircleIcon: React.FC<IconProps> = ({ size = 18, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
    <polyline points="22 4 12 14.01 9 11.01" />
  </svg>
);

export const XCircleIcon: React.FC<IconProps> = ({ size = 18, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <circle cx="12" cy="12" r="10" />
    <line x1="15" y1="9" x2="9" y2="15" />
    <line x1="9" y1="9" x2="15" y2="15" />
  </svg>
);

export const ChevronDownIcon: React.FC<IconProps> = ({ size = 18, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <polyline points="6 9 12 15 18 9" />
  </svg>
);

export const XIcon: React.FC<IconProps> = ({ size = 14, color = 'currentColor', ...props }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <line x1="18" y1="6" x2="6" y2="18" />
    <line x1="6" y1="6" x2="18" y2="18" />
  </svg>
);
