import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { sellOrdersApi, tradersApi, assetTypesApi } from '../api';
import type { SellOrderResponse, PaginatedResponse, TraderResponse, AssetTypeResponse } from '../types';
import Modal from '../components/Modal';
import Paginator from '../components/Paginator';
import { formatDate, fmt } from '../utils';

const STATUS_OPTIONS = ['', 'Open', 'PartiallyFilled', 'Filled', 'Cancelled'];

function statusBadge(s: string) {
  const map: Record<string, string> = {
    Open: 'badge-blue', PartiallyFilled: 'badge-yellow', Filled: 'badge-green', Cancelled: 'badge-gray',
  };
  return <span className={`badge ${map[s] ?? 'badge-gray'}`}>{s}</span>;
}

export default function SellOrdersPage() {
  const [data, setData] = useState<PaginatedResponse<SellOrderResponse> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [page, setPage] = useState(1);

  // Filters
  const [filterAsset, setFilterAsset] = useState('');
  const [filterTrader, setFilterTrader] = useState('');
  const [filterStatus, setFilterStatus] = useState('');

  // Dropdown options
  const [traders, setTraders] = useState<TraderResponse[]>([]);
  const [assetTypes, setAssetTypes] = useState<AssetTypeResponse[]>([]);

  // Create modal
  const [showCreate, setShowCreate] = useState(false);
  const [createForm, setCreateForm] = useState({ traderId: '', assetTypeId: '', quantity: '', unitPrice: '' });
  const [submitting, setSubmitting] = useState(false);

  // Edit modal
  const [editOrder, setEditOrder] = useState<SellOrderResponse | null>(null);
  const [editForm, setEditForm] = useState({ unitPrice: '', quantity: '' });

  async function load() {
    setLoading(true);
    setError('');
    try {
      setData(await sellOrdersApi.list(filterAsset || undefined, filterTrader || undefined, filterStatus || undefined, page));
    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    async function init() {
      try {
        const [t, a] = await Promise.all([tradersApi.list(1, 200), assetTypesApi.list()]);
        setTraders(t.items);
        setAssetTypes(a);
        if (t.items.length > 0) setCreateForm(f => ({ ...f, traderId: t.items[0].id }));
        if (a.length > 0) setCreateForm(f => ({ ...f, assetTypeId: a[0].id }));
      } catch (e: any) {
        setError(e.message);
      }
    }
    init();
  }, []);

  useEffect(() => { load(); }, [page, filterAsset, filterTrader, filterStatus]); // eslint-disable-line react-hooks/exhaustive-deps

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    try {
      await sellOrdersApi.create(
        createForm.traderId,
        createForm.assetTypeId,
        parseFloat(createForm.quantity),
        parseFloat(createForm.unitPrice),
      );
      setShowCreate(false);
      setCreateForm(f => ({ ...f, quantity: '', unitPrice: '' }));
      setPage(1);
      load();
    } catch (e: any) {
      setError(e.message);
    } finally {
      setSubmitting(false);
    }
  }

  async function handleEdit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    try {
      await sellOrdersApi.update(
        editOrder!.id,
        editForm.unitPrice ? parseFloat(editForm.unitPrice) : undefined,
        editForm.quantity ? parseFloat(editForm.quantity) : undefined,
      );
      setEditOrder(null);
      load();
    } catch (e: any) {
      setError(e.message);
    } finally {
      setSubmitting(false);
    }
  }

  async function handleCancel(id: string) {
    if (!confirm('Cancel this sell order?')) return;
    try {
      await sellOrdersApi.cancel(id);
      load();
    } catch (e: any) {
      setError(e.message);
    }
  }

  function applyFilters() { setPage(1); load(); }

  return (
    <div className="page">
      <div className="page-header">
        <h1 className="page-title">Sell Orders</h1>
        <button className="btn btn-primary" onClick={() => setShowCreate(true)}>+ New Order</button>
      </div>

      {error && <div className="error-banner">{error}</div>}

      {/* Filters */}
      <div className="filter-bar">
        <div className="field">
          <label>Asset Type</label>
          <select value={filterAsset} onChange={e => { setFilterAsset(e.target.value); setPage(1); }}>
            <option value="">All assets</option>
            {assetTypes.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
          </select>
        </div>
        <div className="field">
          <label>Trader</label>
          <select value={filterTrader} onChange={e => { setFilterTrader(e.target.value); setPage(1); }}>
            <option value="">All traders</option>
            {traders.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
          </select>
        </div>
        <div className="field">
          <label>Status</label>
          <select value={filterStatus} onChange={e => { setFilterStatus(e.target.value); setPage(1); }}>
            {STATUS_OPTIONS.map(s => <option key={s} value={s}>{s || 'All statuses'}</option>)}
          </select>
        </div>
        <button className="btn btn-secondary btn-sm" style={{ alignSelf: 'flex-end' }} onClick={applyFilters}>Refresh</button>
      </div>

      <div className="card">
        <div className="table-wrap">
          {loading
            ? <div className="loading">Loading…</div>
            : (
              <table>
                <thead>
                  <tr>
                    <th>Asset</th>
                    <th>Seller</th>
                    <th className="text-right">Orig Qty</th>
                    <th className="text-right">Remaining</th>
                    <th className="text-right">Price</th>
                    <th>Status</th>
                    <th>Created</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {data?.items.length === 0 && (
                    <tr><td colSpan={8}><div className="empty">No orders found</div></td></tr>
                  )}
                  {data?.items.map(o => (
                    <tr key={o.id}>
                      <td><strong>{o.assetName}</strong></td>
                      <td>
                        <Link className="td-link" to={`/traders/${o.traderId}`}>{o.traderName}</Link>
                      </td>
                      <td className="text-right">{fmt(o.originalQuantity)}</td>
                      <td className="text-right">{fmt(o.remainingQuantity)}</td>
                      <td className="text-right"><strong>{fmt(o.unitPrice)}</strong></td>
                      <td>{statusBadge(o.status)}</td>
                      <td className="text-muted">{formatDate(o.createdAt)}</td>
                      <td>
                        <div className="row">
                          {(o.status === 'Open' || o.status === 'PartiallyFilled') && (
                            <>
                              <button
                                className="btn btn-secondary btn-xs"
                                onClick={() => { setEditOrder(o); setEditForm({ unitPrice: String(o.unitPrice), quantity: String(o.remainingQuantity) }); }}
                              >Edit</button>
                              <button className="btn btn-danger btn-xs" onClick={() => handleCancel(o.id)}>Cancel</button>
                            </>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
        </div>
        {data && <Paginator page={page} pageSize={50} totalCount={data.totalCount} onPage={setPage} />}
      </div>

      {/* Create modal */}
      {showCreate && (
        <Modal title="New Sell Order" onClose={() => setShowCreate(false)}>
          <form onSubmit={handleCreate}>
            <div className="modal-body form-grid">
              <div className="field">
                <label>Seller (Trader)</label>
                <select value={createForm.traderId} onChange={e => setCreateForm(f => ({ ...f, traderId: e.target.value }))} required>
                  {traders.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                </select>
              </div>
              <div className="field">
                <label>Asset Type</label>
                <select value={createForm.assetTypeId} onChange={e => setCreateForm(f => ({ ...f, assetTypeId: e.target.value }))} required>
                  {assetTypes.filter(a => a.isActive).map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
                </select>
              </div>
              <div className="form-grid-2">
                <div className="field">
                  <label>Quantity</label>
                  <input
                    type="number"
                    step="any"
                    min="0.000001"
                    value={createForm.quantity}
                    onChange={e => setCreateForm(f => ({ ...f, quantity: e.target.value }))}
                    placeholder="100"
                    required
                  />
                </div>
                <div className="field">
                  <label>Unit Price</label>
                  <input
                    type="number"
                    step="any"
                    min="0.000001"
                    value={createForm.unitPrice}
                    onChange={e => setCreateForm(f => ({ ...f, unitPrice: e.target.value }))}
                    placeholder="25.00"
                    required
                  />
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={() => setShowCreate(false)}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={submitting}>
                {submitting ? 'Creating…' : 'Create Order'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Edit modal */}
      {editOrder && (
        <Modal title={`Edit Order — ${editOrder.assetName}`} onClose={() => setEditOrder(null)}>
          <form onSubmit={handleEdit}>
            <div className="modal-body form-grid">
              <div className="text-muted">
                Current: <strong>{fmt(editOrder.remainingQuantity)}</strong> units @ <strong>{fmt(editOrder.unitPrice)}</strong>
              </div>
              <div className="form-grid-2">
                <div className="field">
                  <label>New Unit Price</label>
                  <input
                    type="number"
                    step="any"
                    value={editForm.unitPrice}
                    onChange={e => setEditForm(f => ({ ...f, unitPrice: e.target.value }))}
                  />
                </div>
                <div className="field">
                  <label>New Quantity</label>
                  <input
                    type="number"
                    step="any"
                    value={editForm.quantity}
                    onChange={e => setEditForm(f => ({ ...f, quantity: e.target.value }))}
                  />
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={() => setEditOrder(null)}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={submitting}>
                {submitting ? 'Saving…' : 'Update'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
