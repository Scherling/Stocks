You are a senior .NET architect and backend engineer.

Create a production-quality .NET API for a simulation game market system.

## High-level goal

Build a market API backed by PostgreSQL for a simulation game.

This market supports:
- Sell orders only
- No buy order book
- Traders/agents that own assets and credits
- Trade execution where buyers purchase from the cheapest available sell orders
- Price quotation before purchase
- Strong transaction logging and auditability
- Sufficient historical data to calculate market averages and other analytics

The solution should be designed as if it will later need to support thousands of traders and many transactions.

## Tech stack requirements

Use:
- .NET 8
- ASP.NET Core Web API
- PostgreSQL
- Entity Framework Core with Npgsql
- Clean, maintainable project structure
- Proper transactional consistency
- Optimistic or pessimistic locking where appropriate to prevent double-selling
- OpenAPI / Swagger
- Docker support for local development
- Seed data support
- Migrations

## Output I want from you

Create a complete implementation in code, not just an outline.

I want:
1. A .NET project with sensible structure
2. Domain model
3. EF Core DbContext and configurations
4. Migrations
5. API endpoints
6. Application/services layer
7. Validation
8. Error handling
9. Transaction-safe trade execution
10. Audit and transaction history
11. Read models / query endpoints for market analytics
12. Seed/demo data
13. README with setup and usage instructions
14. Example requests/responses
15. Unit tests for core business logic
16. Integration tests for the trade flow if practical

## Domain language

Use clear domain terms. Prefer these names unless you have a very good reason not to:
- Trader = a market participant
- AssetType = the type of good/resource/item being traded
- TraderAssetBalance = how much of a given asset a trader owns
- CreditBalance = how many credits/currency units a trader has
- SellOrder = an offer to sell a quantity of one AssetType at a fixed unit price
- Trade = a completed purchase operation initiated by a buyer
- TradeFill = each partial fill against a specific sell order
- LedgerEntry = immutable transaction/audit record
- MarketPriceSnapshot or MarketStats = optional aggregated analytics data

## Important business rules

### Market model
- This is a sell-order-only market.
- Buyers do not place persistent buy orders.
- A buyer can request to purchase X units of an asset.
- The system must determine whether enough quantity exists across current sell orders.
- If enough quantity exists, the buyer either:
  - buys all requested units, or
  - buys nothing
- Partial purchase of requested quantity is NOT allowed for the final purchase operation.
- However, the purchase may internally fill across multiple sell orders.

### Pricing model
- Sell orders are matched by lowest price first.
- If multiple sell orders have the same price, use FIFO by creation time.
- A quote for 10 units may differ from a quote for 100 units because more expensive sell orders may need to be included.
- A quote operation must calculate:
  - whether the requested quantity can be fully satisfied
  - the total cost
  - average unit price
  - which sell orders would be consumed
  - price impact / breakdown per level if useful
- The actual execute-purchase operation should use the same matching logic.

### Trader balances
- Each trader has:
  - a credit balance
  - asset balances per asset type
- A trader cannot sell more of an asset than they actually have available.
- A trader cannot spend more credits than they have available.
- Assets locked in active sell orders must not be spendable or resellable elsewhere.
- It is acceptable and preferred to explicitly track:
  - total asset balance
  - reserved asset balance
  - available asset balance
- Likewise for credits if needed in future, though for now credits are only debited at execution time.

### Sell order lifecycle
A sell order should support:
- Create
- Read
- Update
- Cancel / delete
- List/filter
- Optional pause/reactivate only if it makes sense; otherwise skip
- A sell order should track:
  - trader id
  - asset type id
  - original quantity
  - remaining quantity
  - unit price
  - status
  - created at
  - updated at
  - optional version/concurrency token

Rules:
- On order creation, reserve the seller’s asset quantity.
- On order update:
  - asset type should probably be immutable once created
  - reducing/increasing quantity must properly adjust reserved balances
  - price can be updated
- On cancel/delete:
  - unfilled reserved quantity must be released back to the seller’s available balance
- Completed orders should not be editable.

### Trade execution
A purchase flow should:
1. Accept buyer trader id, asset type id, requested quantity
2. Find cheapest available sell orders in correct order
3. Verify full requested quantity is available
4. Compute total cost
5. Verify buyer has enough credits
6. Execute atomically:
   - debit buyer credits
   - credit seller credits
   - transfer asset units from sellers to buyer
   - decrement or complete sell orders
   - release consumed reserved quantities appropriately
   - create trade record
   - create trade fill records
   - create immutable ledger entries
7. Commit transaction
8. Return detailed execution result

This must be transaction-safe and protected against race conditions.

### Transaction logging / auditability
This is very important.

All meaningful market-changing operations must leave a durable audit trail in the database.

I want immutable records sufficient to reconstruct what happened.

At minimum, log:
- Trader creation/update if meaningful
- Credit adjustments
- Asset balance adjustments
- Sell order creation/update/cancel
- Trade execution
- Trade fills
- Asset transfers
- Credit transfers

