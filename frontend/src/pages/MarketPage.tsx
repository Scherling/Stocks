import { useState, useEffect } from 'react';
import { marketApi, assetTypesApi } from '../api';
import type { AssetTypeResponse, MarketStatsResponse, MarketDepthResponse, RecentTradeResponse } from '../types';
import { formatDate, fmt } from '../utils';

export default function MarketPage() {
  const [assetTypes, setAssetTypes] = useState<AssetTypeResponse[]>([]);
  const [selectedId, setSelectedId] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const [stats, setStats] = useState<MarketStatsResponse | null>(null);
  const [depth, setDepth] = useState<MarketDepthResponse | null>(null);
  const [recentTrades, setRecentTrades] = useState<RecentTradeResponse[]>([]);

  useEffect(() => {
    assetTypesApi.list()
      .then(items => {
        setAssetTypes(items);
        const active = items.find(a => a.isActive);
        if (active) setSelectedId(active.id);
      })
      .catch(e => setError(e.message));
  }, []);

  useEffect(() => {
    if (!selectedId) return;
    setLoading(true);
    setError('');
    Promise.all([
      marketApi.stats(selectedId),
      marketApi.depth(selectedId),
      marketApi.recentTrades(selectedId, 20),
    ])
      .then(([s, d, r]) => { setStats(s); setDepth(d); setRecentTrades(r); })
      .catch(e => setError(e.message))
      .finally(() => setLoading(false));
  }, [selectedId]);

  function refresh() {
    if (!selectedId) return;
    setLoading(true);
    setError('');
    Promise.all([
      marketApi.stats(selectedId),
      marketApi.depth(selectedId),
      marketApi.recentTrades(selectedId, 20),
    ])
      .then(([s, d, r]) => { setStats(s); setDepth(d); setRecentTrades(r); })
      .catch(e => setError(e.message))
      .finally(() => setLoading(false));
  }

  return (
    <div className="page">
      <div className="page-header">
        <h1 className="page-title">Market Analytics</h1>
        <div className="row">
          <select
            value={selectedId}
            onChange={e => setSelectedId(e.target.value)}
            style={{ minWidth: 200 }}
          >
            <option value="">Select asset…</option>
            {assetTypes.map(a => (
              <option key={a.id} value={a.id}>{a.name}</option>
            ))}
          </select>
          <button className="btn btn-secondary btn-sm" onClick={refresh} disabled={!selectedId || loading}>
            ↻ Refresh
          </button>
        </div>
      </div>

      {error && <div className="error-banner">{error}</div>}

      {!selectedId && (
        <div className="empty">Select an asset type to view market data.</div>
      )}

      {selectedId && loading && <div className="loading">Loading…</div>}

      {selectedId && !loading && stats && (
        <>
          {/* Stats */}
          <div className="stat-grid">
            <div className="stat-card">
              <div className="stat-label">Best Ask</div>
              <div className={`stat-value ${stats.bestAsk != null ? 'green' : ''}`}>
                {stats.bestAsk != null ? fmt(stats.bestAsk) : '—'}
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Latest Price</div>
              <div className="stat-value">
                {stats.latestTradedPrice != null ? fmt(stats.latestTradedPrice) : '—'}
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-label">VWAP</div>
              <div className="stat-value">
                {stats.vwap != null ? fmt(stats.vwap) : '—'}
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Avg Price</div>
              <div className="stat-value">
                {stats.averagePrice != null ? fmt(stats.averagePrice) : '—'}
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Total Volume</div>
              <div className="stat-value yellow">{fmt(stats.totalVolume, 0)}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Trade Count</div>
              <div className="stat-value">{stats.totalTradeCount}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Open Sell Volume</div>
              <div className="stat-value">{fmt(stats.openSellVolume, 0)}</div>
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            {/* Market Depth */}
            <div className="card" style={{ marginBottom: 0 }}>
              <div className="card-header">
                Order Book — {depth?.assetName}
                {depth?.bestAsk != null && (
                  <span className="badge badge-green ml-auto">Best ask: {fmt(depth.bestAsk)}</span>
                )}
              </div>
              <div className="table-wrap">
                {(depth?.levels.length ?? 0) === 0
                  ? <div className="empty">No open orders</div>
                  : (
                    <table>
                      <thead>
                        <tr>
                          <th className="text-right">Price</th>
                          <th className="text-right">Volume</th>
                          <th className="text-right">Orders</th>
                        </tr>
                      </thead>
                      <tbody>
                        {depth?.levels.map((l, i) => (
                          <tr key={i}>
                            <td className="text-right"><strong>{fmt(l.unitPrice)}</strong></td>
                            <td className="text-right">{fmt(l.totalQuantity)}</td>
                            <td className="text-right text-muted">{l.orderCount}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
              </div>
              {depth && (
                <div style={{ padding: '10px 14px', borderTop: '1px solid var(--border)', fontSize: 12, color: 'var(--text-muted)' }}>
                  Total open volume: <strong>{fmt(depth.totalOpenVolume)}</strong>
                </div>
              )}
            </div>

            {/* Recent Trades */}
            <div className="card" style={{ marginBottom: 0 }}>
              <div className="card-header">Recent Trades</div>
              <div className="table-wrap">
                {recentTrades.length === 0
                  ? <div className="empty">No recent trades</div>
                  : (
                    <table>
                      <thead>
                        <tr>
                          <th className="text-right">Qty</th>
                          <th className="text-right">Avg Price</th>
                          <th className="text-right">Total</th>
                          <th>Time</th>
                        </tr>
                      </thead>
                      <tbody>
                        {recentTrades.map((t, i) => (
                          <tr key={i}>
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
            </div>
          </div>
        </>
      )}
    </div>
  );
}
