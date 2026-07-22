import { useEffect, useState } from 'react';
import api from '../../api/client';
import type { DoctorDto, ApiResponse } from '../../types';
import doctorPortrait from '../../assets/doctor_portrait.png';
import { DoctorIcon, CheckIcon, ClockIcon, CheckCircleIcon, XCircleIcon } from '../../components/common/Icons';

export default function AdminDashboard() {
  const [doctors, setDoctors] = useState<DoctorDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<'all' | 'pending' | 'approved' | 'rejected'>('all');
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchDoctors();
  }, []);

  const fetchDoctors = async () => {
    try {
      setLoading(true);
      let res;
      try {
        res = await api.get<ApiResponse<DoctorDto[]>>('/doctors/admin/all');
      } catch {
        res = await api.get<ApiResponse<DoctorDto[]>>('/doctors/all');
      }
      setDoctors(res.data.data || []);
    } catch (err: any) {
      console.error('Failed to fetch doctors', err);
      setError(err.response?.data?.message || 'Failed to load doctor applications.');
    } finally {
      setLoading(false);
    }
  };

  const handleApprove = async (id: string) => {
    setActionSuccess(null);
    setError(null);
    setActionLoading(id);
    try {
      await api.put(`/doctors/${id}/approve`);
      setActionSuccess('Doctor application approved successfully!');
      fetchDoctors();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to approve doctor.');
    } finally {
      setActionLoading(null);
    }
  };

  const handleReject = async (id: string) => {
    setActionSuccess(null);
    setError(null);
    setActionLoading(id);
    try {
      await api.put(`/doctors/${id}/reject`);
      setActionSuccess('Doctor application rejected.');
      fetchDoctors();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to reject doctor.');
    } finally {
      setActionLoading(null);
    }
  };

  const pendingCount = doctors.filter((d) => d.approvalStatus === 0).length;
  const approvedCount = doctors.filter((d) => d.approvalStatus === 1).length;
  const rejectedCount = doctors.filter((d) => d.approvalStatus === 2).length;

  const filteredDoctors = doctors.filter((d) => {
    if (filter === 'pending') return d.approvalStatus === 0;
    if (filter === 'approved') return d.approvalStatus === 1;
    if (filter === 'rejected') return d.approvalStatus === 2;
    return true;
  });

  return (
    <div className="page-enter" style={{ display: 'flex', flexDirection: 'column', gap: 28 }}>
      <div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6 }}>
          <span className="badge badge-amber">Admin Portal</span>
        </div>
        <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '1.875rem', fontWeight: 700 }}>
          Practitioner Approvals & Overview
        </h1>
        <p style={{ color: 'var(--text-secondary)', marginTop: 4 }}>
          Review doctor registrations, verify clinical credentials, and grant system access.
        </p>
      </div>

      {actionSuccess && (
        <div style={{ padding: '12px 16px', background: 'var(--color-teal-100)', color: 'var(--color-teal-800)', borderRadius: 'var(--radius-md)', display: 'flex', alignItems: 'center', gap: 8, fontSize: '0.9375rem' }}>
          <CheckIcon size={16} /> {actionSuccess}
        </div>
      )}

      {error && (
        <div style={{ padding: '12px 16px', background: 'var(--color-rose-100)', color: 'var(--color-rose-700)', borderRadius: 'var(--radius-md)', fontSize: '0.9375rem' }}>
          {error}
        </div>
      )}

      {/* Metrics Row */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: 20 }}>
        <div className="card glass-card" style={{ padding: '24px 22px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', borderTop: '3px solid var(--text-primary)' }}>
          <div>
            <div style={{ fontSize: '0.8125rem', fontWeight: 600, color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
              Total Registered
            </div>
            <div style={{ fontSize: '2rem', fontWeight: 800, fontFamily: 'var(--font-display)', marginTop: 6, color: 'var(--text-primary)' }}>
              {doctors.length}
            </div>
          </div>
          <div style={{ width: 44, height: 44, borderRadius: 12, background: 'var(--bg-subtle)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <DoctorIcon size={22} color="var(--text-primary)" />
          </div>
        </div>

        <div className="card glass-card" style={{ padding: '24px 22px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', borderTop: '3px solid #f59e0b', background: 'linear-gradient(180deg, rgba(254,243,199,0.25) 0%, var(--bg-surface) 100%)' }}>
          <div>
            <div style={{ fontSize: '0.8125rem', fontWeight: 600, color: '#b45309', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
              Pending Review
            </div>
            <div style={{ fontSize: '2rem', fontWeight: 800, fontFamily: 'var(--font-display)', marginTop: 6, color: '#d97706' }}>
              {pendingCount}
            </div>
          </div>
          <div style={{ width: 44, height: 44, borderRadius: 12, background: '#fef3c7', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <ClockIcon size={22} color="#b45309" />
          </div>
        </div>

        <div className="card glass-card" style={{ padding: '24px 22px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', borderTop: '3px solid var(--accent)', background: 'linear-gradient(180deg, var(--color-teal-50) 0%, var(--bg-surface) 100%)' }}>
          <div>
            <div style={{ fontSize: '0.8125rem', fontWeight: 600, color: 'var(--color-teal-800)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
              Approved Doctors
            </div>
            <div style={{ fontSize: '2rem', fontWeight: 800, fontFamily: 'var(--font-display)', marginTop: 6, color: 'var(--accent)' }}>
              {approvedCount}
            </div>
          </div>
          <div style={{ width: 44, height: 44, borderRadius: 12, background: 'var(--color-teal-100)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <CheckCircleIcon size={22} color="var(--accent)" />
          </div>
        </div>

        <div className="card glass-card" style={{ padding: '24px 22px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', borderTop: '3px solid var(--color-rose-600)', background: 'linear-gradient(180deg, var(--color-rose-50) 0%, var(--bg-surface) 100%)' }}>
          <div>
            <div style={{ fontSize: '0.8125rem', fontWeight: 600, color: 'var(--color-rose-700)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
              Rejected
            </div>
            <div style={{ fontSize: '2rem', fontWeight: 800, fontFamily: 'var(--font-display)', marginTop: 6, color: 'var(--color-rose-600)' }}>
              {rejectedCount}
            </div>
          </div>
          <div style={{ width: 44, height: 44, borderRadius: 12, background: 'var(--color-rose-100)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <XCircleIcon size={22} color="var(--color-rose-600)" />
          </div>
        </div>
      </div>

      {/* Doctors List Card */}
      <div className="card" style={{ padding: 24 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20, flexWrap: 'wrap', gap: 12 }}>
          <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.125rem', fontWeight: 700 }}>
            Doctor Applications ({filteredDoctors.length})
          </h2>

          <div style={{ display: 'flex', gap: 8 }}>
            {(['all', 'pending', 'approved', 'rejected'] as const).map((f) => (
              <button
                key={f}
                className={`btn ${filter === f ? 'btn-primary' : 'btn-ghost'}`}
                onClick={() => setFilter(f)}
                style={{ padding: '6px 14px', fontSize: '0.8125rem', textTransform: 'capitalize' }}
              >
                {f}
              </button>
            ))}
          </div>
        </div>

        {loading ? (
          <div className="skeleton" style={{ height: 200 }} />
        ) : filteredDoctors.length === 0 ? (
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.9375rem', textAlign: 'center', padding: '36px 0' }}>
            No doctor accounts match this filter.
          </p>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
            {filteredDoctors.map((doc) => {
              const statusBadge = doc.approvalStatus === 1 ? (
                <span className="badge badge-teal" style={{ display: 'flex', alignItems: 'center', gap: 4 }}><CheckCircleIcon size={14} /> Approved</span>
              ) : doc.approvalStatus === 2 ? (
                <span className="badge badge-rose" style={{ display: 'flex', alignItems: 'center', gap: 4 }}><XCircleIcon size={14} /> Rejected</span>
              ) : (
                <span className="badge badge-amber" style={{ display: 'flex', alignItems: 'center', gap: 4 }}><ClockIcon size={14} /> Pending Approval</span>
              );

              return (
                <div
                  key={doc.id}
                  style={{
                    padding: 20,
                    borderRadius: 'var(--radius-md)',
                    border: '1px solid var(--border-default)',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    gap: 16
                  }}
                >
                  <div style={{ display: 'flex', gap: 16, alignItems: 'center' }}>
                    <img
                      src={doc.profilePictureUrl || doctorPortrait}
                      alt={doc.firstName}
                      style={{ width: 64, height: 64, borderRadius: 99, objectFit: 'cover', border: '2px solid var(--border-default)' }}
                      onError={(e) => { (e.target as HTMLImageElement).src = doctorPortrait; }}
                    />

                    <div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                        <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.125rem', fontWeight: 700 }}>
                          Dr. {doc.firstName} {doc.lastName}
                        </h3>
                        {statusBadge}
                      </div>

                      <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: 2 }}>
                        ✉️ {doc.email} • 📞 {doc.phoneNumber || 'No phone'} • Fee: {doc.consultationFee} JOD
                      </div>

                      <div style={{ display: 'flex', gap: 6, marginTop: 8 }}>
                        {doc.specialties && doc.specialties.length > 0 ? (
                          doc.specialties.map((s) => (
                            <span key={s.id} className="badge badge-teal" style={{ fontSize: '0.75rem' }}>{s.name}</span>
                          ))
                        ) : (
                          <span style={{ fontSize: '0.78125rem', color: 'var(--text-secondary)', fontStyle: 'italic' }}>No specialties assigned</span>
                        )}
                      </div>
                    </div>
                  </div>

                  {/* Actions */}
                  <div style={{ display: 'flex', gap: 8 }}>
                    {doc.approvalStatus !== 1 && (
                      <button
                        className="btn btn-primary"
                        disabled={actionLoading === doc.id}
                        onClick={() => handleApprove(doc.id)}
                        style={{ fontSize: '0.85rem', gap: 6 }}
                      >
                        <CheckIcon size={14} color="white" /> Approve
                      </button>
                    )}

                    {doc.approvalStatus !== 2 && (
                      <button
                        className="btn btn-ghost"
                        disabled={actionLoading === doc.id}
                        onClick={() => handleReject(doc.id)}
                        style={{ fontSize: '0.85rem', color: 'var(--color-rose-600)' }}
                      >
                        Reject
                      </button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
