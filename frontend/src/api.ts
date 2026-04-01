import type {
  PaginatedResponse,
  TraderResponse, TraderBalancesResponse,
  AssetTypeResponse,
  SellOrderResponse, MarketDepthResponse,
  QuoteResponse,
  TradeResponse, TradeFillResponse,
  LedgerEntryResponse, AssetTransferResponse, CreditTransferResponse,
  MarketStatsResponse, RecentTradeResponse,
} from './types';

async function req<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`/api${path}`, {
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    ...options,
  });
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.detail ?? body.title ?? `${res.status} ${res.statusText}`);
  }
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

function qs(params: Record<string, string | number | undefined | null>): string {
  const p = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) {
    if (v != null && v !== '') p.set(k, String(v));
  }
  const s = p.toString();
  return s ? `?${s}` : '';
}

// ── Traders ───────────────────────────────────────────────────────────────────
export const tradersApi = {
  list: (page = 1, pageSize = 50) =>
    req<PaginatedResponse<TraderResponse>>(`/traders${qs({ page, pageSize })}`),
  get: (id: string) =>
    req<TraderResponse>(`/traders/${id}`),
  create: (name: string) =>
    req<TraderResponse>('/traders', { method: 'POST', body: JSON.stringify({ name }) }),
  update: (id: string, name: string) =>
    req<TraderResponse>(`/traders/${id}`, { method: 'PUT', body: JSON.stringify({ name }) }),
  delete: (id: string) =>
    req<void>(`/traders/${id}`, { method: 'DELETE' }),
  balances: (id: string) =>
    req<TraderBalancesResponse>(`/traders/${id}/balances`),
  adjustCredits: (id: string, amount: number, reason: string) =>
    req<void>(`/traders/${id}/credits/adjust`, {
      method: 'POST', body: JSON.stringify({ amount, reason }),
    }),
  adjustAsset: (id: string, assetTypeId: string, amount: number, reason: string) =>
    req<void>(`/traders/${id}/assets/adjust`, {
      method: 'POST', body: JSON.stringify({ assetTypeId, amount, reason }),
    }),
  sellOrders: (id: string, page = 1, pageSize = 50) =>
    req<PaginatedResponse<SellOrderResponse>>(`/traders/${id}/sell-orders${qs({ page, pageSize })}`),
  trades: (id: string, page = 1, pageSize = 50) =>
    req<PaginatedResponse<TradeResponse>>(`/traders/${id}/trades${qs({ page, pageSize })}`),
  ledger: (id: string, page = 1, pageSize = 50) =>
    req<PaginatedResponse<LedgerEntryResponse>>(`/traders/${id}/ledger${qs({ page, pageSize })}`),
};

// ── Asset Types ───────────────────────────────────────────────────────────────
export const assetTypesApi = {
  list: () =>
    req<AssetTypeResponse[]>('/asset-types'),
  create: (slug: string, name: string, unitName: string) =>
    req<AssetTypeResponse>('/asset-types', { method: 'POST', body: JSON.stringify({ slug, name, unitName }) }),
  update: (id: string, name: string, unitName: string) =>
    req<AssetTypeResponse>(`/asset-types/${id}`, { method: 'PUT', body: JSON.stringify({ name, unitName }) }),
  deactivate: (id: string) =>
    req<void>(`/asset-types/${id}/deactivate`, { method: 'PATCH' }),
};

// ── Sell Orders ───────────────────────────────────────────────────────────────
export const sellOrdersApi = {
  list: (assetTypeId?: string, traderId?: string, status?: string, page = 1, pageSize = 50) =>
    req<PaginatedResponse<SellOrderResponse>>(`/sell-orders${qs({ assetTypeId, traderId, status, page, pageSize })}`),
  create: (traderId: string, assetTypeId: string, quantity: number, unitPrice: number) =>
    req<SellOrderResponse>('/sell-orders', { method: 'POST', body: JSON.stringify({ traderId, assetTypeId, quantity, unitPrice }) }),
  update: (id: string, unitPrice?: number, quantity?: number) =>
    req<SellOrderResponse>(`/sell-orders/${id}`, { method: 'PATCH', body: JSON.stringify({ unitPrice, quantity }) }),
  cancel: (id: string) =>
    req<void>(`/sell-orders/${id}`, { method: 'DELETE' }),
  depth: (assetTypeId: string) =>
    req<MarketDepthResponse>(`/sell-orders/market-depth/${assetTypeId}`),
};

// ── Quotes ────────────────────────────────────────────────────────────────────
export const quotesApi = {
  get: (assetTypeId: string, quantity: number, buyerTraderId?: string) =>
    req<QuoteResponse>(`/quotes${qs({ assetTypeId, quantity, buyerTraderId })}`),
};

// ── Trades ────────────────────────────────────────────────────────────────────
export const tradesApi = {
  list: (assetTypeId?: string, buyerTraderId?: string, page = 1, pageSize = 50) =>
    req<PaginatedResponse<TradeResponse>>(`/trades${qs({ assetTypeId, buyerTraderId, page, pageSize })}`),
  get: (id: string) =>
    req<TradeResponse>(`/trades/${id}`),
  execute: (buyerTraderId: string, assetTypeId: string, quantity: number, idempotencyKey?: string) =>
    req<TradeResponse>('/trades', { method: 'POST', body: JSON.stringify({ buyerTraderId, assetTypeId, quantity, idempotencyKey }) }),
  fills: (id: string) =>
    req<TradeFillResponse[]>(`/trades/${id}/fills`),
};

// ── Ledger ────────────────────────────────────────────────────────────────────
export const ledgerApi = {
  assetTransfers: (traderId?: string, assetTypeId?: string, page = 1, pageSize = 50) =>
    req<PaginatedResponse<AssetTransferResponse>>(`/ledger/asset-transfers${qs({ traderId, assetTypeId, page, pageSize })}`),
  creditTransfers: (traderId?: string, page = 1, pageSize = 50) =>
    req<PaginatedResponse<CreditTransferResponse>>(`/ledger/credit-transfers${qs({ traderId, page, pageSize })}`),
  forTrade: (id: string) =>
    req<LedgerEntryResponse[]>(`/ledger/trade/${id}`),
  forSellOrder: (id: string) =>
    req<LedgerEntryResponse[]>(`/ledger/sell-order/${id}`),
};

// ── Market ────────────────────────────────────────────────────────────────────
export const marketApi = {
  stats: (assetTypeId: string, from?: string, to?: string) =>
    req<MarketStatsResponse>(`/market/${assetTypeId}/stats${qs({ from, to })}`),
  depth: (assetTypeId: string) =>
    req<MarketDepthResponse>(`/market/${assetTypeId}/depth`),
  recentTrades: (assetTypeId: string, limit = 20) =>
    req<RecentTradeResponse[]>(`/market/${assetTypeId}/recent-trades${qs({ limit })}`),
};
