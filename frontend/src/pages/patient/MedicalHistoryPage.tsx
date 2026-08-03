import { useEffect, useState } from 'react';
import api from '../../api/client';
import type { MedicalRecordDto, ApiResponse } from '../../types';
import { FileTextIcon, SearchIcon, DoctorIcon, CalendarIcon, PillIcon } from '../../components/common/Icons';
import PaginationControls from '../../components/common/PaginationControls';
import useDebouncedValue from '../../hooks/useDebouncedValue';

const PAGE_SIZE = 20;

export default function MedicalHistoryPage() {
  const [records, setRecords] = useState<MedicalRecordDto[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [skip, setSkip] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const debouncedSearch = useDebouncedValue(searchQuery);

  useEffect(() => {
    const controller = new AbortController();
    async function fetchHistory() {
      try {
        setLoading(true);
        const params = new URLSearchParams({
          skip: String(skip), take: String(PAGE_SIZE), search: debouncedSearch,
        });
        const res = await api.get<ApiResponse<MedicalRecordDto[]>>(`/medicalrecords/my-history?${params}`, {
          signal: controller.signal,
        });
        setRecords(res.data.data || []);
        setTotalCount(res.data.pagination?.totalCount ?? res.data.data?.length ?? 0);
        setError(null);
      } catch (err: any) {
        if (controller.signal.aborted) return;
        console.error('Failed to load medical history', err);
        setError(err.response?.data?.message || 'Failed to load medical records.');
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    }
    fetchHistory();
    return () => controller.abort();
  }, [debouncedSearch, skip]);

  const filteredRecords = records;

  return (
    <div className="page-enter" style={{ maxWidth: 960, margin: '0 auto', display: 'flex', flexDirection: 'column', gap: 32 }}>
      {/* Header Banner */}
      <div className="card" style={{ padding: 32, display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: 'linear-gradient(135deg, #ffffff 0%, var(--accent-light) 100%)' }}>
        <div>
          <span className="badge badge-teal" style={{ marginBottom: 8 }}>Patient Medical Profile</span>
          <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 700 }}>
            Medical History
          </h1>
          <p style={{ color: 'var(--text-secondary)', marginTop: 4 }}>
            Review past clinical diagnoses, treatment plans, and notes from your consultations.
          </p>
        </div>

        <div style={{
          width: 64, height: 64, borderRadius: 16,
          background: 'var(--color-teal-800)',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          color: 'white',
          boxShadow: 'var(--shadow-md)'
        }}>
          <FileTextIcon size={32} color="white" />
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
            placeholder="Search by diagnosis, doctor, or treatment..."
            value={searchQuery}
            onChange={(e) => { setSearchQuery(e.target.value); setSkip(0); }}
            style={{ paddingLeft: 42 }}
          />
        </div>
        <span style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', fontWeight: 500, whiteSpace: 'nowrap' }}>
          {totalCount} records found
        </span>
      </div>

      {/* History Records List */}
      {loading ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div className="skeleton" style={{ height: 160 }} />
          <div className="skeleton" style={{ height: 160 }} />
        </div>
      ) : error ? (
        <div className="card" style={{ padding: 32, textAlign: 'center', color: 'var(--color-rose-600)' }}>
          {error}
        </div>
      ) : filteredRecords.length === 0 ? (
        <div className="card" style={{ padding: 48, textAlign: 'center', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 12 }}>
          <div style={{ padding: 16, borderRadius: 99, background: 'var(--bg-subtle)' }}>
            <FileTextIcon size={36} color="var(--text-secondary)" />
          </div>
          <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 600 }}>
            No Medical History Found
          </h3>
          <p style={{ color: 'var(--text-secondary)', maxWidth: 420 }}>
            {searchQuery
              ? 'No medical records matched your search terms.'
              : 'Medical records created by your doctors after completed consultations will appear here.'}
          </p>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
          {filteredRecords.map((record) => (
            <div key={record.id} className="card" style={{ padding: 28, display: 'flex', flexDirection: 'column', gap: 16, borderLeft: '4px solid var(--accent)' }}>
              {/* Card Header */}
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div>
                  <span style={{ fontSize: '0.75rem', textTransform: 'uppercase', letterSpacing: '0.06em', color: 'var(--text-secondary)', fontWeight: 600 }}>
                    Clinical Diagnosis
                  </span>
                  <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.375rem', fontWeight: 700, color: 'var(--color-teal-900)', marginTop: 2 }}>
                    {record.diagnosis}
                  </h3>
                </div>

                <div style={{ textAlign: 'right' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: '0.875rem', fontWeight: 500, color: 'var(--text-primary)' }}>
                    <DoctorIcon size={16} color="var(--accent)" />
                    {record.doctorName || 'Attending Physician'}
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: '0.8125rem', color: 'var(--text-secondary)', marginTop: 4, justifyContent: 'flex-end' }}>
                    <CalendarIcon size={14} color="currentColor" />
                    {new Date(record.appointmentDate || record.createdAt).toLocaleDateString(undefined, {
                      year: 'numeric',
                      month: 'long',
                      day: 'numeric'
                    })}
                  </div>
                </div>
              </div>

              {/* Treatment Section */}
              {record.treatment && (
                <div style={{ padding: 14, background: 'var(--color-teal-50)', borderRadius: 'var(--radius-md)', border: '1px solid var(--color-teal-100)' }}>
                  <div style={{ fontSize: '0.8125rem', fontWeight: 600, color: 'var(--color-teal-800)', marginBottom: 4, display: 'flex', alignItems: 'center', gap: 6 }}>
                    <PillIcon size={16} color="var(--color-teal-800)" />
                    Treatment Plan / Action Required:
                  </div>
                  <div style={{ fontSize: '0.9375rem', color: 'var(--color-teal-950)', lineHeight: 1.5 }}>
                    {record.treatment}
                  </div>
                </div>
              )}

              {/* Clinical Notes */}
              {record.notes && (
                <div style={{ fontSize: '0.9375rem', color: 'var(--text-secondary)', lineHeight: 1.6, borderTop: '1px dashed var(--border-default)', paddingTop: 14 }}>
                  <strong style={{ color: 'var(--text-primary)' }}>Doctor Notes: </strong>
                  {record.notes}
                </div>
              )}
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
