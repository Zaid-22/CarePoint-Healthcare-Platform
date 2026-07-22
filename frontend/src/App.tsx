import { Routes, Route, Navigate } from 'react-router-dom';
import ProtectedRoute from './components/ProtectedRoute';
import LoginPage from './pages/auth/LoginPage';
import RegisterPage from './pages/auth/RegisterPage';
import PatientLayout from './layouts/PatientLayout';
import DoctorLayout from './layouts/DoctorLayout';
import PatientDashboard from './pages/patient/PatientDashboard';
import FindDoctors from './pages/patient/FindDoctors';
import MyAppointments from './pages/patient/MyAppointments';
import MedicalHistoryPage from './pages/patient/MedicalHistoryPage';
import PatientProfilePage from './pages/patient/PatientProfilePage';
import DoctorDashboard from './pages/doctor/DoctorDashboard';
import DoctorAppointments from './pages/doctor/DoctorAppointments';
import DoctorProfilePage from './pages/doctor/DoctorProfilePage';
import UnauthorizedPage from './pages/UnauthorizedPage';

import AdminLayout from './layouts/AdminLayout';
import AdminDashboard from './pages/admin/AdminDashboard';
import AdminSpecialtiesPage from './pages/admin/AdminSpecialtiesPage';
import LandingPage from './pages/LandingPage';
import { useAppSelector } from './hooks/useRedux';

function HomeRoute() {
  const { isAuthenticated, user } = useAppSelector((s) => s.auth);
  if (!isAuthenticated) return <LandingPage />;
  const userRoles = user?.roles || (user?.role ? [user.role] : []);
  if (userRoles.includes('Admin')) return <Navigate to="/admin/dashboard" replace />;
  if (userRoles.includes('Doctor')) return <Navigate to="/doctor/dashboard" replace />;
  return <Navigate to="/dashboard" replace />;
}

export default function App() {
  return (
    <Routes>
      {/* Public */}
      <Route path="/" element={<HomeRoute />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/unauthorized" element={<UnauthorizedPage />} />

      {/* Admin routes */}
      <Route element={<ProtectedRoute allowedRoles={['Admin']} />}>
        <Route element={<AdminLayout />}>
          <Route path="/admin/dashboard" element={<AdminDashboard />} />
          <Route path="/admin/specialties" element={<AdminSpecialtiesPage />} />
        </Route>
      </Route>

      {/* Patient routes */}
      <Route element={<ProtectedRoute allowedRoles={['Patient']} />}>
        <Route element={<PatientLayout />}>
          <Route path="/dashboard" element={<PatientDashboard />} />
          <Route path="/find-doctors" element={<FindDoctors />} />
          <Route path="/my-appointments" element={<MyAppointments />} />
          <Route path="/medical-history" element={<MedicalHistoryPage />} />
          <Route path="/my-profile" element={<PatientProfilePage />} />
        </Route>
      </Route>

      {/* Doctor routes */}
      <Route element={<ProtectedRoute allowedRoles={['Doctor']} />}>
        <Route element={<DoctorLayout />}>
          <Route path="/doctor/dashboard" element={<DoctorDashboard />} />
          <Route path="/doctor/appointments" element={<DoctorAppointments />} />
          <Route path="/doctor/profile" element={<DoctorProfilePage />} />
        </Route>
      </Route>

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
