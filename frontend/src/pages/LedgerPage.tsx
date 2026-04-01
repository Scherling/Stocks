import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { ledgerApi, tradersApi, assetTypesApi } from '../api';
import type {
  AssetTransferResponse, CreditTransferResponse, PaginatedResponse,
  TraderResponse, AssetTypeResponse,
} from '../types';
import Paginator from '../components/Paginator';
import { formatDate, fmt, shortId } from '../utils';

type Tab = 'assets' | 'credits';

export default function LedgerPage() {
  const [tab, setTab] = useState<Tab>('assets');
  const [error, setError] = useState('');

  // Options
  const [traders, setTraders] = useState<TraderResponse[]>([]);
  const [assetTypes, setAssetTypes] = useState<AssetTypeResponse[]>([]);

  // Asset transfers state
  const [assetData, setAssetData] = useState<PaginatedResponse<AssetTransferResponse> | null>(null);
  const [assetLoading, setAssetLoading] = useState(false);
  const [assetPage, setAssetPage] = useState(1);
  const [filterAssetTrader, setFilterAssetTrader] = useState('');
  const [filterAssetType, setFilterAssetType] = useState('');

  // Credit transfers state
  const [creditData, setCreditData] = useState<PaginatedResponse<CreditTransferResponse> | null>(null);
  const [creditLoading, setCreditLoading] = useState(false);
  const [creditPage, setCreditPage] = useState(1);
  const [filterCreditTrader, setFilterCreditTrader] = useState('');

  useEffect(() => {
    async function init() {
      try {
        const [t, a] = await Promise.all([tradersApi.list(1, 200), assetTypesApi.list()]);
        setTraders(t.items);
        setAssetTypes(a);
      } catch (e: any) {
        setError(e.message);
      }
    }
    init();
  }, []);

  useEffect(() => {
    if (tab !== 'assets') return;
    setAssetLoading(true);
    ledgerApi
      .assetTransfers(filterAssetTrader || undefined, filterAssetType || undefined, assetPage)
      .then(setAssetData)
      .catch(e => setError(e.message))
      .finally(() => setAssetLoading(false));
  }, [tab, assetPage, filterAssetTrader, filterAssetType]);

  useEffect(() => {
    if (tab !== 'credits') return;
    setCreditLoading(true);
    ledgerApi
      .creditTransfers(filterCreditTrader || undefined, creditPage)
      .then(setCreditData)
      .catch(e => setError(e.message))
      .finally(() => setCreditLoading(false));
  }, [tab, creditPage, filterCreditTrader]);

  function traderLink(id: string | null, name: string | null) {
    if (!id) return <span className="text-muted">—</span>;
    return <Link className="td-link" to={`/traders/${id}`}>{name ?? shortId(id)}</Link>;
  }

  return (
    <div className="page">
      <div className="page-header">
        <h1 className="page-title">Ledger & Transfers</h1>
      </div>

      {error && <div className="error-banner">{error}</div>}

      <div className="tabs">
        <button className={`tab-btn${tab === 'assets' ? ' active' : ''}`} onClick={() => setTab('assets')}>
          Asset Transfers
        </button>
        <button className={`tab-btn${tab === 'credits' ? ' active' : ''}`} onClick={() => setTab('credits')}>
          Credit Transfers
        </button>
      </div>

      {/* Asset Transfers */}
      {tab === 'assets' && (
        <>
          <div className="filter-bar">
            <div className="field">
              <label>Trader</label>
              <select value={filterAssetTrader} onChange={e => { setFilterAssetTrader(e.target.value); setAssetPage(1); }}>
                <option value="">All traders</option>
                {traders.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
              </select>
            </div>
            <div className="field">
              <label>Asset Type</label>
              <select value={filterAssetType} onChange={e => { setFilterAssetType(e.target.value); setAssetPage(1); }}>
                <option value="">All assets</option>
                {assetTypes.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
              </select>
            </div>
          </div>

          <div className="card">
            <div className="table-wrap">
              {assetLoading
                ? <div className="loading">Loading…</div>
                : (
                  <table>
                    <thead>
                      <tr>
                        <th>Asset</th>
                        <th>From</th>
                        <th>To</th>
                        <th className="text-right">Quantity</th>
                        <th>Trade</th>
                        <th>Date</th>
                      </tr>
                    </thead>
                    <tbody>
                      {assetData?.items.length === 0 && (
                        <tr><td colSpan={6}><div className="empty">No asset transfers</div></td></tr>
                      )}
                      {assetData?.items.map(t => (
                        <tr key={t.id}>
                          <td><strong>{t.assetCode}</strong></td>
                          <td>{traderLink(t.fromTraderId, t.fromTraderName)}</td>
                          <td>{traderLink(t.toTraderId, t.toTraderName)}</td>
                          <td className="text-right text-green">{fmt(t.quantity)}</td>
                          <td>
                            {t.tradeId
                              ? <span className="td-mono">{shortId(t.tradeId)}</span>
                              : <span className="text-muted">—</span>}
                          </td>
                          <td className="text-muted">{formatDate(t.createdAt)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
            </div>
            {assetData && (
              <Paginator page={assetPage} pageSize={50} totalCount={assetData.totalCount} onPage={setAssetPage} />
            )}
          </div>
        </>
      )}

      {/* Credit Transfers */}
      {tab === 'credits' && (
        <>
          <div className="filter-bar">
            <div className="field">
              <label>Trader</label>
              <select value={filterCreditTrader} onChange={e => { setFilterCreditTrader(e.target.value); setCreditPage(1); }}>
                <option value="">All traders</option>
                {traders.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
              </select>
            </div>
          </div>

          <div className="card">
            <div className="table-wrap">
              {creditLoading
                ? <div className="loading">Loading…</div>
                : (
                  <table>
                    <thead>
                      <tr>
                        <th>From</th>
                        <th>To</th>
                        <th className="text-right">Amount</th>
                        <th>Trade</th>
                        <th>Date</th>
                      </tr>
                    </thead>
                    <tbody>
                      {creditData?.items.length === 0 && (
                        <tr><td colSpan={5}><div className="empty">No credit transfers</div></td></tr>
                      )}
                      {creditData?.items.map(t => (
                        <tr key={t.id}>
                          <td>{traderLink(t.fromTraderId, t.fromTraderName)}</td>
                          <td>{traderLink(t.toTraderId, t.toTraderName)}</td>
                          <td className="text-right text-green"><strong>{fmt(t.amount)}</strong></td>
                          <td>
                            {t.tradeId
                              ? <span className="td-mono">{shortId(t.tradeId)}</span>
                              : <span className="text-muted">—</span>}
                          </td>
                          <td className="text-muted">{formatDate(t.createdAt)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
            </div>
            {creditData && (
              <Paginator page={creditPage} pageSize={50} totalCount={creditData.totalCount} onPage={setCreditPage} />
            )}
          </div>
        </>
      )}
    </div>
  );
}
