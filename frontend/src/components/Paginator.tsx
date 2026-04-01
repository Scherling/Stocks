interface Props {
  page: number;
  pageSize: number;
  totalCount: number;
  onPage: (page: number) => void;
}

export default function Paginator({ page, pageSize, totalCount, onPage }: Props) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const start = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalCount);

  return (
    <div className="paginator">
      <span>{totalCount > 0 ? `${start}–${end} of ${totalCount}` : 'No results'}</span>
      <div className="row ml-auto">
        <button
          className="btn btn-secondary btn-sm"
          disabled={page <= 1}
          onClick={() => onPage(page - 1)}
        >← Prev</button>
        <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>
          {page} / {totalPages}
        </span>
        <button
          className="btn btn-secondary btn-sm"
          disabled={page >= totalPages}
          onClick={() => onPage(page + 1)}
        >Next →</button>
      </div>
    </div>
  );
}
