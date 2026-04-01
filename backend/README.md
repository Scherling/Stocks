# Market API

A production-quality simulation game market API built with .NET 10 Minimal API and PostgreSQL.

## Architecture Overview

```
backend/
  Stocks.Api/                  — HTTP layer: endpoints, middleware, DI composition root
  src/
    Market.Domain/             — Entities and enums; no dependencies
    Market.Application/        — Services, DTOs, interfaces; depends on Domain
    Market.Infrastructure/     — EF Core DbContext, migrations, seed; depends on Application
  tests/
    Market.Tests.Unit/         — xUnit + in-memory EF Core; 28 tests
    Market.Tests.Integration/  — xUnit + Testcontainers (real PostgreSQL)
```

The architecture follows a layered approach with **no repository abstraction** — services consume `IMarketDbContext` directly. This keeps the design pragmatic while still enabling unit testing without a real database.

## Domain Model

```
Trader ──< SellOrder ──< TradeFill >── Trade
  │                                       │
  ├── TraderCreditBalance          BuyerTrader
  ├── TraderAssetBalance
  ├── LedgerEntry
  ├── AssetTransfer (as from/to)
  └── CreditTransfer (as from/to)
```

**Trader** — A market participant with credits and asset balances. Soft-deleted (Status = Inactive).

**SellOrder** — An offer to sell a quantity of an asset at a unit price. Reserves inventory on creation; reservation released on cancellation or fill.

**Trade** — A purchase: buyer buys from the cheapest available sell orders (full-fill-or-nothing). Creates TradeFill records for each matched order.

**LedgerEntry** — Immutable append-only audit trail of every credit and asset movement.

**AssetTransfer / CreditTransfer** — Immutable transfer records linking movements to trades/orders.

## Setup

### Prerequisites
- .NET 10 SDK
- Docker (for PostgreSQL or integration tests)

### Database

Start PostgreSQL with Docker Compose:
```bash
docker compose up -d
```

Or connect to an existing PostgreSQL instance and update the connection string in `Stocks.Api/appsettings.json`.

Default connection:
```
Host=localhost;Port=5432;Database=stocks_dev;Username=service_user;Password=Dwarf1234
```

### Migrations

Apply migrations from the `Stocks.Api` project (which references Infrastructure):
```bash
cd Stocks.Api
dotnet ef database update --project ../src/Market.Infrastructure
```

Or generate a new migration after model changes:
```bash
dotnet ef migrations add <MigrationName> --project ../src/Market.Infrastructure --startup-project .
```

### Running

```bash
cd Stocks.Api
dotnet run
```

The API starts on `http://localhost:5000`. In Development mode, migrations are applied automatically on startup and seed data is inserted (5 traders, 3 asset types, 10 sell orders).

### API Documentation

Scalar UI is available at: `http://localhost:5000/scalar/v1`

Health check: `http://localhost:5000/health`

## How Trade Execution Works

A trade is a buyer purchasing a quantity of an asset. The system matches against the cheapest available sell orders:

1. **Quote first** — `GET /api/quotes?assetTypeId=&quantity=&buyerTraderId=` shows what a purchase would cost without committing anything.
2. **Execute** — `POST /api/trades` atomically:
   - Locks sell orders `FOR UPDATE` in price/time order
   - Locks buyer's credit balance `FOR UPDATE`
   - Locks each seller's asset balance and credit balance `FOR UPDATE`
   - Deducts buyer credits, transfers assets, credits each seller
   - Writes Trade, TradeFill, LedgerEntry, AssetTransfer, CreditTransfer records
   - Commits

If the requested quantity cannot be fully filled, the entire trade is rejected (`OrderNotFillableException`).

### Anti-wash-trading

A buyer's own sell orders are excluded from matching. This prevents self-trades.

### Idempotency

Pass `Idempotency-Key: <uuid>` on `POST /api/trades` to safely retry on network failure. The second call returns the original trade without double-executing.

## Concurrency Strategy

All inventory operations use **pessimistic locking** (`SELECT ... FOR UPDATE`) at `RepeatableRead` isolation level. This prevents double-selling under concurrent load.

### Lock acquisition order (deadlock prevention)

Locks are always acquired in this fixed order:

1. Sell orders (as a set, ordered by `unit_price ASC, created_at ASC`)
2. Buyer credit balance
3. Seller asset balances — sorted by `TraderId ASC`
4. Seller credit balances — sorted by `TraderId ASC`

Consistent ordering means two concurrent trades cannot form a cycle, eliminating deadlocks.

### Why pessimistic over optimistic?

Optimistic concurrency causes failed retries under contention, making the caller responsible for retry logic. Pessimistic locking serializes at the database, which is correct for a game market where inventory is scarce and races are expected.

## Endpoints

### Traders
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/traders` | List traders (paginated) |
| POST | `/api/traders` | Create trader |
| GET | `/api/traders/{id}` | Get trader |
| PUT | `/api/traders/{id}` | Update trader |
| DELETE | `/api/traders/{id}` | Soft-delete trader |
| GET | `/api/traders/{id}/balances` | Credit + asset balances |
| POST | `/api/traders/{id}/credits/adjust` | Admin: add/remove credits |
| POST | `/api/traders/{id}/assets/adjust` | Admin: add/remove asset units |
| GET | `/api/traders/{id}/sell-orders` | Trader's open orders |
| GET | `/api/traders/{id}/trades` | Trader's trade history |
| GET | `/api/traders/{id}/ledger` | Trader's ledger entries |

### Asset Types
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/asset-types` | List asset types |
| POST | `/api/asset-types` | Create asset type |
| GET | `/api/asset-types/{id}` | Get asset type |
| PUT | `/api/asset-types/{id}` | Update asset type |
| PATCH | `/api/asset-types/{id}/deactivate` | Deactivate asset type |

