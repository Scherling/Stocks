using FluentAssertions;
using Market.Application.DTOs.Requests;
using Market.Application.Exceptions;
using Market.Application.Services;
using Market.Domain.Entities;
using Market.Domain.Enums;
using Market.Tests.Unit.Helpers;
using Xunit;

namespace Market.Tests.Unit.Services;

public class TradeExecutionServiceTests
{
    private record TestFixture(
        TestMarketDbContext Db,
        TradeExecutionService Svc,
        Trader Buyer,
        Trader Seller,
        AssetType AssetType);

    private static async Task<TestFixture> SetupAsync(
        decimal sellerInventory = 200m,
        decimal buyerCredits = 10_000m,
        decimal sellPrice = 10m,
        decimal sellQty = 100m)
    {
        var db = TestDbContextFactory.Create();
        var buyer = new Trader { Id = Guid.NewGuid(), Name = "Buyer", Status = TraderStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var seller = new Trader { Id = Guid.NewGuid(), Name = "Seller", Status = TraderStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var assetType = new AssetType { Id = Guid.NewGuid(), Code = "IRON", Name = "Iron", UnitName = "ingot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Traders.AddRange(buyer, seller);
        db.AssetTypes.Add(assetType);
        db.TraderCreditBalances.AddRange(
            new TraderCreditBalance { Id = Guid.NewGuid(), TraderId = buyer.Id, Credits = buyerCredits },
            new TraderCreditBalance { Id = Guid.NewGuid(), TraderId = seller.Id, Credits = 0m });
        db.TraderAssetBalances.Add(new TraderAssetBalance
        {
            Id = Guid.NewGuid(), TraderId = seller.Id, AssetTypeId = assetType.Id,
            TotalQuantity = sellerInventory, ReservedQuantity = sellQty
        });
        db.SellOrders.Add(new SellOrder
        {
            Id = Guid.NewGuid(), TraderId = seller.Id, AssetTypeId = assetType.Id,
            OriginalQuantity = sellQty, RemainingQuantity = sellQty, UnitPrice = sellPrice,
            Status = SellOrderStatus.Open, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return new TestFixture(db, new TradeExecutionService(db), buyer, seller, assetType);
    }

    [Fact]
    public async Task Execute_DebitsExactBuyerCredits()
    {
        var f = await SetupAsync(buyerCredits: 1000m, sellPrice: 10m, sellQty: 50m);

        await f.Svc.ExecuteAsync(new ExecuteTradeRequest(f.Buyer.Id, f.AssetType.Id, 50m));

        var credit = f.Db.TraderCreditBalances.First(c => c.TraderId == f.Buyer.Id);
        credit.Credits.Should().Be(500m); // 1000 - 50*10
    }

    [Fact]
    public async Task Execute_CreditsSeller()
    {
        var f = await SetupAsync(sellPrice: 12m, sellQty: 100m, buyerCredits: 5000m);

        await f.Svc.ExecuteAsync(new ExecuteTradeRequest(f.Buyer.Id, f.AssetType.Id, 50m));

        var sellerCredit = f.Db.TraderCreditBalances.First(c => c.TraderId == f.Seller.Id);
        sellerCredit.Credits.Should().Be(600m); // 50 * 12
    }

    [Fact]
    public async Task Execute_DecreasesSellerAssetBalance()
    {
        var f = await SetupAsync(sellerInventory: 100m, sellQty: 100m, sellPrice: 5m, buyerCredits: 5000m);

        await f.Svc.ExecuteAsync(new ExecuteTradeRequest(f.Buyer.Id, f.AssetType.Id, 60m));

        var sellerBalance = f.Db.TraderAssetBalances.First(b => b.TraderId == f.Seller.Id);
        sellerBalance.TotalQuantity.Should().Be(40m);  // 100 - 60
        sellerBalance.ReservedQuantity.Should().Be(40m); // 100 - 60
    }

    [Fact]
    public async Task Execute_IncreasesBuyerAssetBalance()
    {
        var f = await SetupAsync(sellPrice: 5m, sellQty: 100m, buyerCredits: 5000m);

        await f.Svc.ExecuteAsync(new ExecuteTradeRequest(f.Buyer.Id, f.AssetType.Id, 80m));

        var buyerBalance = f.Db.TraderAssetBalances.First(b => b.TraderId == f.Buyer.Id && b.AssetTypeId == f.AssetType.Id);
        buyerBalance.TotalQuantity.Should().Be(80m);
    }

    [Fact]
    public async Task Execute_InsufficientCredits_Throws()
    {
        var f = await SetupAsync(sellPrice: 10m, sellQty: 100m, buyerCredits: 50m); // Only 50 credits, needs 1000

        var act = async () => await f.Svc.ExecuteAsync(new ExecuteTradeRequest(f.Buyer.Id, f.AssetType.Id, 100m));

        await act.Should().ThrowAsync<InsufficientCreditsException>();
    }

    [Fact]
    public async Task Execute_InsufficientInventory_Throws()
    {
        var f = await SetupAsync(sellQty: 10m); // Only 10 available

        var act = async () => await f.Svc.ExecuteAsync(new ExecuteTradeRequest(f.Buyer.Id, f.AssetType.Id, 100m));

        await act.Should().ThrowAsync<OrderNotFillableException>();
    }

    [Fact]
    public async Task Execute_BuyerIsSeller_Throws()
    {
        var db = TestDbContextFactory.Create();
        var trader = new Trader { Id = Guid.NewGuid(), Name = "Both", Status = TraderStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var assetType = new AssetType { Id = Guid.NewGuid(), Code = "WOOD", Name = "Wood", UnitName = "plank", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Traders.Add(trader);
        db.AssetTypes.Add(assetType);
        db.TraderCreditBalances.Add(new TraderCreditBalance { Id = Guid.NewGuid(), TraderId = trader.Id, Credits = 10_000m });
        db.TraderAssetBalances.Add(new TraderAssetBalance { Id = Guid.NewGuid(), TraderId = trader.Id, AssetTypeId = assetType.Id, TotalQuantity = 100m, ReservedQuantity = 100m });
        db.SellOrders.Add(new SellOrder
        {
            Id = Guid.NewGuid(), TraderId = trader.Id, AssetTypeId = assetType.Id,
            OriginalQuantity = 100m, RemainingQuantity = 100m, UnitPrice = 3m,
            Status = SellOrderStatus.Open, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var svc = new TradeExecutionService(db);
        var act = async () => await svc.ExecuteAsync(new ExecuteTradeRequest(trader.Id, assetType.Id, 10m));

        // Buyer's own order is excluded — no available fills
        await act.Should().ThrowAsync<OrderNotFillableException>();
    }

    [Fact]
    public async Task Execute_SpanningMultipleOrders_FillsInPriceOrder()
    {
        var db = TestDbContextFactory.Create();
        var buyer = new Trader { Id = Guid.NewGuid(), Name = "Buyer", Status = TraderStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var seller = new Trader { Id = Guid.NewGuid(), Name = "Seller", Status = TraderStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var assetType = new AssetType { Id = Guid.NewGuid(), Code = "GRAIN", Name = "Grain", UnitName = "bushel", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Traders.AddRange(buyer, seller);
        db.AssetTypes.Add(assetType);
        db.TraderCreditBalances.AddRange(
            new TraderCreditBalance { Id = Guid.NewGuid(), TraderId = buyer.Id, Credits = 10_000m },
            new TraderCreditBalance { Id = Guid.NewGuid(), TraderId = seller.Id, Credits = 0m });
        db.TraderAssetBalances.Add(new TraderAssetBalance { Id = Guid.NewGuid(), TraderId = seller.Id, AssetTypeId = assetType.Id, TotalQuantity = 200m, ReservedQuantity = 200m });
        // Two orders — expensive listed first but cheap should fill first
        db.SellOrders.AddRange(
            new SellOrder { Id = Guid.NewGuid(), TraderId = seller.Id, AssetTypeId = assetType.Id, OriginalQuantity = 100m, RemainingQuantity = 100m, UnitPrice = 20m, Status = SellOrderStatus.Open, CreatedAt = DateTime.UtcNow.AddSeconds(-1), UpdatedAt = DateTime.UtcNow },
            new SellOrder { Id = Guid.NewGuid(), TraderId = seller.Id, AssetTypeId = assetType.Id, OriginalQuantity = 100m, RemainingQuantity = 100m, UnitPrice = 10m, Status = SellOrderStatus.Open, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var svc = new TradeExecutionService(db);
        var result = await svc.ExecuteAsync(new ExecuteTradeRequest(buyer.Id, assetType.Id, 100m));

        result.TotalCost.Should().Be(1000m); // 100 * 10 (cheapest)
        result.AverageUnitPrice.Should().Be(10m);
    }

    [Fact]
    public async Task Execute_CreatesTradeAndFillRecords()
    {
        var f = await SetupAsync(sellPrice: 8m, sellQty: 50m, buyerCredits: 5000m);

        var result = await f.Svc.ExecuteAsync(new ExecuteTradeRequest(f.Buyer.Id, f.AssetType.Id, 30m));

        result.Fills.Should().HaveCount(1);
        result.Fills[0].Quantity.Should().Be(30m);
        result.Fills[0].UnitPrice.Should().Be(8m);
        f.Db.Trades.Should().HaveCount(1);
        f.Db.TradeFills.Should().HaveCount(1);
    }

    [Fact]
    public async Task Execute_CreatesLedgerEntries()
    {
        var f = await SetupAsync(sellPrice: 5m, sellQty: 100m, buyerCredits: 5000m);

        await f.Svc.ExecuteAsync(new ExecuteTradeRequest(f.Buyer.Id, f.AssetType.Id, 50m));

        f.Db.LedgerEntries.Should().HaveCountGreaterThanOrEqualTo(4); // At minimum: seller asset debit, seller credit, buyer credit debit, buyer asset credit
    }
}
