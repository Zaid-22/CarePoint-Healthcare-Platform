import { useCallback, useEffect, useState } from 'react';
import api from '../../api/client';
import AdminPageHeader from '../../components/admin/AdminPageHeader';
import PaginationControls from '../../components/common/PaginationControls';
import { SearchIcon, UserIcon } from '../../components/common/Icons';
import type { ApiResponse, PatientDto } from '../../types';

const PAGE_SIZE = 20;

export default function AdminPatientsPage() {
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [skip, setSkip] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchPatients = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const params = new URLSearchParams({ skip: String(skip), take: String(PAGE_SIZE) });
      if (search) params.set('search', search);
      const response = await api.get<ApiResponse<PatientDto[]>>(`/patients?${params}`);
      setPatients(response.data.data ?? []);
      setTotalCount(response.data.pagination?.totalCount ?? response.data.data?.length ?? 0);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load patients.');
    } finally {
      setLoading(false);
    }
  }, [search, skip]);

  useEffect(() => {
    fetchPatients();
  }, [fetchPatients]);

  return (
    <div className="page-enter admin-page-stack">
      <AdminPageHeader
        eyebrow="Patient operations"
        title="Patient Directory"
        description="Locate patient profiles and verify contact and emergency details while clinical records remain protected in their dedicated workflows."
      />
      {error && <div className="admin-notice admin-notice--error">{error}</div>}

      <section className="card admin-resource-card">
        <div className="admin-resource-toolbar">
          <div>
            <h2>Registered patients</h2>
            <p>{totalCount} matching profile{totalCount === 1 ? '' : 's'}</p>
          </div>
          <form
            className="admin-filter-form"
            onSubmit={(event) => { event.preventDefault(); setSkip(0); setSearch(searchInput.trim()); }}
          >
            <label className="admin-search-field">
              <span className="sr-only">Search patients</span>
              <SearchIcon size={16} />
              <input
                className="form-input"
                value={searchInput}
                onChange={(event) => setSearchInput(event.target.value)}
                placeholder="Name or email"
                maxLength={200}
              />
            </label>
            <button className="btn btn-secondary" type="submit">Search</button>
          </form>
        </div>

        {loading ? (
          <div className="skeleton" style={{ height: 260 }} />
        ) : patients.length === 0 ? (
          <div className="admin-empty-state"><UserIcon size={38} /><p>No patient profiles match this search.</p></div>
        ) : (
          <div className="admin-resource-list" role="list">
            {patients.map((patient) => (
              <article className="admin-resource-row admin-patient-row" key={patient.id} role="listitem">
                <div className="admin-primary-cell">
                  <strong>{patient.firstName} {patient.lastName}</strong>
                  <span>{patient.email}</span>
                </div>
                <div className="admin-detail-cell"><span>Phone</span><strong>{patient.phoneNumber || 'Not provided'}</strong></div>
                <div className="admin-detail-cell"><span>Blood type</span><strong>{patient.bloodType || 'Not provided'}</strong></div>
                <div className="admin-detail-cell admin-detail-cell--wide"><span>Emergency contact</span><strong>{patient.emergencyContact || 'Not provided'}</strong></div>
              </article>
            ))}
          </div>
        )}

        <PaginationControls skip={skip} take={PAGE_SIZE} totalCount={totalCount} onPageChange={setSkip} />
      </section>
    </div>
  );
}
