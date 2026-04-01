using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Market.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "asset_transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_trader_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_trader_id = table.Column<Guid>(type: "uuid", nullable: true),
                    asset_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    trade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sell_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_transfers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "asset_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    unit_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "credit_transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_trader_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_trader_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    trade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credit_transfers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ledger_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sell_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entry_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity_delta = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    credit_delta = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    metadata = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "traders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_traders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sell_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    remaining_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sell_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_sell_orders_asset_types_asset_type_id",
                        column: x => x.asset_type_id,
                        principalTable: "asset_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sell_orders_traders_trader_id",
                        column: x => x.trader_id,
                        principalTable: "traders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trader_asset_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    reserved_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trader_asset_balances", x => x.id);
                    table.ForeignKey(
                        name: "fk_trader_asset_balances_asset_types_asset_type_id",
                        column: x => x.asset_type_id,
                        principalTable: "asset_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_trader_asset_balances_traders_trader_id",
                        column: x => x.trader_id,
                        principalTable: "traders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trader_credit_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credits = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trader_credit_balances", x => x.id);
                    table.ForeignKey(
                        name: "fk_trader_credit_balances_traders_trader_id",
                        column: x => x.trader_id,
                        principalTable: "traders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trades",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_trader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    total_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    average_unit_price = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trades", x => x.id);
                    table.ForeignKey(
                        name: "fk_trades_asset_types_asset_type_id",
                        column: x => x.asset_type_id,
                        principalTable: "asset_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_trades_traders_buyer_trader_id",
                        column: x => x.buyer_trader_id,
                        principalTable: "traders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trade_fills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trade_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sell_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_trader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    sub_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trade_fills", x => x.id);
                    table.ForeignKey(
                        name: "fk_trade_fills_sell_orders_sell_order_id",
                        column: x => x.sell_order_id,
                        principalTable: "sell_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_trade_fills_trades_trade_id",
                        column: x => x.trade_id,
                        principalTable: "trades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asset_transfers_asset_created",
                table: "asset_transfers",
                columns: new[] { "asset_type_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_transfers_trade_id",
                table: "asset_transfers",
                column: "trade_id",
                filter: "trade_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_asset_types_code",
                table: "asset_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_credit_transfers_created",
                table: "credit_transfers",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_credit_transfers_trade_id",
                table: "credit_transfers",
                column: "trade_id",
                filter: "trade_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_sell_order_id",
                table: "ledger_entries",
                column: "sell_order_id",
                filter: "sell_order_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_trade_id",
                table: "ledger_entries",
                column: "trade_id",
                filter: "trade_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_trader_created",
                table: "ledger_entries",
                columns: new[] { "trader_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sell_orders_asset_status_price_created",
                table: "sell_orders",
                columns: new[] { "asset_type_id", "status", "unit_price", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sell_orders_trader_id",
                table: "sell_orders",
                column: "trader_id");

            migrationBuilder.CreateIndex(
                name: "ix_trade_fills_asset_executed",
                table: "trade_fills",
                columns: new[] { "asset_type_id", "executed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_trade_fills_sell_order_id",
                table: "trade_fills",
                column: "sell_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_trade_fills_trade_id",
                table: "trade_fills",
                column: "trade_id");

            migrationBuilder.CreateIndex(
                name: "ix_trader_asset_balances_asset_type_id",
                table: "trader_asset_balances",
                column: "asset_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_trader_asset_balances_trader_asset",
                table: "trader_asset_balances",
                columns: new[] { "trader_id", "asset_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trader_credit_balances_trader_id",
                table: "trader_credit_balances",
                column: "trader_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_traders_name",
                table: "traders",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_trades_asset_type_id",
                table: "trades",
                column: "asset_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_trades_buyer_executed",
                table: "trades",
                columns: new[] { "buyer_trader_id", "executed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_trades_idempotency_key",
                table: "trades",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_transfers");

            migrationBuilder.DropTable(
                name: "credit_transfers");

            migrationBuilder.DropTable(
                name: "ledger_entries");

            migrationBuilder.DropTable(
                name: "trade_fills");

            migrationBuilder.DropTable(
                name: "trader_asset_balances");

            migrationBuilder.DropTable(
                name: "trader_credit_balances");

            migrationBuilder.DropTable(
                name: "sell_orders");

            migrationBuilder.DropTable(
                name: "trades");

            migrationBuilder.DropTable(
                name: "asset_types");

            migrationBuilder.DropTable(
                name: "traders");
        }
    }
}
