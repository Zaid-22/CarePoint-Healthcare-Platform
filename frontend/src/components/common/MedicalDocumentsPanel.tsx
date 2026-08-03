import { FormEvent, useCallback, useEffect, useState } from 'react';
import api from '../../api/client';
import type { ApiResponse, MedicalDocumentDto } from '../../types';
import { FileTextIcon, ShieldLockIcon } from './Icons';

interface Props {
  patientProfileId: string;
  appointmentId?: string;
  allowDelete?: boolean;
  compact?: boolean;
}

const formatSize = (bytes: number) =>
  bytes < 1024 * 1024 ? `${Math.max(1, Math.round(bytes / 1024))} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`;

export default function MedicalDocumentsPanel({
  patientProfileId,
  appointmentId,
  allowDelete = false,
  compact = false,
}: Props) {
  const [documents, setDocuments] = useState<MedicalDocumentDto[]>([]);
  const [file, setFile] = useState<File | null>(null);
  const [documentType, setDocumentType] = useState('Lab result');
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');

  const loadDocuments = useCallback(async () => {
    setLoading(true);
    try {
      const response = await api.get<ApiResponse<MedicalDocumentDto[]>>(
        `/documents/patient/${patientProfileId}?take=100`,
      );
      setDocuments(response.data.data ?? []);
      setError('');
    } catch {
      setError('Documents could not be loaded.');
    } finally {
      setLoading(false);
    }
  }, [patientProfileId]);

  useEffect(() => {
    loadDocuments();
  }, [loadDocuments]);

  const upload = async (event: FormEvent) => {
    event.preventDefault();
    if (!file) return;
    const form = new FormData();
    form.append('patientProfileId', patientProfileId);
    if (appointmentId) form.append('appointmentId', appointmentId);
    form.append('documentType', documentType);
    form.append('file', file);
    setUploading(true);
    setError('');
    try {
      await api.post('/documents', form, { headers: { 'Content-Type': 'multipart/form-data' } });
      setFile(null);
      await loadDocuments();
    } catch (requestError: any) {
      setError(requestError.response?.data?.message ?? 'The document could not be uploaded.');
    } finally {
      setUploading(false);
    }
  };

  const download = async (document: MedicalDocumentDto) => {
    try {
      const response = await api.get(document.downloadUrl.replace(/^\/api/, ''), { responseType: 'blob' });
      const url = URL.createObjectURL(response.data);
      const anchor = window.document.createElement('a');
      anchor.href = url;
      anchor.download = document.fileName;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch {
      setError('The protected file could not be downloaded.');
    }
  };

  const remove = async (documentId: string) => {
    try {
      await api.delete(`/documents/${documentId}`);
      setDocuments((current) => current.filter((item) => item.id !== documentId));
    } catch {
      setError('The document could not be deleted.');
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: compact ? 16 : 24 }}>
      <div style={{
        display: 'flex', gap: 12, alignItems: 'center', padding: '14px 16px',
        borderRadius: 'var(--radius-md)', background: 'var(--color-teal-50)',
        border: '1px solid var(--color-teal-200)', color: 'var(--color-teal-900)',
      }}>
        <ShieldLockIcon size={22} />
        <div>
          <div style={{ fontWeight: 700 }}>Protected clinical files</div>
          <div style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)' }}>
            Downloads are streamed only after your identity and care relationship are verified.
          </div>
        </div>
      </div>

      <form onSubmit={upload} className="card" style={{ padding: compact ? 18 : 24 }}>
        <div style={{ fontFamily: 'var(--font-display)', fontWeight: 700, marginBottom: 14 }}>
          Add a document
        </div>
        <div className={compact ? 'document-upload-grid is-compact' : 'document-upload-grid'}>
          <div className="form-group">
            <label className="form-label">Document type</label>
            <select className="form-input" value={documentType} onChange={(event) => setDocumentType(event.target.value)}>
              <option>Lab result</option>
              <option>Imaging report</option>
              <option>Referral</option>
              <option>Discharge summary</option>
              <option>Other</option>
            </select>
          </div>
          <div className="form-group">
            <label className="form-label">PDF or image · 10 MB max</label>
            <input
              className="form-input"
              type="file"
              accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
              onChange={(event) => setFile(event.target.files?.[0] ?? null)}
              required
            />
          </div>
          <button className="btn btn-primary" disabled={!file || uploading} type="submit">
            {uploading ? 'Uploading…' : 'Upload securely'}
          </button>
        </div>
      </form>

      {error && <div className="alert alert-error">{error}</div>}

      <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
        {loading ? (
          <div className="skeleton" style={{ height: 96 }} />
        ) : documents.length === 0 ? (
          <div className="card" style={{ padding: 28, textAlign: 'center', color: 'var(--text-secondary)' }}>
            No clinical documents have been added yet.
          </div>
        ) : documents.map((document) => (
          <div key={document.id} className="card document-row" style={{
            padding: 18, display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 16,
          }}>
            <div style={{ display: 'flex', gap: 12, alignItems: 'center', minWidth: 0 }}>
              <div style={{
                width: 40, height: 40, borderRadius: 9, background: 'var(--accent-light)',
                color: 'var(--accent)', display: 'grid', placeItems: 'center', flexShrink: 0,
              }}><FileTextIcon size={19} /></div>
              <div style={{ minWidth: 0 }}>
                <div style={{ fontWeight: 700, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                  {document.fileName}
                </div>
                <div style={{ color: 'var(--text-secondary)', fontSize: '0.8125rem', marginTop: 3 }}>
                  {document.documentType ?? 'Clinical document'} · {formatSize(document.fileSizeBytes)} · {new Date(document.createdAt).toLocaleDateString()}
                </div>
              </div>
            </div>
            <div className="document-actions" style={{ display: 'flex', gap: 8, flexShrink: 0 }}>
              <button className="btn btn-secondary" type="button" onClick={() => download(document)}>Download</button>
              {allowDelete && <button className="btn btn-ghost" type="button" onClick={() => remove(document.id)}>Delete</button>}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
