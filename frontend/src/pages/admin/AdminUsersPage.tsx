import { useCallback, useEffect, useState } from 'react';
import api from '../../api/client';
import AdminPageHeader from '../../components/admin/AdminPageHeader';
import PaginationControls from '../../components/common/PaginationControls';
import { SearchIcon, ShieldIcon } from '../../components/common/Icons';
import { useAppSelector } from '../../hooks/useRedux';
import type { AdminUserDto, ApiResponse } from '../../types';

const PAGE_SIZE = 20;

export default function AdminUsersPage() {
  const currentUserId = useAppSelector((state) => state.auth.user?.userId);
  const [users, setUsers] = useState<AdminUserDto[]>([]);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [role, setRole] = useState('');
  const [skip, setSkip] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [pendingUser, setPendingUser] = useState<AdminUserDto | null>(null);
  const [actionLoading, setActionLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fetchUsers = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const params = new URLSearchParams({ skip: String(skip), take: String(PAGE_SIZE) });
      if (search) params.set('search', search);
      if (role) params.set('role', role);
      const response = await api.get<ApiResponse<AdminUserDto[]>>(`/admin/users?${params}`);
      setUsers(response.data.data ?? []);
      setTotalCount(response.data.pagination?.totalCount ?? response.data.data?.length ?? 0);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load user accounts.');
    } finally {
      setLoading(false);
    }
  }, [role, search, skip]);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  const submitSearch = (event: React.FormEvent) => {
    event.preventDefault();
    setSkip(0);
    setSearch(searchInput.trim());
  };

  const setDisabled = async () => {
    if (!pendingUser) return;
    setActionLoading(true);
    setError(null);
    setMessage(null);
    try {
      const disabled = !pendingUser.isDisabled;
      await api.put(`/admin/users/${pendingUser.id}/disabled`, { disabled });
      setMessage(`${pendingUser.firstName} ${pendingUser.lastName}'s account was ${disabled ? 'disabled' : 'enabled'}.`);
      setPendingUser(null);
      await fetchUsers();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to change account access.');
    } finally {
      setActionLoading(false);
    }
  };

  return (
    <div className="page-enter admin-page-stack">
      <AdminPageHeader
        eyebrow="Access control"
        title="User Accounts"
        description="Search every account, review assigned roles, and suspend access without deleting clinical history."
      />

      {message && <div className="admin-notice admin-notice--success">{message}</div>}
      {error && <div className="admin-notice admin-notice--error">{error}</div>}

      <section className="card admin-resource-card">
        <div className="admin-resource-toolbar">
          <div>
            <h2>Account directory</h2>
            <p>{totalCount} matching account{totalCount === 1 ? '' : 's'}</p>
          </div>
          <form className="admin-filter-form" onSubmit={submitSearch}>
            <label className="admin-search-field">
              <span className="sr-only">Search users</span>
              <SearchIcon size={16} />
              <input
                className="form-input"
                value={searchInput}
                onChange={(event) => setSearchInput(event.target.value)}
                placeholder="Name or email"
                maxLength={200}
              />
            </label>
            <select
              className="form-input"
              aria-label="Filter users by role"
              value={role}
              onChange={(event) => { setRole(event.target.value); setSkip(0); }}
            >
              <option value="">All roles</option>
              <option value="Admin">Admin</option>
              <option value="Doctor">Doctor</option>
              <option value="Patient">Patient</option>
            </select>
            <button className="btn btn-secondary" type="submit">Search</button>
          </form>
        </div>

        {loading ? (
          <div className="skeleton" style={{ height: 260 }} />
        ) : users.length === 0 ? (
          <div className="admin-empty-state"><ShieldIcon size={38} /><p>No user accounts match these filters.</p></div>
        ) : (
          <div className="admin-resource-list" role="list">
            {users.map((user) => {
              const isSelf = user.id === currentUserId;
              return (
                <article className="admin-resource-row admin-user-row" key={user.id} role="listitem">
                  <div className="admin-primary-cell">
                    <strong>{user.firstName} {user.lastName}</strong>
                    <span>{user.email}</span>
                  </div>
                  <div className="admin-badge-group" aria-label="Assigned roles">
                    {user.roles.map((assignedRole) => (
                      <span className="badge badge-stone" key={assignedRole}>{assignedRole}</span>
                    ))}
                  </div>
                  <div>
                    <span className={`badge ${user.isDisabled ? 'badge-rose' : user.isLockedOut ? 'badge-amber' : 'badge-teal'}`}>
                      {user.isDisabled ? 'Disabled' : user.isLockedOut ? 'Temporarily locked' : 'Active'}
                    </span>
                  </div>
                  <div className="admin-row-actions">
                    {pendingUser?.id === user.id ? (
                      <>
                        <button className="btn btn-ghost" type="button" onClick={() => setPendingUser(null)} disabled={actionLoading}>Cancel</button>
                        <button className={user.isDisabled ? 'btn btn-primary' : 'btn btn-danger'} type="button" onClick={setDisabled} disabled={actionLoading}>
                          {actionLoading ? 'Saving…' : `Confirm ${user.isDisabled ? 'enable' : 'disable'}`}
                        </button>
                      </>
                    ) : (
                      <button
                        className={user.isDisabled ? 'btn btn-secondary' : 'btn btn-ghost'}
                        type="button"
                        disabled={isSelf}
                        title={isSelf ? 'You cannot change your own access.' : undefined}
                        onClick={() => setPendingUser(user)}
                      >
                        {user.isDisabled ? 'Enable' : 'Disable'}
                      </button>
                    )}
                  </div>
                </article>
              );
            })}
          </div>
        )}

        <PaginationControls skip={skip} take={PAGE_SIZE} totalCount={totalCount} onPageChange={setSkip} />
      </section>
    </div>
  );
}
