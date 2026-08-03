import { useEffect, useState } from 'react';
import api from '../../api/client';
import type { PrescriptionDto, ApiResponse } from '../../types';
import { PillIcon, DoctorIcon, CalendarIcon, SearchIcon, ClockIcon, InfoIcon, HourglassIcon } from '../../components/common/Icons';
import PaginationControls from '../../components/common/PaginationControls';
import useDebouncedValue from '../../hooks/useDebouncedValue';

const PAGE_SIZE = 20;

export default function MyPrescriptionsPage() {
  const [prescriptions, setPrescriptions] = useState<PrescriptionDto[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [skip, setSkip] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const debouncedSearch = useDebouncedValue(searchQuery);

  useEffect(() => {
    const controller = new AbortController();
    async function fetchPrescriptions() {
      try {
        setLoading(true);
        const params = new URLSearchParams({
          skip: String(skip), take: String(PAGE_SIZE), search: debouncedSearch,
        });
        const res = await api.get<ApiResponse<PrescriptionDto[]>>(`/prescriptions/my-prescriptions?${params}`, {
          signal: controller.signal,
        });
        setPrescriptions(res.data.data || []);
        setTotalCount(res.data.pagination?.totalCount ?? res.data.data?.length ?? 0);
        setError(null);
      } catch (err: any) {
        if (controller.signal.aborted) return;
        console.error('Failed to load prescriptions', err);
        setError(err.response?.data?.message || 'Failed to load digital prescriptions.');
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    }
    fetchPrescriptions();
    return () => controller.abort();
  }, [debouncedSearch, skip]);

  const filtered = prescriptions;

  return (
    <div className="page-enter" style={{ maxWidth: 960, margin: '0 auto', display: 'flex', flexDirection: 'column', gap: 32 }}>
      {/* Header Banner */}
      <div className="card" style={{ padding: 32, display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: 'linear-gradient(135deg, #ffffff 0%, var(--accent-light) 100%)' }}>
        <div>
          <span className="badge badge-teal" style={{ marginBottom: 8 }}>Digital Pharmacy</span>
          <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 700 }}>
            My Prescriptions
          </h1>
          <p style={{ color: 'var(--text-secondary)', marginTop: 4 }}>
            View electronic prescriptions, dosages, and pharmacy instructions issued by your doctors.
          </p>
        </div>

        <div style={{
          width: 64, height: 64, borderRadius: 16,
          background: 'var(--accent)',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          color: 'white',
          boxShadow: 'var(--shadow-md)'
        }}>
          <PillIcon size={32} color="white" />
        </div>
      </div>

      {/* Filter & Search Bar */}
      <div className="card" style={{ padding: 20, display: 'flex', alignItems: 'center', gap: 16 }}>
        <div style={{ position: 'relative', flex: 1 }}>
          <div style={{ position: 'absolute', left: 14, top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }}>
            <SearchIcon size={18} />
          </div>
          <input
            className="form-input"
            type="text"
            placeholder="Search by medication name, doctor, or notes..."
            value={searchQuery}
            onChange={(e) => { setSearchQuery(e.target.value); setSkip(0); }}
            style={{ paddingLeft: 42 }}
          />
        </div>
        <span style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', fontWeight: 500, whiteSpace: 'nowrap' }}>
          {totalCount} prescriptions
        </span>
      </div>

      {/* Prescriptions List */}
      {loading ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div className="skeleton" style={{ height: 180 }} />
          <div className="skeleton" style={{ height: 180 }} />
        </div>
      ) : error ? (
        <div className="card" style={{ padding: 32, textAlign: 'center', color: 'var(--color-rose-600)' }}>
          {error}
        </div>
      ) : filtered.length === 0 ? (
        <div className="card" style={{ padding: 48, textAlign: 'center', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 12 }}>
          <div style={{ padding: 16, borderRadius: 99, background: 'var(--bg-subtle)' }}>
            <PillIcon size={36} color="var(--text-secondary)" />
          </div>
          <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 600 }}>
            No Digital Prescriptions
          </h3>
          <p style={{ color: 'var(--text-secondary)', maxWidth: 420 }}>
            {searchQuery
              ? 'No prescriptions matched your search terms.'
              : 'Prescriptions issued by your doctor during consultations will be available here.'}
          </p>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
          {filtered.map((rx) => (
            <div key={rx.id} className="card" style={{ padding: 28, display: 'flex', flexDirection: 'column', gap: 20 }}>
              {/* Header info */}
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', borderBottom: '1px solid var(--border-default)', paddingBottom: 16 }}>
                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <DoctorIcon size={18} color="var(--accent)" />
                    <span style={{ fontWeight: 700, fontSize: '1.125rem', color: 'var(--color-teal-950)' }}>
                      {rx.doctorName || 'Attending Physician'}
                    </span>
                  </div>
                  {rx.notes && (
                    <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginTop: 4, fontStyle: 'italic' }}>
                      Note: "{rx.notes}"
                    </p>
                  )}
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: '0.8125rem', color: 'var(--text-secondary)' }}>
                  <CalendarIcon size={14} />
                  Issued {new Date(rx.createdAt).toLocaleDateString(undefined, {
                    year: 'numeric',
                    month: 'short',
                    day: 'numeric'
                  })}
                </div>
              </div>

              {/* Items grid */}
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))', gap: 14 }}>
                {rx.items.map((item) => (
                  <div key={item.id} style={{
                    padding: 16,
                    borderRadius: 'var(--radius-md)',
                    background: 'var(--color-teal-50)',
                    border: '1px solid var(--color-teal-200)',
                    display: 'flex',
                    flexDirection: 'column',
                    gap: 6
                  }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <span style={{ fontWeight: 700, fontSize: '1rem', color: 'var(--color-teal-900)', display: 'flex', alignItems: 'center', gap: 6 }}>
                        <PillIcon size={16} color="var(--accent)" />
                        {item.medicationName}
                      </span>
                      <span className="badge badge-teal" style={{ fontSize: '0.75rem' }}>
                        {item.dosage}
                      </span>
                    </div>

                    <div style={{ fontSize: '0.85rem', color: 'var(--text-primary)', fontWeight: 500, display: 'flex', alignItems: 'center', gap: 6 }}>
                      <ClockIcon size={14} color="var(--text-secondary)" />
                      <span>Frequency: <span style={{ color: 'var(--color-teal-800)' }}>{item.frequency}</span></span>
                    </div>

                    {item.duration && (
                      <div style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: 6 }}>
                        <HourglassIcon size={14} color="var(--text-secondary)" />
                        <span>Duration: {item.duration}</span>
                      </div>
                    )}

                    {item.instructions && (
                      <div style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', marginTop: 2, fontStyle: 'italic', display: 'flex', alignItems: 'center', gap: 6 }}>
                        <InfoIcon size={14} color="var(--accent)" />
                        <span>{item.instructions}</span>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          ))}
          <PaginationControls
            skip={skip}
            take={PAGE_SIZE}
            totalCount={totalCount}
            onPageChange={setSkip}
          />
        </div>
      )}
    </div>
  );
}
