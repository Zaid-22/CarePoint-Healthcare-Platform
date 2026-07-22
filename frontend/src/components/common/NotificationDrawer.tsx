import { useEffect, useState } from 'react';
import api from '../../api/client';
import type { NotificationDto, ApiResponse } from '../../types';
import { CheckIcon, BellIcon } from './Icons';

export default function NotificationDrawer() {
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);

  const fetchNotifications = async () => {
    try {
      setLoading(true);
      const res = await api.get<ApiResponse<NotificationDto[]>>('/notifications');
      setNotifications(res.data.data || []);
    } catch (e) {
      console.error('Failed to fetch notifications', e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchNotifications();
  }, []);

  const unreadCount = notifications.filter((n) => !n.isRead).length;

  const markAsRead = async (id: string) => {
    try {
      await api.put(`/notifications/${id}/read`);
      setNotifications((prev) =>
        prev.map((n) => (n.id === id ? { ...n, isRead: true } : n))
      );
    } catch (e) {
      console.error('Failed to mark notification as read', e);
    }
  };

  return (
    <div style={{ position: 'relative' }}>
      {/* Bell Button */}
      <button
        type="button"
        onClick={() => setOpen(!open)}
        className="btn btn-ghost"
        style={{
          position: 'relative',
          width: 40,
          height: 40,
          padding: 0,
          borderRadius: '50%',
          display: 'inline-flex',
          alignItems: 'center',
          justifyContent: 'center',
          background: open ? 'var(--bg-subtle)' : 'transparent',
          transition: 'all 150ms ease',
        }}
        title="Notifications"
      >
        <BellIcon size={20} color="var(--text-primary)" />
        {unreadCount > 0 && (
          <span style={{
            position: 'absolute',
            top: 2,
            right: 2,
            background: 'var(--color-rose-600)',
            color: 'white',
            borderRadius: 99,
            minWidth: 18,
            height: 18,
            padding: '0 4px',
            fontSize: '0.6875rem',
            fontWeight: 700,
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: '0 0 0 2px var(--bg-surface)',
          }}>
            {unreadCount}
          </span>
        )}
      </button>

      {/* Popover panel */}
      {open && (
        <>
          <div
            style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, zIndex: 900 }}
            onClick={() => setOpen(false)}
          />
          <div className="card page-enter" style={{
            position: 'absolute',
            top: 46,
            left: 0,
            width: 340,
            maxHeight: 480,
            overflowY: 'auto',
            zIndex: 999,
            padding: 20,
            boxShadow: '0 20px 45px -10px rgba(0, 0, 0, 0.25)',
            border: '1px solid var(--border-default)',
            background: 'var(--bg-surface)',
          }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16, borderBottom: '1px solid var(--border-default)', paddingBottom: 10 }}>
              <div style={{ fontWeight: 700, fontSize: '1rem', fontFamily: 'var(--font-display)' }}>
                Notifications ({unreadCount} unread)
              </div>
              <button
                type="button"
                onClick={() => setOpen(false)}
                style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: '1rem', color: 'var(--text-secondary)' }}
              >
                ✕
              </button>
            </div>

            {loading ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                <div className="skeleton" style={{ height: 60 }} />
                <div className="skeleton" style={{ height: 60 }} />
              </div>
            ) : notifications.length === 0 ? (
              <div style={{ padding: '24px 12px', textAlign: 'center', color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                No notifications right now.
              </div>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                {notifications.map((n) => (
                  <div
                    key={n.id}
                    style={{
                      padding: 12,
                      borderRadius: 'var(--radius-md)',
                      background: n.isRead ? 'transparent' : 'var(--accent-light)',
                      border: n.isRead ? '1px solid var(--border-default)' : '1px solid var(--accent)',
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'flex-start',
                      gap: 10,
                      transition: 'all 120ms ease'
                    }}
                  >
                    <div>
                      <div style={{ fontWeight: 600, fontSize: '0.875rem', color: 'var(--text-primary)' }}>
                        {n.title}
                      </div>
                      <div style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', marginTop: 2 }}>
                        {n.message}
                      </div>
                      <div style={{ fontSize: '0.71875rem', color: 'var(--text-secondary)', marginTop: 4 }}>
                        {new Date(n.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </div>
                    </div>

                    {!n.isRead && (
                      <button
                        type="button"
                        onClick={() => markAsRead(n.id)}
                        title="Mark as Read"
                        style={{
                          background: 'var(--accent)',
                          color: 'white',
                          border: 'none',
                          borderRadius: 99,
                          width: 24,
                          height: 24,
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          cursor: 'pointer',
                          flexShrink: 0
                        }}
                      >
                        <CheckIcon size={12} color="white" />
                      </button>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
}
