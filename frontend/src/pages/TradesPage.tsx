import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { tradesApi, quotesApi, tradersApi, assetTypesApi } from '../api';
import type {
  TradeResponse, PaginatedResponse, TradeFillResponse,
  TraderResponse, AssetTypeResponse, QuoteResponse,
} from '../types';
import Modal from '../components/Modal';
import Paginator from '../components/Paginator';
import { formatDate, fmt, shortId } from '../utils';

export default function TradesPage() {
  const [data, setData] = useState<PaginatedResponse<TradeResponse> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [page, setPage] = useState(1);

  // Filters
  const [filterAsset, setFilterAsset] = useState('');
  const [filterBuyer, setFilterBuyer] = useState('');

  // Options
  const [traders, setTraders] = useState<TraderResponse[]>([]);
  const [assetTypes, setAssetTypes] = useState<AssetTypeResponse[]>([]);

  // Execute trade modal
  const [showExecute, setShowExecute] = useState(false);
  const [execForm, setExecForm] = useState({ buyerTraderId: '', assetTypeId: '', quantity: '', idempotencyKey: '' });
  const [quote, setQuote] = useState<QuoteResponse | null>(null);
  const [quoteLoading, setQuoteLoading] = useState(false);
  const [quoteError, setQuoteError] = useState('');
  const [executing, setExecuting] = useState(false);

  // Fills modal
  const [fillsTrade, setFillsTrade] = useState<TradeResponse | null>(null);

  async function load() {
    setLoading(true);
    setError('');
    try {
      setData(await tradesApi.list(filterAsset || undefined, filterBuyer || undefined, page));
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
        if (t.items.length > 0) setExecForm(f => ({ ...f, buyerTraderId: t.items[0].id }));
        if (a.length > 0) setExecForm(f => ({ ...f, assetTypeId: a[0].id }));
      } catch (e: any) {
        setError(e.message);
      }
    }
    init();
  }, []);

  useEffect(() => { load(); }, [page, filterAsset, filterBuyer]); // eslint-disable-line react-hooks/exhaustive-deps

  async function getQuote() {
    if (!execForm.assetTypeId || !execForm.quantity) return;
    setQuoteLoading(true);
    setQuoteError('');
    setQuote(null);
    try {
      setQuote(await quotesApi.get(
        execForm.assetTypeId,
        parseFloat(execForm.quantity),
        execForm.buyerTraderId || undefined,
      ));
    } catch (e: any) {
      setQuoteError(e.message);
    } finally {
      setQuoteLoading(false);
    }
  }

  async function handleExecute(e: React.FormEvent) {
    e.preventDefault();
    setExecuting(true);
    setError('');
    try {
      await tradesApi.execute(
        execForm.buyerTraderId,
        execForm.assetTypeId,
        parseFloat(execForm.quantity),
        execForm.idempotencyKey || undefined,
      );
      setShowExecute(false);
      setQuote(null);
      setExecForm(f => ({ ...f, quantity: '', idempotencyKey: '' }));
      setPage(1);
      load();
    } catch (e: any) {
      setError(e.message);
    } finally {
      setExecuting(false);
    }
  }

  function openExecute() {
    setQuote(null);
    setQuoteError('');
    setShowExecute(true);
  }

  return (
    <div className="page">
      <div className="page-header">
        <h1 className="page-title">Trades</h1>
        <button className="btn btn-primary" onClick={openExecute}>⚡ Execute Trade</button>
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
          <label>Buyer</label>
          <select value={filterBuyer} onChange={e => { setFilterBuyer(e.target.value); setPage(1); }}>
            <option value="">All buyers</option>
            {traders.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
          </select>
        </div>
        <button className="btn btn-secondary btn-sm" style={{ alignSelf: 'flex-end' }} onClick={() => { setPage(1); load(); }}>Refresh</button>
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
                    <th>Buyer</th>
                    <th className="text-right">Quantity</th>
                    <th className="text-right">Avg Price</th>
                    <th className="text-right">Total Cost</th>
                    <th>Fills</th>
                    <th>Executed</th>
                  </tr>
                </thead>
                <tbody>
                  {data?.items.length === 0 && (
                    <tr><td colSpan={7}><div className="empty">No trades found</div></td></tr>
                  )}
                  {data?.items.map(t => (
                    <tr key={t.id}>
                      <td><strong>{t.assetCode}</strong></td>
                      <td><Link className="td-link" to={`/traders/${t.buyerTraderId}`}>{shortId(t.buyerTraderId)}</Link></td>
                      <td className="text-right">{fmt(t.totalQuantity)}</td>
                      <td className="text-right">{fmt(t.averageUnitPrice)}</td>
                      <td className="text-right text-green"><strong>{fmt(t.totalCost)}</strong></td>
                      <td>
                        <button className="btn btn-secondary btn-xs" onClick={() => setFillsTrade(t)}>
                          {t.fills.length} fill{t.fills.length !== 1 ? 's' : ''}
                        </button>
                      </td>
                      <td className="text-muted">{formatDate(t.executedAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
        </div>
        {data && <Paginator page={page} pageSize={50} totalCount={data.totalCount} onPage={setPage} />}
      </div>

      {/* Execute Trade modal */}
      {showExecute && (
        <Modal title="Execute Trade" size="lg" onClose={() => setShowExecute(false)}>
          <form onSubmit={handleExecute}>
            <div className="modal-body form-grid">
              <div className="form-grid-2">
                <div className="field">
                  <label>Buyer Trader</label>
                  <select
                    value={execForm.buyerTraderId}
                    onChange={e => { setExecForm(f => ({ ...f, buyerTraderId: e.target.value })); setQuote(null); }}
                    required
                  >
                    {traders.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                  </select>
                </div>
                <div className="field">
                  <label>Asset Type</label>
                  <select
                    value={execForm.assetTypeId}
                    onChange={e => { setExecForm(f => ({ ...f, assetTypeId: e.target.value })); setQuote(null); }}
                    required
                  >
                    {assetTypes.filter(a => a.isActive).map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
                  </select>
                </div>
              </div>
              <div className="form-grid-2">
                <div className="field">
                  <label>Quantity</label>
                  <input
                    type="number"
                    step="any"
                    min="0.000001"
                    value={execForm.quantity}
                    onChange={e => { setExecForm(f => ({ ...f, quantity: e.target.value })); setQuote(null); }}
                    placeholder="100"
                    required
                  />
                </div>
                <div className="field">
                  <label>Idempotency Key (optional)</label>
                  <input
                    value={execForm.idempotencyKey}
                    onChange={e => setExecForm(f => ({ ...f, idempotencyKey: e.target.value }))}
                    placeholder="Unique key for safe retry"
                  />
                </div>
              </div>

              {/* Quote section */}
              <div>
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  onClick={getQuote}
                  disabled={quoteLoading || !execForm.quantity || !execForm.assetTypeId}
                >
                  {quoteLoading ? 'Fetching…' : '🔍 Get Quote'}
                </button>
              </div>

              {quoteError && <div className="error-banner">{quoteError}</div>}

              {quote && (
                <div className="quote-box">
                  <div className="quote-box-title">Quote Preview</div>
                  <div className="quote-row">
                    <span>Fillable</span>
                    <span className={quote.isFillable ? 'text-green' : 'text-red'}>
                      {quote.isFillable ? '✓ Yes' : '✗ No'}
                    </span>
                  </div>
                  <div className="quote-row">
                    <span>Available Qty</span>
                    <span>{fmt(quote.availableQuantity)}</span>
                  </div>
                  {quote.averageUnitPrice != null && (
                    <div className="quote-row">
                      <span>Avg Price</span>
                      <span>{fmt(quote.averageUnitPrice)}</span>
                    </div>
                  )}
                  {quote.minUnitPrice != null && quote.maxUnitPrice != null && (
                    <div className="quote-row">
                      <span>Price Range</span>
                      <span>{fmt(quote.minUnitPrice)} – {fmt(quote.maxUnitPrice)}</span>
                    </div>
                  )}
                  <div className="quote-row">
                    <span>Orders consumed</span>
                    <span>{quote.ordersConsumed}</span>
                  </div>
                  {quote.buyerHasSufficientCredits != null && (
                    <div className="quote-row">
                      <span>Buyer has credits</span>
                      <span className={quote.buyerHasSufficientCredits ? 'text-green' : 'text-red'}>
                        {quote.buyerHasSufficientCredits ? '✓ Yes' : '✗ No — insufficient credits'}
                      </span>
                    </div>
                  )}
                  {quote.totalCost != null && (
                    <div className="quote-row total">
                      <span>Total Cost</span>
                      <span className="text-green">{fmt(quote.totalCost)}</span>
                    </div>
                  )}

                  {quote.fillPreview.length > 0 && (
                    <div style={{ marginTop: 12 }}>
                      <div className="quote-box-title">Fill Preview</div>
                      <table style={{ marginTop: 4 }}>
                        <thead>
                          <tr>
                            <th>Seller</th>
                            <th className="text-right">Qty</th>
                            <th className="text-right">Price</th>
                            <th className="text-right">Subtotal</th>
                          </tr>
                        </thead>
                        <tbody>
                          {quote.fillPreview.map((f, i) => (
                            <tr key={i}>
                              <td><span className="td-mono">{shortId(f.sellerTraderId)}</span></td>
                              <td className="text-right">{fmt(f.quantity)}</td>
                              <td className="text-right">{fmt(f.unitPrice)}</td>
                              <td className="text-right">{fmt(f.subTotal)}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>
              )}
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={() => setShowExecute(false)}>Cancel</button>
              <button
                type="submit"
                className="btn btn-primary"
                disabled={executing || !quote?.isFillable}
              >
                {executing ? 'Executing…' : 'Execute Trade'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Fills modal */}
      {fillsTrade && (
        <Modal title={`Fills — ${fillsTrade.assetCode}`} size="lg" onClose={() => setFillsTrade(null)}>

          <div className="modal-body">
            <div className="mb-16 text-muted">
              Trade ID: <span className="text-mono">{fillsTrade.id}</span>
            </div>
            <div className="quote-box mb-16">
              <div className="quote-row"><span>Quantity</span><span>{fmt(fillsTrade.totalQuantity)}</span></div>
              <div className="quote-row"><span>Avg Price</span><span>{fmt(fillsTrade.averageUnitPrice)}</span></div>
              <div className="quote-row total"><span>Total Cost</span><span className="text-green">{fmt(fillsTrade.totalCost)}</span></div>
            </div>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Seller</th>
                    <th className="text-right">Qty</th>
                    <th className="text-right">Price</th>
                    <th className="text-right">Subtotal</th>
                    <th>Executed</th>
                  </tr>
                </thead>
                <tbody>
                  {fillsTrade.fills.map((f: TradeFillResponse) => (
                    <tr key={f.id}>
                      <td><Link className="td-link" to={`/traders/${f.sellerTraderId}`}>{shortId(f.sellerTraderId)}</Link></td>
                      <td className="text-right">{fmt(f.quantity)}</td>
                      <td className="text-right">{fmt(f.unitPrice)}</td>
                      <td className="text-right text-green">{fmt(f.subTotal)}</td>
                      <td className="text-muted">{formatDate(f.executedAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
          <div className="modal-footer">
            <button className="btn btn-secondary" onClick={() => setFillsTrade(null)}>Close</button>
          </div>
        </Modal>
      )}
    </div>
  );
}
