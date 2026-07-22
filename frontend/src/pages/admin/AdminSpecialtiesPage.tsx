import { useEffect, useState } from 'react';
import api from '../../api/client';
import type { SpecialtyDto, ApiResponse } from '../../types';
import { PillIcon, PlusIcon, CheckIcon, SearchIcon } from '../../components/common/Icons';

export default function AdminSpecialtiesPage() {
  const [specialties, setSpecialties] = useState<SpecialtyDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  
  // Modal states
  const [showModal, setShowModal] = useState(false);
  const [editingSpecialty, setEditingSpecialty] = useState<SpecialtyDto | null>(null);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Delete confirm state
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const fetchSpecialties = async (showSkeleton = false) => {
    try {
      if (showSkeleton) setLoading(true);
      const res = await api.get<ApiResponse<SpecialtyDto[]>>('/specialties');
      setSpecialties(res.data.data || []);
    } catch (err) {
      console.error('Failed to load specialties', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSpecialties(true);
  }, []);

  const handleOpenAddModal = () => {
    setEditingSpecialty(null);
    setName('');
    setDescription('');
    setError(null);
    setShowModal(true);
  };

  const handleOpenEditModal = (spec: SpecialtyDto) => {
    setEditingSpecialty(spec);
    setName(spec.name);
    setDescription(spec.description || '');
    setError(null);
    setShowModal(true);
  };

  const handleSubmitForm = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;

    try {
      setSubmitting(true);
      setError(null);
      
      if (editingSpecialty) {
        await api.put(`/specialties/${editingSpecialty.id}`, {
          name: name.trim(),
          description: description.trim(),
        });
      } else {
        await api.post('/specialties', {
          name: name.trim(),
          description: description.trim(),
        });
      }

      setName('');
      setDescription('');
      setShowModal(false);
      fetchSpecialties();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to save specialty.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      setSubmitting(true);
      await api.delete(`/specialties/${id}`);
      setDeletingId(null);
      fetchSpecialties();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to deactivate specialty.');
    } finally {
      setSubmitting(false);
    }
  };

  const filteredSpecialties = specialties.filter((s) =>
    s.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    (s.description && s.description.toLowerCase().includes(searchTerm.toLowerCase()))
  );

  return (
    <div className="page-enter" style={{ display: 'flex', flexDirection: 'column', gap: 32 }}>
      {/* Header */}
      <div className="card" style={{ padding: 32, display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: 'linear-gradient(135deg, #ffffff 0%, var(--color-teal-50) 100%)' }}>
        <div>
          <span className="badge badge-teal" style={{ marginBottom: 8 }}>System Management</span>
          <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '2.25rem', fontWeight: 700 }}>
            Clinical Specialties
          </h1>
          <p style={{ color: 'var(--text-secondary)', marginTop: 4 }}>
            Configure and manage clinical categories available for practitioner onboarding and patient filters.
          </p>
        </div>

        <button
          className="btn btn-primary"
          onClick={handleOpenAddModal}
          style={{ padding: '12px 20px', gap: 8 }}
        >
          <PlusIcon size={18} /> Add Specialty
        </button>
      </div>

      {/* Filter and Content Card */}
      <div className="card" style={{ padding: 28 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24, gap: 16, flexWrap: 'wrap' }}>
          <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700 }}>
            Configured Specialties ({filteredSpecialties.length})
          </h2>

          <div style={{ position: 'relative', width: 280 }}>
            <input
              className="form-input"
              type="text"
              placeholder="Search specialties..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              style={{ paddingLeft: 36 }}
            />
            <div style={{ position: 'absolute', left: 12, top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }}>
              <SearchIcon size={16} />
            </div>
          </div>
        </div>

        {loading ? (
          <div className="skeleton" style={{ height: 200 }} />
        ) : filteredSpecialties.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '48px 0', color: 'var(--text-secondary)' }}>
            <PillIcon size={48} style={{ opacity: 0.3, marginBottom: 12 }} />
            <p style={{ fontSize: '1rem', fontWeight: 500 }}>No matching specialties found.</p>
            <p style={{ fontSize: '0.875rem', marginTop: 4 }}>Try adjusting your search query or click "Add Specialty" to create one.</p>
          </div>
        ) : (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: 20 }}>
            {filteredSpecialties.map((spec) => (
              <div
                key={spec.id}
                style={{
                  padding: 20,
                  borderRadius: 'var(--radius-lg)',
                  border: '1px solid var(--border-default)',
                  background: 'var(--bg-surface)',
                  display: 'flex',
                  flexDirection: 'column',
                  gap: 12,
                  transition: 'transform 150ms ease, box-shadow 150ms ease',
                  position: 'relative'
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                    <div style={{
                      width: 38, height: 38, borderRadius: 10,
                      background: 'var(--color-teal-100)', color: 'var(--color-teal-700)',
                      display: 'flex', alignItems: 'center', justifyContent: 'center'
                    }}>
                      <PillIcon size={20} />
                    </div>
                    <span className={`badge ${spec.isActive !== false ? 'badge-teal' : 'badge-rose'}`}>
                      {spec.isActive !== false ? 'Active' : 'Inactive'}
                    </span>
                  </div>

                  {typeof spec.doctorCount === 'number' && (
                    <span style={{ fontSize: '0.78125rem', fontWeight: 600, color: 'var(--color-teal-800)', background: 'var(--color-teal-50)', padding: '2px 8px', borderRadius: 12 }}>
                      {spec.doctorCount} {spec.doctorCount === 1 ? 'Doctor' : 'Doctors'}
                    </span>
                  )}
                </div>

                <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.125rem', fontWeight: 700 }}>
                  {spec.name}
                </h3>
                <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', lineHeight: 1.5, flex: 1 }}>
                  {spec.description || 'No description provided.'}
                </p>

                <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end', paddingTop: 12, borderTop: '1px solid var(--border-default)' }}>
                  <button
                    className="btn btn-secondary"
                    onClick={() => handleOpenEditModal(spec)}
                    style={{ fontSize: '0.8125rem', padding: '6px 12px' }}
                  >
                    Edit
                  </button>
                  <button
                    className="btn btn-ghost"
                    onClick={() => setDeletingId(spec.id)}
                    style={{ fontSize: '0.8125rem', padding: '6px 12px', color: 'var(--color-rose-600)' }}
                  >
                    Deactivate
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Modal for adding/editing specialty */}
      {showModal && (
        <div style={{
          position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
          background: 'rgba(0,0,0,0.4)', backdropFilter: 'blur(4px)',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          zIndex: 1000, padding: 20
        }}>
          <div className="card page-enter" style={{ width: '100%', maxWidth: 460, padding: 32 }}>
            <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.5rem', fontWeight: 700, marginBottom: 8 }}>
              {editingSpecialty ? 'Edit Clinical Specialty' : 'Add New Specialty'}
            </h2>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginBottom: 20 }}>
              {editingSpecialty
                ? 'Update specialty name and description for doctors and patients.'
                : 'Create a new medical category available for doctor profiles and filter tags.'}
            </p>

            {error && (
              <div style={{
                padding: '10px 14px', background: 'var(--color-rose-100)', color: 'var(--color-rose-600)',
                borderRadius: 'var(--radius-md)', fontSize: '0.85rem', marginBottom: 16
              }}>
                {error}
              </div>
            )}

            <form onSubmit={handleSubmitForm} style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
              <div className="form-group">
                <label className="form-label">Specialty Name</label>
                <input
                  className="form-input"
                  placeholder="e.g. Cardiology"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  required
                />
              </div>

              <div className="form-group">
                <label className="form-label">Description</label>
                <textarea
                  className="form-input"
                  rows={3}
                  placeholder="Brief description of clinical domain and conditions treated..."
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                />
              </div>

              <div style={{ display: 'flex', gap: 12, justifyContent: 'flex-end', marginTop: 8 }}>
                <button
                  type="button"
                  className="btn btn-ghost"
                  onClick={() => setShowModal(false)}
                  disabled={submitting}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={submitting || !name.trim()}
                  style={{ gap: 6 }}
                >
                  <CheckIcon size={16} /> {submitting ? 'Saving...' : 'Save Specialty'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Delete/Deactivate Confirmation Modal */}
      {deletingId && (
        <div style={{
          position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
          background: 'rgba(0,0,0,0.4)', backdropFilter: 'blur(4px)',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          zIndex: 1000, padding: 20
        }}>
          <div className="card page-enter" style={{ width: '100%', maxWidth: 400, padding: 28, textAlign: 'center' }}>
            <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: 700, marginBottom: 8 }}>
              Deactivate Specialty?
            </h3>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginBottom: 24 }}>
              Deactivating this specialty will remove it from future doctor selections and patient search filters. Existing practitioner links are preserved.
            </p>

            <div style={{ display: 'flex', gap: 12, justifyContent: 'center' }}>
              <button
                className="btn btn-ghost"
                onClick={() => setDeletingId(null)}
                disabled={submitting}
              >
                Cancel
              </button>
              <button
                className="btn"
                style={{ background: 'var(--color-rose-600)', color: 'white' }}
                onClick={() => handleDelete(deletingId)}
                disabled={submitting}
              >
                {submitting ? 'Deactivating...' : 'Confirm Deactivation'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
