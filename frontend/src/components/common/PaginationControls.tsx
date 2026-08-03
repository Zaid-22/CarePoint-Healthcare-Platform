interface PaginationControlsProps {
  skip: number;
  take: number;
  totalCount: number;
  onPageChange: (nextSkip: number) => void;
}

export default function PaginationControls({
  skip,
  take,
  totalCount,
  onPageChange,
}: PaginationControlsProps) {
  if (totalCount <= take && skip === 0) return null;

  const first = totalCount === 0 ? 0 : skip + 1;
  const last = Math.min(skip + take, totalCount);

  return (
    <nav
      aria-label="List pagination"
      style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        gap: 12,
        flexWrap: 'wrap',
        paddingTop: 16,
        borderTop: '1px solid var(--border-default)',
      }}
    >
      <span style={{ fontSize: '0.8125rem', color: 'var(--text-secondary)', fontWeight: 600 }}>
        Records {first}–{last} of {totalCount}
      </span>
      <div style={{ display: 'flex', gap: 8 }}>
        <button
          type="button"
          className="btn btn-secondary"
          disabled={skip === 0}
          onClick={() => onPageChange(Math.max(0, skip - take))}
        >
          Previous
        </button>
        <button
          type="button"
          className="btn btn-secondary"
          disabled={skip + take >= totalCount}
          onClick={() => onPageChange(skip + take)}
        >
          Next
        </button>
      </div>
    </nav>
  );
}
