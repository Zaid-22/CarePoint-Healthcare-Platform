import { useCallback, useEffect, useState } from 'react';
import api from '../../api/client';
import type { ApiResponse, PrescriptionRevisionDto } from '../../types';

interface Props {
  prescriptionId: string;
  revisionVersion: string;
}

export default function PrescriptionRevisionHistory({ prescriptionId, revisionVersion }: Props) {
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [revisions, setRevisions] = useState<PrescriptionRevisionDto[]>([]);

  const loadRevisions = useCallback(async (signal: AbortSignal) => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get<ApiResponse<PrescriptionRevisionDto[]>>(
        `/prescriptions/${prescriptionId}/revisions`,
        { signal },
      );
      setRevisions(response.data.data ?? []);
    } catch (requestError: any) {
      if (signal.aborted) return;
      setError(requestError.response?.data?.message ?? 'Correction history could not be loaded.');
    } finally {
      if (!signal.aborted) setLoading(false);
    }
  }, [prescriptionId]);

  useEffect(() => {
    if (!open) return;

    const controller = new AbortController();
    void loadRevisions(controller.signal);
    return () => controller.abort();
  }, [loadRevisions, open, revisionVersion]);

  return (
    <div style={{ borderTop: '1px dashed var(--border-default)', paddingTop: 12 }}>
      <button
        type="button"
        className="btn btn-ghost"
        aria-expanded={open}
        onClick={() => setOpen((current) => !current)}
        style={{ padding: '6px 10px', fontSize: '0.8125rem' }}
      >
        {open ? 'Hide correction history' : 'View correction history'}
      </button>

      {open && (
        <div style={{ marginTop: 12 }}>
          {loading ? (
            <div className="skeleton" style={{ height: 72 }} />
          ) : error ? (
            <div className="alert alert-error">{error}</div>
          ) : revisions.length === 0 ? (
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.8125rem' }}>
              No corrections have been recorded.
            </p>
          ) : (
            <ol style={{ listStyle: 'none', display: 'grid', gap: 12 }}>
              {revisions.map((revision) => (
                <li
                  key={revision.id}
                  style={{
                    borderLeft: '3px solid var(--color-amber-500)',
                    padding: '10px 12px',
                    background: 'var(--bg-subtle)',
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
                    <strong style={{ fontSize: '0.875rem' }}>{revision.changeReason}</strong>
                    <span style={{ color: 'var(--text-secondary)', fontSize: '0.75rem' }}>
                      {new Date(revision.editedAt).toLocaleString()}
                    </span>
                  </div>
                  <div style={{ color: 'var(--text-secondary)', fontSize: '0.75rem', marginTop: 2 }}>
                    Corrected by {revision.editedByName}
                  </div>
                  {revision.notes && (
                    <p style={{ fontSize: '0.8125rem', marginTop: 8 }}>Previous note: {revision.notes}</p>
                  )}
                  <ul style={{ margin: '8px 0 0 18px', fontSize: '0.8125rem' }}>
                    {revision.items.map((item) => (
                      <li key={item.id}>
                        {item.medicationName} · {item.dosage} · {item.frequency}
                      </li>
                    ))}
                  </ul>
                </li>
              ))}
            </ol>
          )}
        </div>
      )}
    </div>
  );
}
