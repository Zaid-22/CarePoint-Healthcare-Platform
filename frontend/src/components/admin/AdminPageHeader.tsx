import type { ReactNode } from 'react';

interface AdminPageHeaderProps {
  eyebrow: string;
  title: string;
  description: string;
  action?: ReactNode;
}

export default function AdminPageHeader({
  eyebrow,
  title,
  description,
  action,
}: AdminPageHeaderProps) {
  return (
    <header className="card admin-page-header">
      <div>
        <span className="badge badge-rose">{eyebrow}</span>
        <h1>{title}</h1>
        <p>{description}</p>
      </div>
      {action && <div className="admin-page-header-action">{action}</div>}
    </header>
  );
}