Use an append-only ledger style where appropriate.

The goal is that we can answer later:
- What trades happened?
- Which orders were matched?
- Which trader sold to which buyer?
- What was the executed price?
- How did a trader’s balances change over time?
- What is the average market price for an asset over time?

### Market averages / analytics
The system must persist enough transaction history to calculate market metrics for each AssetType.

Provide endpoints/queries for things like:
- latest traded price
- simple average traded price over a period
- volume-weighted average price (VWAP) over a period
- total traded volume over a period
- open sell volume
- best ask
- market depth snapshot
- recent trade history

Use completed trades / trade fills as the source of truth for market averages, not open orders.

## Required operations / endpoints

Implement these as HTTP API endpoints with proper request/response models.

### Trader operations
Create endpoints for:
- Create trader
- Get trader by id
- List traders
- Update trader basic info
- Delete trader if safe, or soft-delete if better
- Get trader balances
- Adjust trader credits (admin/system endpoint)
- Adjust trader asset balance (admin/system endpoint)
- Get trader open sell orders
- Get trader trade history
- Get trader ledger entries / balance history

Trader fields can include:
- id
- name
- status
- created at
- updated at

### AssetType operations
Create endpoints for:
- Create asset type
- Get asset type
- List asset types
- Update asset type
- Deactivate asset type if useful

AssetType fields can include:
- id
- code
- name
- unit name
- is active

### Trader asset balance operations
Create endpoints for:
- Get all balances for a trader
- Get balance for a trader + asset type
- Adjust balances through admin/system operation
- Optionally transfer assets directly between traders if you think that helps for setup/admin scenarios

Track:
- total quantity
- reserved quantity
- available quantity

### Sell order operations
Create endpoints for:
- Create sell order
- Get sell order by id
- List sell orders
- Filter sell orders by asset type
- Filter sell orders by trader
- Update sell order price
- Update sell order quantity
- Cancel sell order
- Delete sell order only if safe; otherwise prefer cancel semantics
- Get open market depth for an asset type
- Get best ask for an asset type

### Quote operations
Create endpoints for:
- Quote purchase price for asset type + quantity + buyer id optional
- Return whether fill is possible
- Return total cost
- Return average unit price
- Return effective price range
- Return how many orders would be consumed
- Return detailed fill preview
- Optionally return whether buyer currently has sufficient credits

This endpoint must NOT mutate state.

### Trade operations
Create endpoints for:
- Execute purchase for buyer + asset type + quantity
- Get trade by id
- List trades
- Filter trades by asset type
- Filter trades by trader
- Get fills for a trade
- Get recent trades for asset type

The execute purchase operation is one of the most critical parts of the system.

### Ledger / audit operations
Create endpoints for:
- Get ledger entries for trader
- Get ledger entries for sell order
- Get ledger entries for trade
- Get asset transfer history
- Get credit transfer history
- Get balance change history over time

### Market analytics operations
Create endpoints for:
- Get current best ask for asset type
- Get current open sell volume for asset type
- Get market depth by price level for asset type
- Get recent trade history for asset type
- Get latest traded price for asset type
- Get average traded price over a date range
- Get VWAP over a date range
- Get total traded volume over a date range
- Optionally OHLC candlestick-style aggregation if easy to add

## Database / schema requirements

Design a PostgreSQL schema that supports the above.

I expect tables roughly along these lines, though refine as needed:
- traders
- asset_types
- trader_asset_balances
- trader_credit_balances
- sell_orders
- trades
- trade_fills
- ledger_entries
- credit_transfers
- asset_transfers
- optional market_price_snapshots

Use proper indexes.

Important indexes likely include:
- sell_orders on (asset_type_id, status, unit_price, created_at)
- trade_fills on (asset_type_id, executed_at)
- trades on (buyer_trader_id, executed_at)
- sell_orders on trader_id
- ledger_entries on trader_id, created_at

Consider numeric precision carefully.
Use decimal/numeric for currency and prices.
Use decimal/numeric for quantities too, unless you intentionally decide quantities must be whole integers.

## Concurrency and correctness requirements

This is extremely important.

Design the trade execution so the same inventory cannot be sold twice.

You may use:
- database transactions
- row locking
- SELECT FOR UPDATE via EF/Npgsql if appropriate
- concurrency tokens/version columns

The final design should be robust enough for concurrent purchase attempts.

At minimum:
- lock the sell orders being consumed
- lock relevant buyer/seller balance rows
- ensure reserved quantities are handled correctly
- rollback fully on failure

Document your concurrency approach clearly in the README and code comments.

## Validation rules

Include validation such as:
- trader must exist
- asset type must exist and be active
- quantity > 0
- price > 0
- seller cannot create order for quantity exceeding available balance
- buyer cannot buy from own order if that should be disallowed; choose a rule and document it
- completed/cancelled orders cannot be modified
- purchase requires full fill availability
- purchase requires enough buyer credits
- no negative balances
- no invalid transitions

## Design expectations

