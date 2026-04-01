import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { tradersApi } from '../api';
import type { TraderResponse, PaginatedResponse } from '../types';
import Modal from '../components/Modal';
import Paginator from '../components/Paginator';
import { formatDate, shortId } from '../utils';

function statusBadge(status: string) {
  return <span className={`badge ${status === 'Active' ? 'badge-green' : 'badge-gray'}`}>{status}</span>;
}

export default function TradersPage() {
  const [data, setData] = useState<PaginatedResponse<TraderResponse> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [page, setPage] = useState(1);
  const [showCreate, setShowCreate] = useState(false);
  const [newName, setNewName] = useState('');
  const [submitting, setSubmitting] = useState(false);

  async function load() {
    setLoading(true);
    setError('');
    try {
      setData(await tradersApi.list(page));
    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { load(); }, [page]); // eslint-disable-line react-hooks/exhaustive-deps

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    try {
      await tradersApi.create(newName);
      setShowCreate(false);
      setNewName('');
      setPage(1);
      load();
    } catch (e: any) {
      setError(e.message);
    } finally {
      setSubmitting(false);
    }
  }

  async function handleDelete(id: string, name: string) {
    if (!confirm(`Delete trader "${name}"? This cannot be undone.`)) return;
    try {
      await tradersApi.delete(id);
      load();
    } catch (e: any) {
      setError(e.message);
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <h1 className="page-title">Traders</h1>
        <button className="btn btn-primary" onClick={() => setShowCreate(true)}>+ New Trader</button>
      </div>

      {error && <div className="error-banner">{error}</div>}

      <div className="card">
        <div className="table-wrap">
          {loading
            ? <div className="loading">Loading…</div>
            : (
              <table>
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Status</th>
                    <th>ID</th>
                    <th>Created</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {data?.items.length === 0 && (
                    <tr><td colSpan={5}><div className="empty">No traders</div></td></tr>
                  )}
                  {data?.items.map(t => (
                    <tr key={t.id}>
                      <td><Link className="td-link" to={`/traders/${t.id}`}>{t.name}</Link></td>
                      <td>{statusBadge(t.status)}</td>
                      <td><span className="td-mono">{shortId(t.id)}</span></td>
                      <td className="text-muted">{formatDate(t.createdAt)}</td>
                      <td>
                        <div className="row">
                          <Link className="btn btn-secondary btn-xs" to={`/traders/${t.id}`}>View</Link>
                          <button className="btn btn-danger btn-xs" onClick={() => handleDelete(t.id, t.name)}>Delete</button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
        </div>
        {data && (
          <Paginator page={page} pageSize={50} totalCount={data.totalCount} onPage={setPage} />
        )}
      </div>

      {showCreate && (
        <Modal title="New Trader" onClose={() => setShowCreate(false)}>
          <form onSubmit={handleCreate}>
            <div className="modal-body">
              <div className="field">
                <label>Name</label>
                <input
                  autoFocus
                  value={newName}
                  onChange={e => setNewName(e.target.value)}
                  placeholder="e.g. Alice"
                  required
                />
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={() => setShowCreate(false)}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={submitting}>
                {submitting ? 'Creating…' : 'Create Trader'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
