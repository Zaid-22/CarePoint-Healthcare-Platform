import { lazy, Suspense, useEffect } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import ProtectedRoute from './components/ProtectedRoute';
import { useAppDispatch, useAppSelector } from './hooks/useRedux';
import { initializeSession } from './store/slices/authSlice';

const LoginPage = lazy(() => import('./pages/auth/LoginPage'));
const RegisterPage = lazy(() => import('./pages/auth/RegisterPage'));
const ForgotPasswordPage = lazy(() => import('./pages/auth/ForgotPasswordPage'));
const ResetPasswordPage = lazy(() => import('./pages/auth/ResetPasswordPage'));
const PatientLayout = lazy(() => import('./layouts/PatientLayout'));
const DoctorLayout = lazy(() => import('./layouts/DoctorLayout'));
const AdminLayout = lazy(() => import('./layouts/AdminLayout'));
const PatientDashboard = lazy(() => import('./pages/patient/PatientDashboard'));
const FindDoctors = lazy(() => import('./pages/patient/FindDoctors'));
const MyAppointments = lazy(() => import('./pages/patient/MyAppointments'));
const MedicalHistoryPage = lazy(() => import('./pages/patient/MedicalHistoryPage'));
const PatientProfilePage = lazy(() => import('./pages/patient/PatientProfilePage'));
const MyPrescriptionsPage = lazy(() => import('./pages/patient/MyPrescriptionsPage'));
const MyDocumentsPage = lazy(() => import('./pages/patient/MyDocumentsPage'));
const DoctorDashboard = lazy(() => import('./pages/doctor/DoctorDashboard'));
const DoctorAppointments = lazy(() => import('./pages/doctor/DoctorAppointments'));
const DoctorProfilePage = lazy(() => import('./pages/doctor/DoctorProfilePage'));
const UnauthorizedPage = lazy(() => import('./pages/UnauthorizedPage'));
const AdminDashboard = lazy(() => import('./pages/admin/AdminDashboard'));
const AdminSpecialtiesPage = lazy(() => import('./pages/admin/AdminSpecialtiesPage'));
const LandingPage = lazy(() => import('./pages/LandingPage'));

function HomeRoute() {
  const { isAuthenticated, user } = useAppSelector((s) => s.auth);
  if (!isAuthenticated) return <LandingPage />;
  const userRoles = user?.roles || (user?.role ? [user.role] : []);
  if (userRoles.includes('Admin')) return <Navigate to="/admin/dashboard" replace />;
  if (userRoles.includes('Doctor')) return <Navigate to="/doctor/dashboard" replace />;
  return <Navigate to="/dashboard" replace />;
}

export default function App() {
  const dispatch = useAppDispatch();
  const initialized = useAppSelector((state) => state.auth.initialized);

  useEffect(() => {
    dispatch(initializeSession());
  }, [dispatch]);

  if (!initialized) {
    return <div className="page-enter" style={{ minHeight: '100vh', background: 'var(--bg-page)' }} />;
  }

  return (
    <Suspense fallback={<div className="page-enter" style={{ minHeight: '100vh', background: 'var(--bg-page)' }} />}>
      <Routes>
      {/* Public */}
      <Route path="/" element={<HomeRoute />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />
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
          <Route path="/my-prescriptions" element={<MyPrescriptionsPage />} />
          <Route path="/my-documents" element={<MyDocumentsPage />} />
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
    </Suspense>
  );
}
