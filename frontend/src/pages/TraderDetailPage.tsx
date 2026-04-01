import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { tradersApi, assetTypesApi, sellOrdersApi } from '../api';
import type {
  TraderResponse, TraderBalancesResponse,
  AssetTypeResponse, SellOrderResponse, TradeResponse, LedgerEntryResponse,
  PaginatedResponse,
} from '../types';
import Modal from '../components/Modal';
import Paginator from '../components/Paginator';
import { formatDate, fmt } from '../utils';

type Tab = 'overview' | 'orders' | 'trades' | 'ledger';

function orderStatusBadge(s: string) {
  const map: Record<string, string> = {
    Open: 'badge-blue', PartiallyFilled: 'badge-yellow', Filled: 'badge-green', Cancelled: 'badge-gray',
  };
  return <span className={`badge ${map[s] ?? 'badge-gray'}`}>{s}</span>;
}

function creditDeltaClass(n: number | null) {
  if (n == null) return '';
  return n >= 0 ? 'text-green' : 'text-red';
}

export default function TraderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [trader, setTrader] = useState<TraderResponse | null>(null);
  const [balances, setBalances] = useState<TraderBalancesResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [tab, setTab] = useState<Tab>('overview');

  // Tab data
  const [orders, setOrders] = useState<PaginatedResponse<SellOrderResponse> | null>(null);
  const [ordersPage, setOrdersPage] = useState(1);
  const [ordersLoading, setOrdersLoading] = useState(false);

  const [trades, setTrades] = useState<PaginatedResponse<TradeResponse> | null>(null);
  const [tradesPage, setTradesPage] = useState(1);
  const [tradesLoading, setTradesLoading] = useState(false);

  const [ledger, setLedger] = useState<PaginatedResponse<LedgerEntryResponse> | null>(null);
  const [ledgerPage, setLedgerPage] = useState(1);
  const [ledgerLoading, setLedgerLoading] = useState(false);

  // Modals
  const [showEditName, setShowEditName] = useState(false);
  const [showAdjustCredits, setShowAdjustCredits] = useState(false);
  const [showAdjustAsset, setShowAdjustAsset] = useState(false);

  // Form state
  const [editName, setEditName] = useState('');
  const [creditsAmount, setCreditsAmount] = useState('');
  const [creditsReason, setCreditsReason] = useState('');
  const [assetTypes, setAssetTypes] = useState<AssetTypeResponse[]>([]);
  const [assetTypeId, setAssetTypeId] = useState('');
  const [assetAmount, setAssetAmount] = useState('');
  const [assetReason, setAssetReason] = useState('');
  const [submitting, setSubmitting] = useState(false);

  async function loadTrader() {
    setLoading(true);
    setError('');
    try {
      const [t, b] = await Promise.all([
        tradersApi.get(id!),
        tradersApi.balances(id!),
      ]);
      setTrader(t);
      setBalances(b);
      setEditName(t.name);
    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { loadTrader(); }, [id]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (tab !== 'orders') return;
    setOrdersLoading(true);
    tradersApi.sellOrders(id!, ordersPage).then(setOrders).catch(e => setError(e.message)).finally(() => setOrdersLoading(false));
  }, [tab, ordersPage, id]);

  useEffect(() => {
    if (tab !== 'trades') return;
    setTradesLoading(true);
    tradersApi.trades(id!, tradesPage).then(setTrades).catch(e => setError(e.message)).finally(() => setTradesLoading(false));
  }, [tab, tradesPage, id]);

  useEffect(() => {
    if (tab !== 'ledger') return;
    setLedgerLoading(true);
    tradersApi.ledger(id!, ledgerPage).then(setLedger).catch(e => setError(e.message)).finally(() => setLedgerLoading(false));
  }, [tab, ledgerPage, id]);

  async function handleDelete() {
    if (!confirm(`Delete trader "${trader?.name}"?`)) return;
    try {
      await tradersApi.delete(id!);
      navigate('/traders');
    } catch (e: any) {
      setError(e.message);
    }
  }

  async function handleEditName(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    try {
      await tradersApi.update(id!, editName);
      setShowEditName(false);
      loadTrader();
    } catch (e: any) {
      setError(e.message);
    } finally {
      setSubmitting(false);
    }
  }

  async function handleAdjustCredits(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    try {
      await tradersApi.adjustCredits(id!, parseFloat(creditsAmount), creditsReason);
      setShowAdjustCredits(false);
      setCreditsAmount('');
      setCreditsReason('');
      loadTrader();
    } catch (e: any) {
      setError(e.message);
    } finally {
      setSubmitting(false);
    }
  }

  async function handleAdjustAsset(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    try {
      await tradersApi.adjustAsset(id!, assetTypeId, parseFloat(assetAmount), assetReason);
      setShowAdjustAsset(false);
      setAssetTypeId('');
      setAssetAmount('');
      setAssetReason('');
      loadTrader();
    } catch (e: any) {
      setError(e.message);
    } finally {
      setSubmitting(false);
    }
  }

  async function openAdjustAsset() {
    try {
      const types = await assetTypesApi.list();
      setAssetTypes(types.filter(t => t.isActive));
      if (types.length > 0) setAssetTypeId(types[0].id);
    } catch (e: any) {
      setError(e.message);
    }
    setShowAdjustAsset(true);
  }

  async function handleCancelOrder(orderId: string) {
    if (!confirm('Cancel this sell order?')) return;
    try {
      await sellOrdersApi.cancel(orderId);
      setOrdersPage(1);
      tradersApi.sellOrders(id!, 1).then(setOrders);
    } catch (e: any) {
      setError(e.message);
    }
  }

  if (loading) return <div className="page"><div className="loading">Loading…</div></div>;

  return (
    <div className="page">
      {/* Header */}
      <div className="page-header">
        <div>
          <div className="row mb-12" style={{ gap: 6 }}>
            <Link to="/traders" className="text-muted" style={{ fontSize: 12 }}>← Traders</Link>
          </div>
          <div className="row">
            <h1 className="page-title" style={{ flex: 'none' }}>{trader?.name}</h1>
            <span className={`badge ${trader?.status === 'Active' ? 'badge-green' : 'badge-gray'}`}>
              {trader?.status}
            </span>
          </div>
        </div>
        <div className="row ml-auto">
          <button className="btn btn-secondary btn-sm" onClick={() => setShowEditName(true)}>Rename</button>
          <button className="btn btn-danger btn-sm" onClick={handleDelete}>Delete</button>
        </div>
      </div>

      {error && <div className="error-banner">{error}</div>}

      {/* Tabs */}
      <div className="tabs">
        {(['overview', 'orders', 'trades', 'ledger'] as Tab[]).map(t => (
          <button key={t} className={`tab-btn${tab === t ? ' active' : ''}`} onClick={() => setTab(t)}>
            {t.charAt(0).toUpperCase() + t.slice(1)}
          </button>
        ))}
      </div>

      {/* Overview tab */}
      {tab === 'overview' && (
        <>
          {/* Credits */}
          <div className="card mb-16">
            <div className="card-header">
              Credits
              <div className="ml-auto">
                <button className="btn btn-success btn-sm" onClick={() => setShowAdjustCredits(true)}>Adjust Credits</button>
              </div>
            </div>
            <div className="card-body">
              <div className="credits-big">{fmt(balances?.credits ?? 0)}</div>
              <div className="text-muted mt-12">Available balance</div>
            </div>
          </div>

          {/* Asset balances */}
          <div className="card mb-16">
            <div className="card-header">
              Asset Balances
              <div className="ml-auto">
                <button className="btn btn-secondary btn-sm" onClick={openAdjustAsset}>Adjust Asset</button>
              </div>
            </div>
            <div className="table-wrap">
              {(balances?.assetBalances.length ?? 0) === 0
                ? <div className="empty">No asset holdings</div>
                : (
                  <table>
                    <thead>
                      <tr>
                        <th>Asset</th>
                        <th className="text-right">Total</th>
                        <th className="text-right">Reserved</th>
                        <th className="text-right">Available</th>
                      </tr>
                    </thead>
                    <tbody>
                      {balances?.assetBalances.map(a => (
                        <tr key={a.assetTypeId}>
                          <td>
                            <div style={{ fontWeight: 500 }}>{a.assetName}</div>
                            <div className="text-muted" style={{ fontSize: 12 }}>{a.assetCode}</div>
                          </td>
                          <td className="text-right">{fmt(a.totalQuantity)}</td>
                          <td className="text-right text-yellow">{fmt(a.reservedQuantity)}</td>
                          <td className="text-right text-green">{fmt(a.availableQuantity)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
            </div>
          </div>

          {/* Info */}
          <div className="card">
            <div className="card-header">Details</div>
            <div className="card-body">
              <div className="info-grid">
                <div className="info-item"><div className="info-label">Trader ID</div><div className="info-value text-mono">{trader?.id}</div></div>
                <div className="info-item"><div className="info-label">Created</div><div className="info-value">{formatDate(trader?.createdAt ?? '')}</div></div>
                <div className="info-item"><div className="info-label">Updated</div><div className="info-value">{formatDate(trader?.updatedAt ?? '')}</div></div>
              </div>
            </div>
          </div>
        </>
      )}

      {/* Orders tab */}
      {tab === 'orders' && (
        <div className="card">
          <div className="table-wrap">
            {ordersLoading
              ? <div className="loading">Loading…</div>
              : (
                <table>
                  <thead>
                    <tr>
                      <th>Asset</th>
                      <th className="text-right">Orig Qty</th>
                      <th className="text-right">Remaining</th>
                      <th className="text-right">Price</th>
                      <th>Status</th>
                      <th>Created</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {orders?.items.length === 0 && (
                      <tr><td colSpan={7}><div className="empty">No sell orders</div></td></tr>
                    )}
                    {orders?.items.map(o => (
                      <tr key={o.id}>
                        <td><span style={{ fontWeight: 500 }}>{o.assetName}</span></td>
                        <td className="text-right">{fmt(o.originalQuantity)}</td>
                        <td className="text-right">{fmt(o.remainingQuantity)}</td>
                        <td className="text-right">{fmt(o.unitPrice)}</td>
                        <td>{orderStatusBadge(o.status)}</td>
                        <td className="text-muted">{formatDate(o.createdAt)}</td>
                        <td>
                          {o.status === 'Open' || o.status === 'PartiallyFilled'
                            ? <button className="btn btn-danger btn-xs" onClick={() => handleCancelOrder(o.id)}>Cancel</button>
                            : null}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
          </div>
          {orders && <Paginator page={ordersPage} pageSize={50} totalCount={orders.totalCount} onPage={setOrdersPage} />}
        </div>
      )}

      {/* Trades tab */}
      {tab === 'trades' && (
        <div className="card">
          <div className="table-wrap">
            {tradesLoading
              ? <div className="loading">Loading…</div>
              : (
                <table>
                  <thead>
                    <tr>
                      <th>Asset</th>
                      <th className="text-right">Quantity</th>
                      <th className="text-right">Avg Price</th>
                      <th className="text-right">Total Cost</th>
                      <th>Executed</th>
                    </tr>
                  </thead>
                  <tbody>
                    {trades?.items.length === 0 && (
                      <tr><td colSpan={5}><div className="empty">No trades</div></td></tr>
                    )}
                    {trades?.items.map(t => (
                      <tr key={t.id}>
                        <td><span style={{ fontWeight: 500 }}>{t.assetCode}</span></td>
                        <td className="text-right">{fmt(t.totalQuantity)}</td>
                        <td className="text-right">{fmt(t.averageUnitPrice)}</td>
                        <td className="text-right text-green">{fmt(t.totalCost)}</td>
                        <td className="text-muted">{formatDate(t.executedAt)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
          </div>
          {trades && <Paginator page={tradesPage} pageSize={50} totalCount={trades.totalCount} onPage={setTradesPage} />}
        </div>
      )}

      {/* Ledger tab */}
      {tab === 'ledger' && (
        <div className="card">
          <div className="table-wrap">
            {ledgerLoading
              ? <div className="loading">Loading…</div>
              : (
                <table>
                  <thead>
                    <tr>
                      <th>Type</th>
                      <th>Asset</th>
                      <th className="text-right">Qty Δ</th>
                      <th className="text-right">Credits Δ</th>
                      <th>Date</th>
                    </tr>
                  </thead>
                  <tbody>
                    {ledger?.items.length === 0 && (
                      <tr><td colSpan={5}><div className="empty">No ledger entries</div></td></tr>
                    )}
                    {ledger?.items.map(e => (
                      <tr key={e.id}>
                        <td><span className="badge badge-blue">{e.entryTypeName}</span></td>
                        <td>{e.assetCode ?? <span className="text-muted">—</span>}</td>
                        <td className={`text-right ${e.quantityDelta != null ? (e.quantityDelta >= 0 ? 'text-green' : 'text-red') : ''}`}>
                          {e.quantityDelta != null ? (e.quantityDelta >= 0 ? '+' : '') + fmt(e.quantityDelta) : '—'}
                        </td>
                        <td className={`text-right ${creditDeltaClass(e.creditDelta)}`}>
                          {e.creditDelta != null ? (e.creditDelta >= 0 ? '+' : '') + fmt(e.creditDelta) : '—'}
                        </td>
                        <td className="text-muted">{formatDate(e.createdAt)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
          </div>
          {ledger && <Paginator page={ledgerPage} pageSize={50} totalCount={ledger.totalCount} onPage={setLedgerPage} />}
        </div>
      )}

      {/* Edit Name modal */}
      {showEditName && (
        <Modal title="Rename Trader" onClose={() => setShowEditName(false)}>
          <form onSubmit={handleEditName}>
            <div className="modal-body">
              <div className="field">
                <label>Name</label>
                <input autoFocus value={editName} onChange={e => setEditName(e.target.value)} required />
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={() => setShowEditName(false)}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={submitting}>
                {submitting ? 'Saving…' : 'Save'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Adjust Credits modal */}
      {showAdjustCredits && (
        <Modal title="Adjust Credits" onClose={() => setShowAdjustCredits(false)}>
          <form onSubmit={handleAdjustCredits}>
            <div className="modal-body form-grid">
              <div className="field">
                <label>Amount (positive to add, negative to remove)</label>
                <input
                  autoFocus
                  type="number"
                  step="any"
                  value={creditsAmount}
                  onChange={e => setCreditsAmount(e.target.value)}
                  placeholder="e.g. 1000 or -500"
                  required
                />
              </div>
              <div className="field">
                <label>Reason (optional)</label>
                <input
                  value={creditsReason}
                  onChange={e => setCreditsReason(e.target.value)}
                  placeholder="e.g. Initial funding"
                />
              </div>
              <div className="text-muted">
                Current balance: <strong className="text-green">{fmt(balances?.credits ?? 0)}</strong>
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={() => setShowAdjustCredits(false)}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={submitting}>
                {submitting ? 'Saving…' : 'Apply'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Adjust Asset modal */}
      {showAdjustAsset && (
        <Modal title="Adjust Asset Balance" onClose={() => setShowAdjustAsset(false)}>
          <form onSubmit={handleAdjustAsset}>
            <div className="modal-body form-grid">
              <div className="field">
                <label>Asset Type</label>
                <select value={assetTypeId} onChange={e => setAssetTypeId(e.target.value)} required>
                  {assetTypes.map(a => (
                    <option key={a.id} value={a.id}>{a.name}</option>
                  ))}
                </select>
              </div>
              <div className="field">
                <label>Amount (positive to add, negative to remove)</label>
                <input
                  autoFocus
                  type="number"
                  step="any"
                  value={assetAmount}
                  onChange={e => setAssetAmount(e.target.value)}
                  placeholder="e.g. 100 or -50"
                  required
                />
              </div>
              <div className="field">
                <label>Reason (optional)</label>
                <input
                  value={assetReason}
                  onChange={e => setAssetReason(e.target.value)}
                  placeholder="e.g. Initial inventory"
                />
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={() => setShowAdjustAsset(false)}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={submitting || !assetTypeId}>
                {submitting ? 'Saving…' : 'Apply'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
