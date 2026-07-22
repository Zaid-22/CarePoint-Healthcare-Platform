import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAppSelector } from '../hooks/useRedux';
import {
  LogoIcon,
  SearchIcon,
  CalendarIcon,
  PillIcon,
  FileTextIcon,
  ShieldLockIcon,
  CheckIcon,
  ClockIcon,
  HeartIcon,
  SparklesIcon,
  BabyIcon,
  BrainIcon,
  BoneIcon,
  StethoscopeIcon,
  ArrowRightIcon,
  StarIcon,
} from '../components/common/Icons';
import carepointHeroBanner from '../assets/carepoint_hero_banner.png';
import doctorPortrait from '../assets/doctor_portrait.png';

export default function LandingPage() {
  const navigate = useNavigate();
  const { isAuthenticated, user } = useAppSelector((s) => s.auth);

  // Interactive UI States
  const [selectedSpecialty, setSelectedSpecialty] = useState('Cardiology');
  const [searchQuery, setSearchQuery] = useState('');
  const [activeWorkflowRole, setActiveWorkflowRole] = useState<'patient' | 'doctor'>('patient');
  const [selectedSlot, setSelectedSlot] = useState('Today, 02:30 PM');

  const handleGetStarted = () => {
    if (isAuthenticated && user) {
      const userRoles = user.roles || (user.role ? [user.role] : []);
      if (userRoles.includes('Doctor')) {
        navigate('/doctor/dashboard');
      } else {
        navigate('/dashboard');
      }
    } else {
      navigate('/register');
    }
  };

  const specialties = [
    { name: 'Cardiology', desc: 'Heart care & cardiovascular health', count: '12 Doctors', icon: <HeartIcon size={22} color="var(--accent)" />, doctorName: 'Dr. Sarah Jenkins', doctorRole: 'Senior Cardiologist • MD', slots: ['Today, 02:30 PM', 'Today, 04:00 PM', 'Tomorrow, 10:15 AM'] },
    { name: 'Dermatology', desc: 'Skin, hair & cosmetic care', count: '8 Doctors', icon: <SparklesIcon size={22} color="var(--accent)" />, doctorName: 'Dr. Marcus Vance', doctorRole: 'Chief Dermatologist • FAAD', slots: ['Today, 03:15 PM', 'Tomorrow, 11:00 AM', 'Tomorrow, 02:30 PM'] },
    { name: 'Pediatrics', desc: 'Child & adolescent healthcare', count: '10 Doctors', icon: <BabyIcon size={22} color="var(--accent)" />, doctorName: 'Dr. Aisha Patel', doctorRole: 'Pediatric Specialist • MD', slots: ['Today, 05:00 PM', 'Tomorrow, 09:30 AM', 'Thu, 01:00 PM'] },
    { name: 'Neurology', desc: 'Brain & nerve consultations', count: '6 Doctors', icon: <BrainIcon size={22} color="var(--accent)" />, doctorName: 'Dr. Elena Rostova', doctorRole: 'Neurologist & Specialist', slots: ['Tomorrow, 10:00 AM', 'Thu, 02:15 PM', 'Fri, 11:30 AM'] },
    { name: 'Orthopedics', desc: 'Bone, joint & muscle care', count: '9 Doctors', icon: <BoneIcon size={22} color="var(--accent)" />, doctorName: 'Dr. James Wilson', doctorRole: 'Orthopedic Surgeon • DO', slots: ['Today, 04:30 PM', 'Tomorrow, 01:45 PM', 'Fri, 09:00 AM'] },
    { name: 'General Medicine', desc: 'Comprehensive primary medical care', count: '15 Doctors', icon: <StethoscopeIcon size={22} color="var(--accent)" />, doctorName: 'Dr. Michael Chen', doctorRole: 'Primary Care Physician', slots: ['Today, 01:15 PM', 'Today, 03:45 PM', 'Tomorrow, 08:30 AM'] },
  ];

  const activeSpecialtyData = specialties.find((s) => s.name === selectedSpecialty) || specialties[0];

  const filteredSpecialties = specialties.filter((s) =>
    s.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    s.desc.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <div style={{ minHeight: '100vh', background: 'var(--bg-page)', color: 'var(--text-primary)' }} className="hero-mesh-bg page-enter">
      {/* Sticky Navigation Bar */}
      <header
        style={{
          position: 'sticky',
          top: 0,
          zIndex: 50,
          background: 'rgba(255, 255, 255, 0.85)',
          backdropFilter: 'blur(16px)',
          WebkitBackdropFilter: 'blur(16px)',
          borderBottom: '1px solid rgba(229, 226, 222, 0.8)',
        }}
      >
        <div
          style={{
            maxWidth: 1240,
            margin: '0 auto',
            padding: '16px 24px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          {/* Logo */}
          <Link to="/" style={{ display: 'flex', alignItems: 'center', gap: 12, textDecoration: 'none' }}>
            <div
              style={{
                width: 42,
                height: 42,
                borderRadius: 12,
                background: 'linear-gradient(135deg, var(--color-teal-600) 0%, var(--color-teal-900) 100%)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'white',
                boxShadow: '0 4px 14px rgba(26, 143, 133, 0.35)',
              }}
            >
              <LogoIcon size={24} color="white" />
            </div>
            <div style={{ display: 'flex', flexDirection: 'column' }}>
              <span style={{ fontFamily: 'var(--font-display)', fontWeight: 800, fontSize: '1.35rem', color: 'var(--color-teal-950)', letterSpacing: '-0.02em' }}>
                CarePoint
              </span>
              <span style={{ fontSize: '0.6875rem', fontWeight: 600, color: 'var(--accent)', textTransform: 'uppercase', letterSpacing: '0.08em' }}>
                Telehealth Cloud
              </span>
            </div>
          </Link>

          {/* Navigation Links */}
          <nav style={{ display: 'flex', alignItems: 'center', gap: 32 }} className="hide-mobile">
            <a href="#features" style={{ textDecoration: 'none', color: 'var(--text-secondary)', fontWeight: 600, fontSize: '0.9375rem', transition: 'color 150ms' }}>
              Features
            </a>
            <a href="#how-it-works" style={{ textDecoration: 'none', color: 'var(--text-secondary)', fontWeight: 600, fontSize: '0.9375rem', transition: 'color 150ms' }}>
              How it Works
            </a>
            <a href="#specialties" style={{ textDecoration: 'none', color: 'var(--text-secondary)', fontWeight: 600, fontSize: '0.9375rem', transition: 'color 150ms' }}>
              Specialties
            </a>
          </nav>

          {/* Auth Actions */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            {isAuthenticated ? (
              <button onClick={handleGetStarted} className="btn btn-primary glow-btn" style={{ padding: '10px 22px', fontSize: '0.9375rem', borderRadius: 'var(--radius-md)', fontWeight: 600 }}>
                Go to Dashboard
              </button>
            ) : (
              <>
                <Link to="/login" className="btn btn-ghost" style={{ fontSize: '0.9375rem', fontWeight: 600 }}>
                  Sign In
                </Link>
                <Link to="/register" className="btn btn-primary glow-btn" style={{ fontSize: '0.9375rem', fontWeight: 600 }}>
                  Get Started
                </Link>
              </>
            )}
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <section style={{ padding: '64px 24px 60px', maxWidth: 1240, margin: '0 auto', position: 'relative' }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 48, alignItems: 'center' }}>
          <div>
            {/* Tag Badge */}
            <div
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 10,
                padding: '6px 16px',
                borderRadius: 99,
                background: 'rgba(26, 143, 133, 0.08)',
                color: 'var(--accent)',
                fontSize: '0.8125rem',
                fontWeight: 700,
                letterSpacing: '0.04em',
                marginBottom: 24,
                border: '1px solid rgba(26, 143, 133, 0.2)',
                boxShadow: '0 2px 8px rgba(26, 143, 133, 0.06)',
              }}
            >
              <span className="pulse-dot"></span>
              <ShieldLockIcon size={16} color="var(--accent)" />
              <span>HIPAA-COMPLIANT TELEHEALTH PLATFORM</span>
            </div>

            <h1
              style={{
                fontFamily: 'var(--font-display)',
                fontSize: '3.25rem',
                fontWeight: 800,
                lineHeight: 1.15,
                letterSpacing: '-0.03em',
                color: 'var(--color-teal-950)',
                marginBottom: 20,
              }}
            >
              Next-Generation Healthcare <span className="gradient-text-teal">Connected</span> & <span className="gradient-text">Simplified</span>
            </h1>

            <p style={{ fontSize: '1.125rem', color: 'var(--text-secondary)', lineHeight: 1.65, marginBottom: 32, maxWidth: 540 }}>
              Consult verified medical specialists instantly, manage electronic prescriptions, and access complete clinical records with zero friction.
            </p>

            {/* Search Input Box */}
            <div
              style={{
                background: 'var(--bg-surface)',
                padding: 8,
                borderRadius: 'var(--radius-lg)',
                border: '1.5px solid var(--color-teal-200)',
                boxShadow: '0 12px 32px -8px rgba(15, 23, 42, 0.1)',
                display: 'flex',
                alignItems: 'center',
                gap: 10,
                marginBottom: 24,
                maxWidth: 520,
              }}
            >
              <div style={{ paddingLeft: 12, display: 'flex', alignItems: 'center', color: 'var(--accent)' }}>
                <SearchIcon size={20} />
              </div>
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search specialty, condition, or doctor..."
                style={{
                  border: 'none',
                  outline: 'none',
                  flex: 1,
                  fontSize: '0.9375rem',
                  fontFamily: 'var(--font-body)',
                  color: 'var(--text-primary)',
                  background: 'transparent',
                }}
              />
              <button
                onClick={handleGetStarted}
                className="btn btn-primary glow-btn"
                style={{ padding: '12px 22px', fontSize: '0.9375rem', borderRadius: 'var(--radius-md)', fontWeight: 600 }}
              >
                Find Doctor
              </button>
            </div>

            {/* Specialty Pills Filter */}
            <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 32 }}>
              {specialties.map((spec) => (
                <button
                  key={spec.name}
                  onClick={() => {
                    setSelectedSpecialty(spec.name);
                    setSelectedSlot(spec.slots[0]);
                  }}
                  className={`badge interactive-chip ${selectedSpecialty === spec.name ? 'active' : 'badge-stone'}`}
                  style={{
                    padding: '6px 14px',
                    fontSize: '0.8125rem',
                    display: 'inline-flex',
                    alignItems: 'center',
                    gap: 6
                  }}
                >
                  {spec.icon}
                  <span>{spec.name}</span>
                </button>
              ))}
            </div>

            {/* Feature Highlights */}
            <div style={{ display: 'flex', gap: 24, paddingTop: 20, borderTop: '1px solid var(--border-default)', flexWrap: 'wrap' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: '0.875rem', fontWeight: 600, color: 'var(--color-teal-900)' }}>
                <div style={{ width: 22, height: 22, borderRadius: 99, background: 'var(--accent-light)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <CheckIcon size={14} color="var(--accent)" />
                </div>
                <span>Real-time Doctor Availability</span>
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: '0.875rem', fontWeight: 600, color: 'var(--color-teal-900)' }}>
                <div style={{ width: 22, height: 22, borderRadius: 99, background: 'var(--accent-light)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <CheckIcon size={14} color="var(--accent)" />
                </div>
                <span>Encrypted Health Records</span>
              </div>
            </div>
          </div>

          {/* Hero Right Visual Column */}
          <div style={{ position: 'relative', display: 'flex', flexDirection: 'column', gap: 20 }}>
            {/* Main Graphic Banner Card */}
            <div
              className="glass-card"
              style={{
                borderRadius: 'var(--radius-xl)',
                overflow: 'hidden',
                border: '1px solid rgba(255, 255, 255, 0.9)',
                boxShadow: '0 25px 50px -12px rgba(15, 23, 42, 0.15)',
                position: 'relative',
              }}
            >
              <img
                src={carepointHeroBanner}
                alt="CarePoint Telehealth Hub"
                style={{ width: '100%', height: 300, objectFit: 'cover', objectPosition: 'center 25%', display: 'block' }}
              />
              <div
                style={{
                  position: 'absolute',
                  inset: 0,
                  background: 'linear-gradient(to top, rgba(6, 43, 40, 0.9) 0%, rgba(6, 43, 40, 0.2) 50%, transparent 100%)',
                  display: 'flex',
                  flexDirection: 'column',
                  justifyContent: 'flex-end',
                  padding: 24,
                  color: 'white',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4 }}>
                  <span className="badge badge-teal" style={{ background: 'rgba(255,255,255,0.2)', color: 'white', border: 'none' }}>
                    Live Telehealth Portal
                  </span>
                  <span style={{ fontSize: '0.75rem', opacity: 0.8, fontWeight: 500 }}>Active Care Network</span>
                </div>
                <div style={{ fontWeight: 800, fontSize: '1.25rem', fontFamily: 'var(--font-display)' }}>
                  Accredited Specialist Clinical Network
                </div>
              </div>
            </div>

            {/* Interactive Live Quick-Book Widget */}
            <div
              className="glass-card glass-card-hover"
              style={{
                padding: 24,
                borderRadius: 'var(--radius-lg)',
                border: '1.5px solid var(--color-teal-200)',
                background: 'linear-gradient(135deg, #ffffff 0%, rgba(240, 253, 251, 0.9) 100%)',
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
                  <img
                    src={doctorPortrait}
                    alt={activeSpecialtyData.doctorName}
                    style={{ width: 48, height: 48, borderRadius: 99, objectFit: 'cover', border: '2.5px solid var(--accent)' }}
                  />
                  <div>
                    <div style={{ fontWeight: 800, fontSize: '1.05rem', color: 'var(--color-teal-950)' }}>
                      {activeSpecialtyData.doctorName}
                    </div>
                    <div style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', fontWeight: 500 }}>
                      {activeSpecialtyData.doctorRole}
                    </div>
                  </div>
                </div>
                <span className="badge badge-teal" style={{ fontSize: '0.75rem', display: 'flex', alignItems: 'center', gap: 4 }}>
                  <StarIcon size={12} color="#f59e0b" /> 4.9 Verified
                </span>
              </div>

              {/* Slot Selector Tabs */}
              <div style={{ background: 'var(--bg-surface)', padding: 14, borderRadius: 'var(--radius-md)', border: '1px solid var(--border-default)' }}>
                <div style={{ fontSize: '0.75rem', fontWeight: 700, color: 'var(--accent)', marginBottom: 8, display: 'flex', alignItems: 'center', gap: 6, letterSpacing: '0.05em' }}>
                  <ClockIcon size={14} color="var(--accent)" /> AVAILABLE APPOINTMENT SLOTS ({selectedSpecialty.toUpperCase()})
                </div>

                <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 12 }}>
                  {activeSpecialtyData.slots.map((slot) => (
                    <button
                      key={slot}
                      onClick={() => setSelectedSlot(slot)}
                      style={{
                        padding: '6px 12px',
                        borderRadius: 'var(--radius-sm)',
                        fontSize: '0.8125rem',
                        fontWeight: 600,
                        border: selectedSlot === slot ? '1.5px solid var(--accent)' : '1px solid var(--border-default)',
                        background: selectedSlot === slot ? 'var(--accent-light)' : 'transparent',
                        color: selectedSlot === slot ? 'var(--accent)' : 'var(--text-secondary)',
                        cursor: 'pointer',
                        transition: 'all 150ms ease',
                      }}
                    >
                      {slot}
                    </button>
                  ))}
                </div>

                <button
                  onClick={handleGetStarted}
                  className="btn btn-primary"
                  style={{ width: '100%', padding: '10px', fontSize: '0.875rem', fontWeight: 600, borderRadius: 'var(--radius-md)' }}
                >
                  Confirm Slot ({selectedSlot})
                </button>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Stats Banner Section */}
      <section
        style={{
          background: 'rgba(255, 255, 255, 0.8)',
          backdropFilter: 'blur(12px)',
          borderTop: '1px solid var(--border-default)',
          borderBottom: '1px solid var(--border-default)',
          padding: '44px 24px',
        }}
      >
        <div
          style={{
            maxWidth: 1240,
            margin: '0 auto',
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
            gap: 28,
            textAlign: 'center',
          }}
        >
          <div className="glass-card-hover" style={{ padding: 16, borderRadius: 'var(--radius-md)' }}>
            <div style={{ fontFamily: 'var(--font-display)', fontSize: '2.5rem', fontWeight: 800, color: 'var(--accent)' }}>50+</div>
            <div style={{ fontSize: '0.9375rem', color: 'var(--text-secondary)', fontWeight: 600, marginTop: 4 }}>Accredited Doctors</div>
          </div>
          <div className="glass-card-hover" style={{ padding: 16, borderRadius: 'var(--radius-md)' }}>
            <div style={{ fontFamily: 'var(--font-display)', fontSize: '2.5rem', fontWeight: 800, color: 'var(--color-teal-900)' }}>12,500+</div>
            <div style={{ fontSize: '0.9375rem', color: 'var(--text-secondary)', fontWeight: 600, marginTop: 4 }}>Virtual Consultations</div>
          </div>
          <div className="glass-card-hover" style={{ padding: 16, borderRadius: 'var(--radius-md)' }}>
            <div style={{ fontFamily: 'var(--font-display)', fontSize: '2.5rem', fontWeight: 800, color: 'var(--accent)' }}>100%</div>
            <div style={{ fontSize: '0.9375rem', color: 'var(--text-secondary)', fontWeight: 600, marginTop: 4 }}>EHR Record Protection</div>
          </div>
          <div className="glass-card-hover" style={{ padding: 16, borderRadius: 'var(--radius-md)' }}>
            <div style={{ fontFamily: 'var(--font-display)', fontSize: '2.5rem', fontWeight: 800, color: 'var(--color-teal-900)' }}>4.95 ★</div>
            <div style={{ fontSize: '0.9375rem', color: 'var(--text-secondary)', fontWeight: 600, marginTop: 4 }}>Patient Satisfaction</div>
          </div>
        </div>
      </section>

      {/* Platform Features Section */}
      <section id="features" style={{ padding: '80px 24px', maxWidth: 1240, margin: '0 auto' }}>
        <div style={{ textAlign: 'center', maxWidth: 640, margin: '0 auto 52px' }}>
          <span className="badge badge-teal" style={{ marginBottom: 12 }}>
            INTEGRATED HEALTHCARE ENGINE
          </span>
          <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '2.35rem', fontWeight: 800, color: 'var(--color-teal-950)', letterSpacing: '-0.02em' }}>
            Built for Modern Clinical Workflows
          </h2>
          <p style={{ color: 'var(--text-secondary)', marginTop: 10, fontSize: '1.05rem', lineHeight: 1.6 }}>
            CarePoint connects patients and healthcare providers through unified booking, e-prescriptions, and secure health data management.
          </p>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: 28 }}>
          {/* Feature 1 */}
          <div className="glass-card glass-card-hover" style={{ padding: 32, borderRadius: 'var(--radius-lg)', display: 'flex', flexDirection: 'column', gap: 16 }}>
            <div
              style={{
                width: 52,
                height: 52,
                borderRadius: 14,
                background: 'linear-gradient(135deg, var(--color-teal-50) 0%, rgba(46,196,182,0.2) 100%)',
                color: 'var(--accent)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                border: '1px solid var(--color-teal-200)',
              }}
            >
              <SearchIcon size={24} />
            </div>
            <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700, color: 'var(--color-teal-950)' }}>
              Verified Doctor Directory
            </h3>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.9375rem', lineHeight: 1.6 }}>
              Browse accredited specialists by clinical specialty, consultation pricing, or geographical region with instant credential checks.
            </p>
          </div>

          {/* Feature 2 */}
          <div className="glass-card glass-card-hover" style={{ padding: 32, borderRadius: 'var(--radius-lg)', display: 'flex', flexDirection: 'column', gap: 16 }}>
            <div
              style={{
                width: 52,
                height: 52,
                borderRadius: 14,
                background: 'linear-gradient(135deg, var(--color-teal-50) 0%, rgba(46,196,182,0.2) 100%)',
                color: 'var(--accent)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                border: '1px solid var(--color-teal-200)',
              }}
            >
              <CalendarIcon size={24} />
            </div>
            <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700, color: 'var(--color-teal-950)' }}>
              Real-Time Slot Booking
            </h3>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.9375rem', lineHeight: 1.6 }}>
              View synchronized clinical calendars and lock in appointment slots instantly with automated confirmation notifications.
            </p>
          </div>

          {/* Feature 3 */}
          <div className="glass-card glass-card-hover" style={{ padding: 32, borderRadius: 'var(--radius-lg)', display: 'flex', flexDirection: 'column', gap: 16 }}>
            <div
              style={{
                width: 52,
                height: 52,
                borderRadius: 14,
                background: 'linear-gradient(135deg, var(--color-teal-50) 0%, rgba(46,196,182,0.2) 100%)',
                color: 'var(--accent)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                border: '1px solid var(--color-teal-200)',
              }}
            >
              <PillIcon size={24} />
            </div>
            <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700, color: 'var(--color-teal-950)' }}>
              Digital E-Prescriptions
            </h3>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.9375rem', lineHeight: 1.6 }}>
              Receive standardized digital prescriptions directly from attending physicians with structured dosage and pharmacy guidance.
            </p>
          </div>

          {/* Feature 4 */}
          <div className="glass-card glass-card-hover" style={{ padding: 32, borderRadius: 'var(--radius-lg)', display: 'flex', flexDirection: 'column', gap: 16 }}>
            <div
              style={{
                width: 52,
                height: 52,
                borderRadius: 14,
                background: 'linear-gradient(135deg, var(--color-teal-50) 0%, rgba(46,196,182,0.2) 100%)',
                color: 'var(--accent)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                border: '1px solid var(--color-teal-200)',
              }}
            >
              <FileTextIcon size={24} />
            </div>
            <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700, color: 'var(--color-teal-950)' }}>
              Centralized Medical Records
            </h3>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.9375rem', lineHeight: 1.6 }}>
              Access diagnostic reports, past consultation logs, and lab results securely inside a protected patient vault.
            </p>
          </div>
        </div>
      </section>

      {/* Dynamic Workflow Timeline Section */}
      <section id="how-it-works" style={{ background: 'linear-gradient(135deg, var(--color-teal-950) 0%, var(--color-teal-900) 100%)', color: 'white', padding: '80px 24px', position: 'relative' }}>
        <div style={{ maxWidth: 1240, margin: '0 auto' }}>
          <div style={{ textAlign: 'center', maxWidth: 640, margin: '0 auto 44px' }}>
            <span className="badge badge-amber" style={{ marginBottom: 12 }}>
              STREAMLINED EXPERIENCE
            </span>
            <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '2.35rem', fontWeight: 800, color: 'white', letterSpacing: '-0.02em' }}>
              How CarePoint Works
            </h2>
            <p style={{ color: 'var(--color-teal-200)', marginTop: 10, fontSize: '1.05rem' }}>
              Choose your role to see how simple telehealth can be.
            </p>

            {/* Role Switcher Toggle */}
            <div
              style={{
                display: 'inline-flex',
                background: 'rgba(255, 255, 255, 0.1)',
                padding: 4,
                borderRadius: 99,
                marginTop: 20,
                border: '1px solid rgba(255, 255, 255, 0.15)',
              }}
            >
              <button
                onClick={() => setActiveWorkflowRole('patient')}
                style={{
                  padding: '8px 22px',
                  borderRadius: 99,
                  border: 'none',
                  fontSize: '0.875rem',
                  fontWeight: 700,
                  cursor: 'pointer',
                  background: activeWorkflowRole === 'patient' ? 'var(--accent)' : 'transparent',
                  color: 'white',
                  transition: 'all 200ms ease',
                }}
              >
                For Patients
              </button>
              <button
                onClick={() => setActiveWorkflowRole('doctor')}
                style={{
                  padding: '8px 22px',
                  borderRadius: 99,
                  border: 'none',
                  fontSize: '0.875rem',
                  fontWeight: 700,
                  cursor: 'pointer',
                  background: activeWorkflowRole === 'doctor' ? 'var(--accent)' : 'transparent',
                  color: 'white',
                  transition: 'all 200ms ease',
                }}
              >
                For Healthcare Providers
              </button>
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: 28 }}>
            {activeWorkflowRole === 'patient' ? (
              <>
                <div style={{ background: 'rgba(255, 255, 255, 0.05)', padding: 32, borderRadius: 'var(--radius-lg)', border: '1px solid rgba(255, 255, 255, 0.1)' }}>
                  <div style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 800, color: 'var(--color-teal-200)', marginBottom: 14 }}>01</div>
                  <h3 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: 8, color: 'white' }}>Create Patient Profile</h3>
                  <p style={{ color: 'rgba(255, 255, 255, 0.75)', fontSize: '0.9375rem', lineHeight: 1.6 }}>
                    Sign up with secure authentication and complete your health history baseline in under two minutes.
                  </p>
                </div>

                <div style={{ background: 'rgba(255, 255, 255, 0.05)', padding: 32, borderRadius: 'var(--radius-lg)', border: '1px solid rgba(255, 255, 255, 0.1)' }}>
                  <div style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 800, color: 'var(--color-teal-200)', marginBottom: 14 }}>02</div>
                  <h3 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: 8, color: 'white' }}>Book & Attend Session</h3>
                  <p style={{ color: 'rgba(255, 255, 255, 0.75)', fontSize: '0.9375rem', lineHeight: 1.6 }}>
                    Select your preferred specialist and time slot to receive virtual consultation details instantly.
                  </p>
                </div>

                <div style={{ background: 'rgba(255, 255, 255, 0.05)', padding: 32, borderRadius: 'var(--radius-lg)', border: '1px solid rgba(255, 255, 255, 0.1)' }}>
                  <div style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 800, color: 'var(--color-teal-200)', marginBottom: 14 }}>03</div>
                  <h3 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: 8, color: 'white' }}>Receive Prescriptions</h3>
                  <p style={{ color: 'rgba(255, 255, 255, 0.75)', fontSize: '0.9375rem', lineHeight: 1.6 }}>
                    Access electronic prescriptions, doctor follow-up notes, and diagnostic records directly in your portal.
                  </p>
                </div>
              </>
            ) : (
              <>
                <div style={{ background: 'rgba(255, 255, 255, 0.05)', padding: 32, borderRadius: 'var(--radius-lg)', border: '1px solid rgba(255, 255, 255, 0.1)' }}>
                  <div style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 800, color: 'var(--color-teal-200)', marginBottom: 14 }}>01</div>
                  <h3 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: 8, color: 'white' }}>Verify Credentials</h3>
                  <p style={{ color: 'rgba(255, 255, 255, 0.75)', fontSize: '0.9375rem', lineHeight: 1.6 }}>
                    Submit medical license credentials for verification to join the accredited CarePoint specialist network.
                  </p>
                </div>

                <div style={{ background: 'rgba(255, 255, 255, 0.05)', padding: 32, borderRadius: 'var(--radius-lg)', border: '1px solid rgba(255, 255, 255, 0.1)' }}>
                  <div style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 800, color: 'var(--color-teal-200)', marginBottom: 14 }}>02</div>
                  <h3 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: 8, color: 'white' }}>Set Availability Slots</h3>
                  <p style={{ color: 'rgba(255, 255, 255, 0.75)', fontSize: '0.9375rem', lineHeight: 1.6 }}>
                    Manage weekly consultation hours, consultation fees, and emergency slot overrides easily.
                  </p>
                </div>

                <div style={{ background: 'rgba(255, 255, 255, 0.05)', padding: 32, borderRadius: 'var(--radius-lg)', border: '1px solid rgba(255, 255, 255, 0.1)' }}>
                  <div style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 800, color: 'var(--color-teal-200)', marginBottom: 14 }}>03</div>
                  <h3 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: 8, color: 'white' }}>Conduct Sessions & Issue Rx</h3>
                  <p style={{ color: 'rgba(255, 255, 255, 0.75)', fontSize: '0.9375rem', lineHeight: 1.6 }}>
                    Review patient charts, conduct telehealth consultations, and generate official digital prescriptions.
                  </p>
                </div>
              </>
            )}
          </div>
        </div>
      </section>

      {/* Specialties Grid Section */}
      <section id="specialties" style={{ padding: '80px 24px', maxWidth: 1240, margin: '0 auto' }}>
        <div style={{ textAlign: 'center', maxWidth: 640, margin: '0 auto 48px' }}>
          <span className="badge badge-teal" style={{ marginBottom: 12 }}>CLINICAL SPECIALTIES</span>
          <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '2.35rem', fontWeight: 800, color: 'var(--color-teal-950)', letterSpacing: '-0.02em' }}>
            Explore Medical Domains
          </h2>
          <p style={{ color: 'var(--text-secondary)', marginTop: 8, fontSize: '1.05rem' }}>
            Connect with board-certified doctors across all clinical domains.
          </p>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: 20 }}>
          {filteredSpecialties.map((s) => (
            <div
              key={s.name}
              className="glass-card glass-card-hover"
              style={{
                padding: 24,
                borderRadius: 'var(--radius-lg)',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                cursor: 'pointer',
                border: selectedSpecialty === s.name ? '2px solid var(--accent)' : '1px solid rgba(229, 226, 222, 0.8)',
                background: selectedSpecialty === s.name ? 'var(--accent-light)' : 'var(--bg-surface)',
                boxShadow: selectedSpecialty === s.name ? 'var(--shadow-md)' : 'var(--shadow-sm)'
              }}
              onClick={() => setSelectedSpecialty(s.name)}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
                <div
                  style={{
                    width: 44,
                    height: 44,
                    borderRadius: 12,
                    background: 'var(--color-teal-50)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    boxShadow: '0 4px 10px rgba(26, 143, 133, 0.1)',
                  }}
                >
                  {s.icon}
                </div>
                <div>
                  <h4 style={{ fontFamily: 'var(--font-display)', fontSize: '1.1rem', fontWeight: 800, color: 'var(--color-teal-950)' }}>
                    {s.name}
                  </h4>
                  <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: 2 }}>{s.desc}</p>
                </div>
              </div>
              <span className="badge badge-teal" style={{ whiteSpace: 'nowrap', fontWeight: 700, fontSize: '0.78125rem' }}>
                {s.count}
              </span>
            </div>
          ))}
        </div>
      </section>

      {/* Bottom CTA Banner */}
      <section style={{ padding: '40px 24px 80px', maxWidth: 1240, margin: '0 auto' }}>
        <div
          className="glass-card"
          style={{
            padding: '56px 36px',
            textAlign: 'center',
            background: 'linear-gradient(135deg, var(--color-teal-950) 0%, var(--color-teal-900) 60%, var(--color-teal-800) 100%)',
            color: 'white',
            borderRadius: 'var(--radius-xl)',
            boxShadow: '0 25px 50px -12px rgba(6, 43, 40, 0.35)',
            border: '1px solid rgba(255, 255, 255, 0.15)',
            position: 'relative',
            overflow: 'hidden',
          }}
        >
          <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '2.5rem', fontWeight: 800, marginBottom: 16, letterSpacing: '-0.02em' }}>
            Ready for Effortless Healthcare?
          </h2>
          <p style={{ fontSize: '1.125rem', color: 'var(--color-teal-100)', maxWidth: 560, margin: '0 auto 32px', lineHeight: 1.6 }}>
            Join thousands of patients and certified doctors using CarePoint for streamlined virtual consultations.
          </p>
          <div style={{ display: 'flex', justifyContent: 'center', gap: 16, flexWrap: 'wrap' }}>
            <Link
              to="/register"
              className="btn glow-btn"
              style={{
                padding: '14px 30px',
                fontSize: '0.96875rem',
                fontWeight: 700,
                background: 'white',
                color: 'var(--color-teal-950)',
                borderRadius: 'var(--radius-md)',
                boxShadow: '0 4px 14px rgba(0, 0, 0, 0.15)',
                display: 'inline-flex',
                alignItems: 'center',
                gap: 8
              }}
            >
              <span>Create Patient Account</span>
              <ArrowRightIcon size={16} />
            </Link>
            <Link
              to="/register"
              className="btn"
              style={{
                padding: '14px 30px',
                fontSize: '0.96875rem',
                fontWeight: 700,
                color: 'white',
                border: '1.5px solid rgba(255, 255, 255, 0.4)',
                background: 'rgba(255, 255, 255, 0.08)',
                borderRadius: 'var(--radius-md)',
              }}
            >
              Join as Healthcare Provider
            </Link>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer style={{ borderTop: '1px solid var(--border-default)', background: 'var(--bg-surface)', padding: '44px 24px' }}>
        <div
          style={{
            maxWidth: 1240,
            margin: '0 auto',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            flexWrap: 'wrap',
            gap: 20,
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <div
              style={{
                width: 36,
                height: 36,
                borderRadius: 10,
                background: 'var(--accent)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'white',
              }}
            >
              <LogoIcon size={20} color="white" />
            </div>
            <span style={{ fontFamily: 'var(--font-display)', fontWeight: 800, fontSize: '1.2rem', color: 'var(--color-teal-950)' }}>
              CarePoint Telehealth
            </span>
          </div>

          <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', fontWeight: 500 }}>
            © {new Date().getFullYear()} CarePoint Medical Technologies Inc. All rights reserved.
          </div>
        </div>
      </footer>
    </div>
  );
}
