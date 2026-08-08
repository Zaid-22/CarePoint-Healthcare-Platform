import { useCallback, useEffect, useState } from 'react';
import api from '../../api/client';
import AdminPageHeader from '../../components/admin/AdminPageHeader';
import { BuildingIcon, CheckIcon, PlusIcon, SearchIcon } from '../../components/common/Icons';
import type { ApiResponse, ClinicDto } from '../../types';

interface ClinicForm {
  name: string;
  address: string;
  phoneNumber: string;
  city: string;
}

const emptyForm: ClinicForm = { name: '', address: '', phoneNumber: '', city: '' };

export default function AdminClinicsPage() {
  const [clinics, setClinics] = useState<ClinicDto[]>([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState<ClinicDto | null>(null);
  const [form, setForm] = useState<ClinicForm>(emptyForm);
  const [showForm, setShowForm] = useState(false);
  const [confirming, setConfirming] = useState<ClinicDto | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fetchClinics = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await api.get<ApiResponse<ClinicDto[]>>('/clinics');
      setClinics(response.data.data ?? []);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load clinics.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchClinics();
  }, [fetchClinics]);

  const openCreate = () => {
    setEditing(null);
    setForm(emptyForm);
    setError(null);
    setShowForm(true);
  };

  const openEdit = (clinic: ClinicDto) => {
    setEditing(clinic);
    setForm({
      name: clinic.name,
      address: clinic.address || '',
      phoneNumber: clinic.phoneNumber || '',
      city: clinic.city || '',
    });
    setError(null);
    setShowForm(true);
  };

  const saveClinic = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    setMessage(null);
    try {
      const payload = {
        name: form.name.trim(),
        address: form.address.trim() || null,
        phoneNumber: form.phoneNumber.trim() || null,
        city: form.city.trim() || null,
      };
      if (editing) await api.put(`/clinics/${editing.id}`, payload);
      else await api.post('/clinics', payload);
      setMessage(`Clinic ${editing ? 'updated' : 'created'} successfully.`);
      setShowForm(false);
      setEditing(null);
      setForm(emptyForm);
      await fetchClinics();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to save clinic.');
    } finally {
      setSubmitting(false);
    }
  };

  const deactivateClinic = async () => {
    if (!confirming) return;
    setSubmitting(true);
    setError(null);
    setMessage(null);
    try {
      await api.delete(`/clinics/${confirming.id}`);
      setMessage(`${confirming.name} was deactivated.`);
      setConfirming(null);
      await fetchClinics();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to deactivate clinic.');
    } finally {
      setSubmitting(false);
    }
  };

  const filteredClinics = clinics.filter((clinic) => {
    const needle = search.toLowerCase();
    return clinic.name.toLowerCase().includes(needle) ||
      (clinic.city || '').toLowerCase().includes(needle) ||
      (clinic.address || '').toLowerCase().includes(needle);
  });

  return (
    <div className="page-enter admin-page-stack">
      <AdminPageHeader
        eyebrow="Care locations"
        title="Clinic Management"
        description="Maintain the active clinic directory used by doctor profiles and patient discovery."
        action={<button className="btn btn-primary" type="button" onClick={openCreate}><PlusIcon size={17} /> Add clinic</button>}
      />

      {message && <div className="admin-notice admin-notice--success">{message}</div>}
      {error && <div className="admin-notice admin-notice--error">{error}</div>}

      <section className="card admin-resource-card">
        <div className="admin-resource-toolbar">
          <div><h2>Active clinics</h2><p>{filteredClinics.length} visible location{filteredClinics.length === 1 ? '' : 's'}</p></div>
          <label className="admin-search-field">
            <span className="sr-only">Search clinics</span>
            <SearchIcon size={16} />
            <input className="form-input" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Name, city, or address" />
          </label>
        </div>

        {loading ? (
          <div className="skeleton" style={{ height: 240 }} />
        ) : filteredClinics.length === 0 ? (
          <div className="admin-empty-state"><BuildingIcon size={38} /><p>No active clinics match this search.</p></div>
        ) : (
          <div className="admin-card-grid">
            {filteredClinics.map((clinic) => (
              <article className="admin-entity-card" key={clinic.id}>
                <div className="admin-entity-card-heading">
                  <span className="admin-icon-tile"><BuildingIcon size={20} /></span>
                  <span className="badge badge-teal">Active</span>
                </div>
                <div>
                  <h3>{clinic.name}</h3>
                  <p>{clinic.address || 'Address not provided'}</p>
                </div>
                <dl className="admin-entity-meta">
                  <div><dt>City</dt><dd>{clinic.city || 'Not provided'}</dd></div>
                  <div><dt>Phone</dt><dd>{clinic.phoneNumber || 'Not provided'}</dd></div>
                </dl>
                <div className="admin-entity-actions">
                  <button className="btn btn-secondary" type="button" onClick={() => openEdit(clinic)}>Edit</button>
                  <button className="btn btn-ghost" type="button" onClick={() => setConfirming(clinic)}>Deactivate</button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>

      {showForm && (
        <div className="admin-modal-backdrop" role="presentation">
          <div className="card admin-modal" role="dialog" aria-modal="true" aria-labelledby="clinic-form-title">
            <div><span className="badge badge-rose">Clinic record</span><h2 id="clinic-form-title">{editing ? 'Edit clinic' : 'Add clinic'}</h2></div>
            <form onSubmit={saveClinic} className="admin-modal-form">
              <label className="form-group"><span className="form-label">Clinic name</span><input className="form-input" required maxLength={200} value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} /></label>
              <div className="admin-form-grid">
                <label className="form-group"><span className="form-label">City</span><input className="form-input" maxLength={100} value={form.city} onChange={(event) => setForm({ ...form, city: event.target.value })} /></label>
                <label className="form-group"><span className="form-label">Phone</span><input className="form-input" maxLength={20} value={form.phoneNumber} onChange={(event) => setForm({ ...form, phoneNumber: event.target.value })} /></label>
              </div>
              <label className="form-group"><span className="form-label">Address</span><textarea className="form-input" rows={3} maxLength={500} value={form.address} onChange={(event) => setForm({ ...form, address: event.target.value })} /></label>
              <div className="admin-modal-actions">
                <button className="btn btn-ghost" type="button" onClick={() => setShowForm(false)} disabled={submitting}>Cancel</button>
                <button className="btn btn-primary" type="submit" disabled={submitting || !form.name.trim()}><CheckIcon size={16} /> {submitting ? 'Saving…' : 'Save clinic'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {confirming && (
        <div className="admin-modal-backdrop" role="presentation">
          <div className="card admin-modal admin-confirm-modal" role="alertdialog" aria-modal="true" aria-labelledby="clinic-confirm-title">
            <span className="admin-icon-tile admin-icon-tile--danger"><BuildingIcon size={22} /></span>
            <h2 id="clinic-confirm-title">Deactivate {confirming.name}?</h2>
            <p>The clinic disappears from active directories. Existing doctor links and historical records are retained.</p>
            <div className="admin-modal-actions">
              <button className="btn btn-ghost" type="button" onClick={() => setConfirming(null)} disabled={submitting}>Keep clinic</button>
              <button className="btn btn-danger" type="button" onClick={deactivateClinic} disabled={submitting}>{submitting ? 'Deactivating…' : 'Deactivate clinic'}</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
