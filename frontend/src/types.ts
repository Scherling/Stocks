// ── Enums ────────────────────────────────────────────────────────────────────
export type TraderStatus = 'Active' | 'Inactive';
export type SellOrderStatus = 'Open' | 'PartiallyFilled' | 'Filled' | 'Cancelled';

// ── Common ────────────────────────────────────────────────────────────────────
export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ── Traders ───────────────────────────────────────────────────────────────────
export interface TraderResponse {
  id: string;
  name: string;
  status: TraderStatus;
  createdAt: string;
  updatedAt: string;
}

export interface AssetBalanceResponse {
  assetTypeId: string;
  assetCode: string;
  assetName: string;
  totalQuantity: number;
  reservedQuantity: number;
  availableQuantity: number;
}

export interface TraderBalancesResponse {
  traderId: string;
  credits: number;
  assetBalances: AssetBalanceResponse[];
}

// ── Asset Types ───────────────────────────────────────────────────────────────
export interface AssetTypeResponse {
  id: string;
  slug: string;
  name: string;
  unitName: string;
  category: string;
  stage: string;
  description: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

// ── Sell Orders ───────────────────────────────────────────────────────────────
export interface SellOrderResponse {
  id: string;
  traderId: string;
  traderName: string;
  assetTypeId: string;
  assetCode: string;
  assetName: string;
  originalQuantity: number;
  remainingQuantity: number;
  filledQuantity: number;
  unitPrice: number;
  status: SellOrderStatus;
  createdAt: string;
  updatedAt: string;
}

export interface MarketDepthLevelResponse {
  unitPrice: number;
  totalQuantity: number;
  orderCount: number;
}

export interface MarketDepthResponse {
  assetTypeId: string;
  assetCode: string;
  assetName: string;
  bestAsk: number | null;
  totalOpenVolume: number;
  levels: MarketDepthLevelResponse[];
}

// ── Quotes ────────────────────────────────────────────────────────────────────
export interface QuoteFillPreviewResponse {
  sellOrderId: string;
  sellerTraderId: string;
  quantity: number;
  unitPrice: number;
  subTotal: number;
}

export interface QuoteResponse {
  assetTypeId: string;
  requestedQuantity: number;
  isFillable: boolean;
  totalCost: number | null;
  averageUnitPrice: number | null;
  minUnitPrice: number | null;
  maxUnitPrice: number | null;
  ordersConsumed: number;
  availableQuantity: number;
  buyerHasSufficientCredits: boolean | null;
  fillPreview: QuoteFillPreviewResponse[];
}

// ── Trades ────────────────────────────────────────────────────────────────────
export interface TradeFillResponse {
  id: string;
  sellOrderId: string;
  sellerTraderId: string;
  quantity: number;
  unitPrice: number;
  subTotal: number;
  executedAt: string;
}

export interface TradeResponse {
  id: string;
  buyerTraderId: string;
  assetTypeId: string;
  assetCode: string;
  requestedQuantity: number;
  totalQuantity: number;
  totalCost: number;
  averageUnitPrice: number;
  executedAt: string;
  idempotencyKey: string | null;
  fills: TradeFillResponse[];
}

// ── Ledger ────────────────────────────────────────────────────────────────────
export interface LedgerEntryResponse {
  id: string;
  traderId: string;
  assetTypeId: string | null;
  assetCode: string | null;
  tradeId: string | null;
  sellOrderId: string | null;
  entryType: number;
  entryTypeName: string;
  quantityDelta: number | null;
  creditDelta: number | null;
  metadata: string | null;
  createdAt: string;
}

export interface AssetTransferResponse {
  id: string;
  fromTraderId: string | null;
  fromTraderName: string | null;
  toTraderId: string | null;
  toTraderName: string | null;
  assetTypeId: string;
  assetCode: string;
  quantity: number;
  tradeId: string | null;
  createdAt: string;
}

export interface CreditTransferResponse {
  id: string;
  fromTraderId: string | null;
  fromTraderName: string | null;
  toTraderId: string | null;
  toTraderName: string | null;
  amount: number;
  tradeId: string | null;
  createdAt: string;
}

// ── Market Analytics ──────────────────────────────────────────────────────────
export interface MarketStatsResponse {
  assetTypeId: string;
  assetCode: string;
  from: string | null;
  to: string | null;
  latestTradedPrice: number | null;
  averagePrice: number | null;
  vwap: number | null;
  totalVolume: number;
  totalTradeCount: number;
  bestAsk: number | null;
  openSellVolume: number;
}

export interface RecentTradeResponse {
  tradeId: string;
  buyerTraderId: string;
  totalQuantity: number;
  averageUnitPrice: number;
  totalCost: number;
  executedAt: string;
}
