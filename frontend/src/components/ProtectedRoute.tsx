import { Navigate, Outlet } from 'react-router-dom';
import { useAppSelector } from '../hooks/useRedux';

interface Props {
  allowedRoles?: string[];
}

export default function ProtectedRoute({ allowedRoles }: Props) {
  const { isAuthenticated, initialized, user } = useAppSelector((s) => s.auth);

  if (!initialized) return null;

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && allowedRoles.length > 0) {
    const userRoles = user?.roles || (user?.role ? [user.role] : []);

    if (userRoles.length === 0) {
      return <Navigate to="/login" replace />;
    }

    const hasRole = userRoles.some((r) => allowedRoles.includes(r));
    if (!hasRole) {
      // Redirect to correct dashboard based on user role
      if (userRoles.includes('Admin')) {
        return <Navigate to="/admin/dashboard" replace />;
      }
      if (userRoles.includes('Doctor')) {
        return <Navigate to="/doctor/dashboard" replace />;
      }
      if (userRoles.includes('Patient')) {
        return <Navigate to="/dashboard" replace />;
      }
      return <Navigate to="/unauthorized" replace />;
    }
  }

  return <Outlet />;
}
