import { useCallback, useEffect, useState } from 'react';
import api from '../../api/client';
import type { DoctorDto, DoctorAdminSummaryDto, ClinicDto, ApiResponse } from '../../types';
import doctorPortrait from '../../assets/doctor_portrait.png';
import { BuildingIcon, DoctorIcon, CheckIcon, ClockIcon, CheckCircleIcon, XCircleIcon } from '../../components/common/Icons';
import PaginationControls from '../../components/common/PaginationControls';
import ApiImage from '../../components/common/ApiImage';

const PAGE_SIZE = 20;

export default function AdminDashboard() {
  const [doctors, setDoctors] = useState<DoctorDto[]>([]);
  const [clinics, setClinics] = useState<ClinicDto[]>([]);
  const [clinicAssignments, setClinicAssignments] = useState<Record<string, string[]>>({});
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<'all' | 'pending' | 'approved' | 'rejected'>('all');
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [skip, setSkip] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [summary, setSummary] = useState<DoctorAdminSummaryDto>({
    totalRegistered: 0,
    pendingCount: 0,
    approvedCount: 0,
    rejectedCount: 0,
  });
  const [reviewing, setReviewing] = useState<{ doctor: DoctorDto; action: 'approve' | 'reject' } | null>(null);
  const [reviewNotes, setReviewNotes] = useState('');

  const fetchDoctors = useCallback(async () => {
    try {
      setLoading(true);
      const status = filter === 'all'
        ? ''
        : `&status=${filter === 'pending' ? 0 : filter === 'approved' ? 1 : 2}`;
      const [res, summaryRes, clinicRes] = await Promise.all([
        api.get<ApiResponse<DoctorDto[]>>(`/doctors/admin/all?skip=${skip}&take=${PAGE_SIZE}${status}`),
        api.get<ApiResponse<DoctorAdminSummaryDto>>('/doctors/admin/summary'),
        api.get<ApiResponse<ClinicDto[]>>('/clinics'),
      ]);
      const loadedDoctors = res.data.data || [];
      setDoctors(loadedDoctors);
      setClinics(clinicRes.data.data || []);
      setClinicAssignments(Object.fromEntries(
        loadedDoctors.map((doctor) => [doctor.id, doctor.clinics.map((clinic) => clinic.id)]),
      ));
      setTotalCount(res.data.pagination?.totalCount ?? res.data.data?.length ?? 0);
      setSummary(summaryRes.data.data);
    } catch (err: any) {
      console.error('Failed to fetch doctors', err);
      setError(err.response?.data?.message || 'Failed to load doctor applications.');
    } finally {
      setLoading(false);
    }
  }, [filter, skip]);

  useEffect(() => {
    fetchDoctors();
  }, [fetchDoctors]);

  const submitCredentialReview = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!reviewing || !reviewNotes.trim()) return;
    setActionSuccess(null);
    setError(null);
    setActionLoading(reviewing.doctor.id);
    try {
      await api.put(`/doctors/${reviewing.doctor.id}/${reviewing.action}`, {
        notes: reviewNotes.trim(),
        credentials: {
          licenseNumber: reviewing.doctor.licenseNumber ?? null,
          licensingAuthority: reviewing.doctor.licensingAuthority ?? null,
          licenseExpiryDate: reviewing.doctor.licenseExpiryDate ?? null,
        },
      });
      setActionSuccess(
        reviewing.action === 'approve'
          ? 'Doctor credentials reviewed and approved.'
          : 'Doctor credentials reviewed and rejected.',
      );
      setReviewing(null);
      setReviewNotes('');
      await fetchDoctors();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to record the credential review.');
    } finally {
      setActionLoading(null);
    }
  };

  const saveClinicAssignments = async (doctor: DoctorDto) => {
    setActionSuccess(null);
    setError(null);
    setActionLoading(doctor.id);
    try {
      await api.put(`/doctors/${doctor.id}/clinics`, {
        clinicIds: clinicAssignments[doctor.id] ?? [],
      });
      setActionSuccess(`Clinic locations updated for Dr. ${doctor.lastName}.`);
      await fetchDoctors();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to update clinic assignments.');
    } finally {
      setActionLoading(null);
    }
  };

  const filteredDoctors = doctors;

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
              {summary.totalRegistered}
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
              {summary.pendingCount}
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
              {summary.approvedCount}
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
              {summary.rejectedCount}
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
            Doctor Applications ({totalCount})
          </h2>

          <div style={{ display: 'flex', gap: 8 }}>
            {(['all', 'pending', 'approved', 'rejected'] as const).map((f) => (
              <button
                key={f}
                className={`btn ${filter === f ? 'btn-primary' : 'btn-ghost'}`}
                onClick={() => { setFilter(f); setSkip(0); }}
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
                    <ApiImage
                      source={doc.profilePictureUrl}
                      fallback={doctorPortrait}
                      authenticated
                      alt={doc.firstName}
                      style={{ width: 64, height: 64, borderRadius: 99, objectFit: 'cover', border: '2px solid var(--border-default)' }}
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

                      <div style={{ marginTop: 8, padding: '10px 12px', background: 'var(--bg-subtle)', borderRadius: 'var(--radius-sm)', fontSize: '0.8125rem' }}>
                        <strong>License:</strong>{' '}
                        {doc.licenseNumber && doc.licensingAuthority && doc.licenseExpiryDate
                          ? `${doc.licenseNumber} · ${doc.licensingAuthority} · expires ${new Date(doc.licenseExpiryDate).toLocaleDateString()}`
                          : 'Credential details incomplete'}
                        {doc.credentialsReviewedAt && (
                          <div style={{ color: 'var(--text-secondary)', marginTop: 4 }}>
                            Reviewed {new Date(doc.credentialsReviewedAt).toLocaleString()}
                            {doc.credentialReviewNotes ? ` · ${doc.credentialReviewNotes}` : ''}
                          </div>
                        )}
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

                      <details style={{ marginTop: 12 }}>
                        <summary style={{ cursor: 'pointer', color: 'var(--accent)', fontSize: '0.8125rem', fontWeight: 600 }}>
                          <BuildingIcon size={14} /> Manage clinic locations
                        </summary>
                        <div style={{ display: 'grid', gap: 8, marginTop: 10, maxWidth: 360 }}>
                          <label className="form-group">
                            <span className="form-label">Assigned clinics</span>
                            <select
                              className="form-input"
                              multiple
                              size={Math.min(Math.max(clinics.length, 2), 5)}
                              value={clinicAssignments[doc.id] ?? []}
                              onChange={(event) => setClinicAssignments((current) => ({
                                ...current,
                                [doc.id]: Array.from(event.currentTarget.selectedOptions, (option) => option.value),
                              }))}
                            >
                              {clinics.map((clinic) => (
                                <option key={clinic.id} value={clinic.id}>
                                  {clinic.name}{clinic.city ? ` — ${clinic.city}` : ''}
                                </option>
                              ))}
                            </select>
                          </label>
                          <span style={{ color: 'var(--text-secondary)', fontSize: '0.75rem' }}>
                            Use Ctrl/Cmd to select multiple locations. Future appointments protect assignments from removal.
                          </span>
                          <button
                            className="btn btn-secondary"
                            type="button"
                            disabled={actionLoading === doc.id}
                            onClick={() => saveClinicAssignments(doc)}
                          >
                            Save locations
                          </button>
                        </div>
                      </details>
                    </div>
                  </div>

                  {/* Actions */}
                  <div style={{ display: 'flex', gap: 8 }}>
                    {doc.approvalStatus !== 1 && (
                      <button
                        className="btn btn-primary"
                        disabled={actionLoading === doc.id}
                        onClick={() => { setReviewing({ doctor: doc, action: 'approve' }); setReviewNotes(''); }}
                        style={{ fontSize: '0.85rem', gap: 6 }}
                      >
                        <CheckIcon size={14} color="white" /> Approve
                      </button>
                    )}

                    {doc.approvalStatus !== 2 && (
                      <button
                        className="btn btn-ghost"
                        disabled={actionLoading === doc.id}
                        onClick={() => { setReviewing({ doctor: doc, action: 'reject' }); setReviewNotes(''); }}
                        style={{ fontSize: '0.85rem', color: 'var(--color-rose-600)' }}
                      >
                        Reject
                      </button>
                    )}
                  </div>
                </div>
              );
            })}
            <PaginationControls
              skip={skip}
              take={PAGE_SIZE}
              totalCount={totalCount}
              onPageChange={setSkip}
            />
          </div>
        )}
      </div>

      {reviewing && (
        <div className="admin-modal-backdrop" role="presentation">
          <form className="card admin-modal" role="dialog" aria-modal="true" aria-labelledby="credential-review-title" onSubmit={submitCredentialReview}>
            <div>
              <span className="badge badge-amber">Credential review</span>
              <h2 id="credential-review-title" style={{ marginTop: 10 }}>
                {reviewing.action === 'approve' ? 'Approve' : 'Reject'} Dr. {reviewing.doctor.firstName} {reviewing.doctor.lastName}
              </h2>
              <p style={{ color: 'var(--text-secondary)', marginTop: 6 }}>
                {reviewing.doctor.licenseNumber || 'No license number'} · {reviewing.doctor.licensingAuthority || 'No authority'}
              </p>
            </div>
            <label className="form-group">
              <span className="form-label">Review findings</span>
              <textarea
                className="form-input"
                rows={4}
                maxLength={1000}
                required
                value={reviewNotes}
                onChange={(event) => setReviewNotes(event.target.value)}
                placeholder="Record what was checked and the reason for this decision."
              />
            </label>
            <div className="admin-modal-actions">
              <button className="btn btn-ghost" type="button" onClick={() => setReviewing(null)} disabled={actionLoading === reviewing.doctor.id}>Cancel</button>
              <button className={reviewing.action === 'approve' ? 'btn btn-primary' : 'btn btn-danger'} type="submit" disabled={!reviewNotes.trim() || actionLoading === reviewing.doctor.id}>
                {actionLoading === reviewing.doctor.id ? 'Recording…' : `Record ${reviewing.action}`}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