Use a clean structure, for example:
- src/Market.Api
- src/Market.Application
- src/Market.Domain
- src/Market.Infrastructure
- tests/...

Or a similarly sensible structure.

Use patterns pragmatically, not over-engineered.
Prefer a service layer for the trade engine and order management logic.

Recommended services:
- TraderService
- AssetTypeService
- SellOrderService
- QuoteService
- TradeExecutionService
- LedgerService
- MarketAnalyticsService

## API design expectations

Use REST endpoints with clear DTOs.

Include proper:
- status codes
- problem details
- validation errors
- pagination on list endpoints where needed
- filtering and sorting where useful

## Business logic details to implement

### Sell order creation
When creating a sell order:
- verify seller exists
- verify seller has sufficient available asset quantity
- reserve quantity
- create order
- log ledger entries

### Sell order quantity update
When increasing quantity:
- verify additional available inventory exists
- increase reserved quantity

When decreasing quantity:
- release unused reserved quantity

If quantity becomes equal to already-filled quantity, handle correctly.
If update would make order invalid, reject it.

### Trade quote calculation
To quote a purchase:
- gather open sell orders for the asset ordered by price asc, created_at asc
- walk through them until requested quantity is satisfied or inventory runs out
- compute:
  - total cost
  - weighted average price
  - max unit price encountered
  - list of fills
- if insufficient quantity, return unfillable response

### Trade execution algorithm
Implement this carefully:
- begin transaction
- lock relevant rows
- re-read available order book inside transaction
- compute fills again inside transaction, do not trust prior quote
- verify enough quantity still exists
- verify buyer credits still sufficient
- for each fill:
  - decrement order remaining quantity
  - if zero, mark completed
  - move sold asset from seller reserved inventory out of seller ownership
  - credit seller credits
  - create trade fill
  - create asset/credit transfer records
  - create ledger entries
- debit buyer credits for total
- add purchased asset quantity to buyer inventory
- create trade record
- commit transaction

Be precise about order of operations and consistency.

## Ledger design expectation

I strongly prefer immutable ledger entries.

Each ledger entry should capture enough metadata to understand why a balance changed.

Possible ledger entry categories:
- CreditAdjustment
- AssetAdjustment
- SellOrderCreatedReservation
- SellOrderReservationReleased
- TradeBuyerCreditDebit
- TradeSellerCreditCredit
- TradeBuyerAssetCredit
- TradeSellerAssetDebit
- OrderCompleted
- OrderCancelled

Each ledger entry should include fields like:
- id
- trader id
- asset type id nullable where not relevant
- trade id nullable
- sell order id nullable
- entry type
- quantity delta nullable
- credit delta nullable
- balance after if you choose to store it
- metadata/json payload if useful
- created at

## Analytics expectations

Provide queries and endpoints to support:
- current best ask
- current open volume
- depth grouped by price level
- recent trades
- average trade price over time range
- VWAP over time range
- volume over time range

Use trade fills or trades as the basis, and be explicit which one you use.

If useful, implement SQL views or efficient LINQ/SQL queries.
If you think some analytics should be precomputed later, mention that in README, but still implement direct query versions now.

## Seed/demo scenario

Seed sample data such as:
- a few asset types like grain, iron, wood
- several traders with credits
- initial asset balances
- several open sell orders at different prices

So the API can be tested immediately.

## Testing expectations

Add unit tests for:
- quote calculation
- sell order creation validation
- sell order update logic
- trade execution logic
- insufficient inventory
- insufficient buyer credits
- FIFO behavior on same-price orders
- cancel order releasing reserved inventory

If integration tests are feasible:
- test end-to-end execute purchase
- test concurrent purchase attempt behavior if practical

## README expectations

Include:
- architecture overview
- domain model explanation
- setup instructions
- docker compose instructions
- migration commands
- how trade execution works
- concurrency strategy
- endpoint summary
- sample curl examples
- assumptions and limitations

## Important assumptions to make explicit

If something is not specified, choose sensible defaults and document them.

Examples of defaults you may choose:
- quantities can be decimals or integers
- soft delete vs hard delete
- whether traders can buy their own sell orders
- whether credits are stored in a dedicated balance table or directly on trader
- whether sell order price can be changed after partial fill

## Nice-to-have extras if not too much complexity

If practical, also include:
- idempotency support for execute purchase endpoint
- correlation ids / request ids in logging
- outbox-ready design notes
- domain events
- event timestamps in UTC only
- simple rate limiting notes
- basic authorization placeholder
- health check endpoint

## Deliverable style

Do not just describe what you would build.
Actually generate the code and files.

Start by:
1. Proposing the solution structure briefly
2. Defining the domain model
3. Implementing the code file by file

Be pragmatic and choose a design that is correct, readable, and maintainable.
Prioritize correctness of trade execution and audit logging over fancy architecture.

## Additional info

I have created a local dev postgrest db for you to use.
It is located on localhost port 5432, it's called 'stocks_dev', the username is 'service_user' and the password is 'Dwarf1234'.
Feel free to add this as a connection string in the project.