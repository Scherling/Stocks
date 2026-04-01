import { useState, useEffect } from 'react';
import { assetTypesApi } from '../api';
import type { AssetTypeResponse } from '../types';
import Modal from '../components/Modal';
import { formatDate } from '../utils';

export default function AssetTypesPage() {
  const [items, setItems] = useState<AssetTypeResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Create modal
  const [showCreate, setShowCreate] = useState(false);
  const [createForm, setCreateForm] = useState({ slug: '', name: '', unitName: '' });

  // Edit modal
  const [editItem, setEditItem] = useState<AssetTypeResponse | null>(null);
  const [editForm, setEditForm] = useState({ name: '', unitName: '' });

  const [submitting, setSubmitting] = useState(false);

  async function load() {
    setLoading(true);
    setError('');
    try {
      setItems(await assetTypesApi.list());
    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { load(); }, []);

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    try {
      await assetTypesApi.create(createForm.slug, createForm.name, createForm.unitName);
      setShowCreate(false);
      setCreateForm({ slug: '', name: '', unitName: '' });
      load();
    } catch (e: any) {
      setError(e.message);
    } finally {
      setSubmitting(false);
    }
  }

  function openEdit(item: AssetTypeResponse) {
    setEditItem(item);
    setEditForm({ name: item.name, unitName: item.unitName });
  }

  async function handleEdit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    try {
      await assetTypesApi.update(editItem!.id, editForm.name, editForm.unitName);
      setEditItem(null);
      load();
    } catch (e: any) {
      setError(e.message);
    } finally {
      setSubmitting(false);
    }
  }

  async function handleDeactivate(item: AssetTypeResponse) {
    if (!confirm(`Deactivate "${item.name}"?`)) return;
    try {
      await assetTypesApi.deactivate(item.id);
      load();
    } catch (e: any) {
      setError(e.message);
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <h1 className="page-title">Asset Types</h1>
        <button className="btn btn-primary" onClick={() => setShowCreate(true)}>+ New Asset Type</button>
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
                    <th>ID</th>
                    <th>Unit</th>
                    <th>Status</th>
                    <th>Created</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {items.length === 0 && (
                    <tr><td colSpan={6}><div className="empty">No asset types</div></td></tr>
                  )}
                  {items.map(a => (
                    <tr key={a.id}>
                      <td>{a.name}</td>
                      <td><span className="td-mono">{a.slug}</span></td>
                      <td className="text-muted">{a.unitName}</td>
                      <td>
                        <span className={`badge ${a.isActive ? 'badge-green' : 'badge-gray'}`}>
                          {a.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td className="text-muted">{formatDate(a.createdAt)}</td>
                      <td>
                        <div className="row">
                          <button className="btn btn-secondary btn-xs" onClick={() => openEdit(a)}>Edit</button>
                          {a.isActive && (
                            <button className="btn btn-danger btn-xs" onClick={() => handleDeactivate(a)}>Deactivate</button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
        </div>
      </div>

      {/* Create modal */}
      {showCreate && (
        <Modal title="New Asset Type" onClose={() => setShowCreate(false)}>
          <form onSubmit={handleCreate}>
            <div className="modal-body form-grid">
              <div className="field">
                <label>ID (unique slug, e.g. steel-ingots)</label>
                <input
                  autoFocus
                  value={createForm.slug}
                  onChange={e => setCreateForm(f => ({ ...f, slug: e.target.value.toLowerCase() }))}
                  placeholder="steel-ingots"
                  required
                />
              </div>
              <div className="field">
                <label>Name</label>
                <input
                  value={createForm.name}
                  onChange={e => setCreateForm(f => ({ ...f, name: e.target.value }))}
                  placeholder="Steel Ingots"
                  required
                />
              </div>
              <div className="field">
                <label>Unit Name</label>
                <input
                  value={createForm.unitName}
                  onChange={e => setCreateForm(f => ({ ...f, unitName: e.target.value }))}
                  placeholder="ton"
                  required
                />
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={() => setShowCreate(false)}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={submitting}>
                {submitting ? 'Creating…' : 'Create'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Edit modal */}
      {editItem && (
        <Modal title={`Edit — ${editItem.name}`} onClose={() => setEditItem(null)}>
          <form onSubmit={handleEdit}>
            <div className="modal-body form-grid">
              <div className="field">
                <label>Name</label>
                <input
                  autoFocus
                  value={editForm.name}
                  onChange={e => setEditForm(f => ({ ...f, name: e.target.value }))}
                  required
                />
              </div>
              <div className="field">
                <label>Unit Name</label>
                <input
                  value={editForm.unitName}
                  onChange={e => setEditForm(f => ({ ...f, unitName: e.target.value }))}
                  required
                />
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={() => setEditItem(null)}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={submitting}>
                {submitting ? 'Saving…' : 'Save'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