### Sell Orders
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/sell-orders` | Create order (reserves inventory) |
| GET | `/api/sell-orders` | List orders (filter: assetTypeId, traderId, status) |
| GET | `/api/sell-orders/{id}` | Get order |
| PATCH | `/api/sell-orders/{id}` | Update price or quantity |
| DELETE | `/api/sell-orders/{id}` | Cancel order (releases reservation) |

### Quotes & Trades
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/quotes` | Quote a purchase (read-only) |
| POST | `/api/trades` | Execute purchase |
| GET | `/api/trades` | List trades |
| GET | `/api/trades/{id}` | Get trade |
| GET | `/api/trades/{id}/fills` | Get fills for trade |

### Ledger
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/ledger/trader/{id}` | Ledger for trader |
| GET | `/api/ledger/sell-order/{id}` | Ledger for sell order |
| GET | `/api/ledger/trade/{id}` | Ledger for trade |
| GET | `/api/ledger/asset-transfers` | Asset transfer history |
| GET | `/api/ledger/credit-transfers` | Credit transfer history |

### Market Analytics
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/market/{assetTypeId}/best-ask` | Lowest available price |
| GET | `/api/market/{assetTypeId}/open-volume` | Total open sell volume |
| GET | `/api/market/{assetTypeId}/depth` | Price depth (grouped by price level) |
| GET | `/api/market/{assetTypeId}/recent-trades` | Recent completed trades |
| GET | `/api/market/{assetTypeId}/stats` | VWAP, avg price, volume (date range) |

## Sample curl Commands

```bash
# List traders
curl http://localhost:5000/api/traders

# Get a quote for 50 GRAIN
ASSET_ID=$(curl -s http://localhost:5000/api/asset-types | jq -r '.items[] | select(.code=="GRAIN") | .id')
BUYER_ID=$(curl -s http://localhost:5000/api/traders | jq -r '.items[] | select(.name=="Alice") | .id')
curl "http://localhost:5000/api/quotes?assetTypeId=$ASSET_ID&quantity=50&buyerTraderId=$BUYER_ID"

# Execute a trade
curl -X POST http://localhost:5000/api/trades \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: $(uuidgen)" \
  -d "{\"buyerTraderId\": \"$BUYER_ID\", \"assetTypeId\": \"$ASSET_ID\", \"quantity\": 50}"

# Create a sell order
SELLER_ID=$(curl -s http://localhost:5000/api/traders | jq -r '.items[] | select(.name=="Bob") | .id')
curl -X POST http://localhost:5000/api/sell-orders \
  -H "Content-Type: application/json" \
  -d "{\"traderId\": \"$SELLER_ID\", \"assetTypeId\": \"$ASSET_ID\", \"quantity\": 100, \"unitPrice\": 12.50}"

# Market depth
curl "http://localhost:5000/api/market/$ASSET_ID/depth"

# Market stats (last 7 days)
FROM=$(date -u -v-7d +"%Y-%m-%dT%H:%M:%SZ" 2>/dev/null || date -u -d "7 days ago" +"%Y-%m-%dT%H:%M:%SZ")
TO=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
curl "http://localhost:5000/api/market/$ASSET_ID/stats?from=$FROM&to=$TO"
```

## Running Tests

### Unit tests
```bash
cd tests/Market.Tests.Unit
dotnet test
```

### Integration tests

Integration tests require Docker (Testcontainers pulls `postgres:16-alpine`):
```bash
cd tests/Market.Tests.Integration
dotnet test
```

The integration tests include a concurrent trade test that fires two simultaneous purchase requests for the same inventory and verifies exactly one succeeds.

## Assumptions & Limitations

- **Quantities are decimal** — fractional units are allowed (e.g., 0.5 GRAIN).
- **Full-fill-or-nothing** — a purchase either fills the entire requested quantity or fails. No partial purchases.
- **No authentication** — endpoints are open. A `// TODO: add bearer token middleware` placeholder exists in Program.cs.
- **Credits are administrative** — added via `POST /api/traders/{id}/credits/adjust`. There is no payment system.
- **Asset adjustments are administrative** — inventory is added via `POST /api/traders/{id}/assets/adjust`. There is no crafting or production system.
- **Timestamps are UTC** — all `CreatedAt`/`UpdatedAt`/`ExecutedAt` fields are stored and returned in UTC.
- **Analytics are on-demand** — VWAP and volume stats are computed from `trade_fills` at query time, not pre-aggregated.
- **Sell order price updates are allowed after partial fill** — this matches common exchange behavior.
- **Traders are soft-deleted** — deleting a trader sets `Status = Inactive`, preserving referential integrity with historical orders and trades.
- **Enum storage** — `TraderStatus` and `SellOrderStatus` are stored as strings (via EF `HasConversion<string>()`) rather than PostgreSQL native enums, for simpler migrations.
