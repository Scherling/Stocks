using FluentAssertions;
using Market.Application.DTOs.Requests;
using Market.Application.Exceptions;
using Market.Application.Services;
using Market.Domain.Entities;
using Market.Domain.Enums;
using Market.Tests.Integration.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Market.Tests.Integration;

[Collection("PostgresIntegration")]
public class TradeExecutionIntegrationTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public TradeExecutionIntegrationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(Guid buyerId, Guid sellerId, Guid assetTypeId, Guid orderId)> SeedTestDataAsync()
    {
        await using var db = _fixture.CreateDbContext();
        var now = DateTime.UtcNow;

        var buyer = new Trader { Id = Guid.NewGuid(), Name = $"Buyer-{Guid.NewGuid()}", Status = TraderStatus.Active, CreatedAt = now, UpdatedAt = now };
        var seller = new Trader { Id = Guid.NewGuid(), Name = $"Seller-{Guid.NewGuid()}", Status = TraderStatus.Active, CreatedAt = now, UpdatedAt = now };
        var assetType = new AssetType { Id = Guid.NewGuid(), Code = $"TST{Guid.NewGuid():N}"[..6].ToUpper(), Name = "Test Asset", UnitName = "unit", IsActive = true, CreatedAt = now, UpdatedAt = now };

        db.Traders.AddRange(buyer, seller);
        db.AssetTypes.Add(assetType);
        db.TraderCreditBalances.AddRange(
            new TraderCreditBalance { Id = Guid.NewGuid(), TraderId = buyer.Id, Credits = 10_000m },
            new TraderCreditBalance { Id = Guid.NewGuid(), TraderId = seller.Id, Credits = 0m });
        db.TraderAssetBalances.Add(new TraderAssetBalance
        {
            Id = Guid.NewGuid(), TraderId = seller.Id, AssetTypeId = assetType.Id,
            TotalQuantity = 200m, ReservedQuantity = 100m
        });

        var order = new SellOrder
        {
            Id = Guid.NewGuid(), TraderId = seller.Id, AssetTypeId = assetType.Id,
            OriginalQuantity = 100m, RemainingQuantity = 100m, UnitPrice = 15m,
            Status = SellOrderStatus.Open, CreatedAt = now, UpdatedAt = now
        };
        db.SellOrders.Add(order);
        await db.SaveChangesAsync();

        return (buyer.Id, seller.Id, assetType.Id, order.Id);
    }

    [Fact]
    public async Task Execute_FullTrade_AllDbRowsCorrect()
    {
        var (buyerId, sellerId, assetTypeId, orderId) = await SeedTestDataAsync();

        await using var db = _fixture.CreateDbContext();
        var svc = new TradeExecutionService(db);

        var result = await svc.ExecuteAsync(new ExecuteTradeRequest(buyerId, assetTypeId, 50m));

        result.TotalCost.Should().Be(750m); // 50 * 15
        result.Fills.Should().HaveCount(1);

        // Verify DB state
        await using var verifyDb = _fixture.CreateDbContext();

        var trade = await verifyDb.Trades.Include(t => t.Fills).FirstAsync(t => t.Id == result.Id);
        trade.TotalQuantity.Should().Be(50m);
        trade.TotalCost.Should().Be(750m);
        trade.Fills.Should().HaveCount(1);

        var fill = trade.Fills.First();
        fill.Quantity.Should().Be(50m);
        fill.SubTotal.Should().Be(750m);

        var buyerCredit = await verifyDb.TraderCreditBalances.FirstAsync(c => c.TraderId == buyerId);
        buyerCredit.Credits.Should().Be(9250m); // 10000 - 750

        var sellerCredit = await verifyDb.TraderCreditBalances.FirstAsync(c => c.TraderId == sellerId);
        sellerCredit.Credits.Should().Be(750m);

        var sellerAsset = await verifyDb.TraderAssetBalances.FirstAsync(b => b.TraderId == sellerId);
        sellerAsset.TotalQuantity.Should().Be(150m); // 200 - 50
        sellerAsset.ReservedQuantity.Should().Be(50m); // 100 - 50

        var buyerAsset = await verifyDb.TraderAssetBalances.FirstAsync(b => b.TraderId == buyerId);
        buyerAsset.TotalQuantity.Should().Be(50m);

        var ledgerEntries = await verifyDb.LedgerEntries.Where(e => e.TradeId == trade.Id).ToListAsync();
        ledgerEntries.Should().HaveCountGreaterThanOrEqualTo(4);

        var assetTransfers = await verifyDb.AssetTransfers.Where(t => t.TradeId == trade.Id).ToListAsync();
        assetTransfers.Should().HaveCount(1);
        assetTransfers[0].Quantity.Should().Be(50m);

        var creditTransfers = await verifyDb.CreditTransfers.Where(t => t.TradeId == trade.Id).ToListAsync();
        creditTransfers.Should().HaveCount(2); // Seller receives + buyer pays
    }

    [Fact]
    public async Task Execute_IdempotencyKey_ReturnsSameTradeOnRetry()
    {
        var (buyerId, _, assetTypeId, _) = await SeedTestDataAsync();
        var idempotencyKey = Guid.NewGuid().ToString();

        await using var db1 = _fixture.CreateDbContext();
        var result1 = await new TradeExecutionService(db1).ExecuteAsync(
            new ExecuteTradeRequest(buyerId, assetTypeId, 10m, idempotencyKey));

        await using var db2 = _fixture.CreateDbContext();
        var result2 = await new TradeExecutionService(db2).ExecuteAsync(
            new ExecuteTradeRequest(buyerId, assetTypeId, 10m, idempotencyKey));

        result2.Id.Should().Be(result1.Id);
        result2.TotalCost.Should().Be(result1.TotalCost);

        // Only one trade should exist with this key
        await using var verifyDb = _fixture.CreateDbContext();
        var tradeCount = await verifyDb.Trades.CountAsync(t => t.IdempotencyKey == idempotencyKey);
        tradeCount.Should().Be(1);
    }
}

[Collection("PostgresIntegration")]
public class ConcurrentTradeTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public ConcurrentTradeTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConcurrentExecution_OnlyOneSucceeds_WhenInventoryExactlyCoversOne()
    {
        await using var setupDb = _fixture.CreateDbContext();
        var now = DateTime.UtcNow;

        var buyer1 = new Trader { Id = Guid.NewGuid(), Name = $"B1-{Guid.NewGuid()}", Status = TraderStatus.Active, CreatedAt = now, UpdatedAt = now };
        var buyer2 = new Trader { Id = Guid.NewGuid(), Name = $"B2-{Guid.NewGuid()}", Status = TraderStatus.Active, CreatedAt = now, UpdatedAt = now };
        var seller = new Trader { Id = Guid.NewGuid(), Name = $"S-{Guid.NewGuid()}", Status = TraderStatus.Active, CreatedAt = now, UpdatedAt = now };
        var assetType = new AssetType { Id = Guid.NewGuid(), Code = $"CNCA{Guid.NewGuid():N}"[..4].ToUpper(), Name = "Concurrent Asset", UnitName = "unit", IsActive = true, CreatedAt = now, UpdatedAt = now };

        setupDb.Traders.AddRange(buyer1, buyer2, seller);
        setupDb.AssetTypes.Add(assetType);
        setupDb.TraderCreditBalances.AddRange(
            new TraderCreditBalance { Id = Guid.NewGuid(), TraderId = buyer1.Id, Credits = 10_000m },
            new TraderCreditBalance { Id = Guid.NewGuid(), TraderId = buyer2.Id, Credits = 10_000m },
            new TraderCreditBalance { Id = Guid.NewGuid(), TraderId = seller.Id, Credits = 0m });
        setupDb.TraderAssetBalances.Add(new TraderAssetBalance
        {
            Id = Guid.NewGuid(), TraderId = seller.Id, AssetTypeId = assetType.Id,
            TotalQuantity = 50m, ReservedQuantity = 50m // Only 50 units available
        });
        setupDb.SellOrders.Add(new SellOrder
        {
            Id = Guid.NewGuid(), TraderId = seller.Id, AssetTypeId = assetType.Id,
            OriginalQuantity = 50m, RemainingQuantity = 50m, UnitPrice = 10m,
            Status = SellOrderStatus.Open, CreatedAt = now, UpdatedAt = now
        });
        await setupDb.SaveChangesAsync();

        // Launch two concurrent purchase requests for the full 50 units
        var task1 = Task.Run(async () =>
        {
            await using var db = _fixture.CreateDbContext();
            return await new TradeExecutionService(db).ExecuteAsync(
                new ExecuteTradeRequest(buyer1.Id, assetType.Id, 50m));
        });

        var task2 = Task.Run(async () =>
        {
            await using var db = _fixture.CreateDbContext();
            return await new TradeExecutionService(db).ExecuteAsync(
                new ExecuteTradeRequest(buyer2.Id, assetType.Id, 50m));
        });

        var results = await Task.WhenAll(
            task1.ContinueWith(t => (Success: !t.IsFaulted, Exception: t.Exception?.InnerException)),
            task2.ContinueWith(t => (Success: !t.IsFaulted, Exception: t.Exception?.InnerException)));

        var successCount = results.Count(r => r.Success);
        var failCount = results.Count(r => !r.Success);

        successCount.Should().Be(1, "exactly one trade should succeed when inventory covers only one");
        failCount.Should().Be(1, "the other trade should fail");
        results.Where(r => !r.Success).All(r => r.Exception is OrderNotFillableException).Should().BeTrue();
    }
}

[CollectionDefinition("PostgresIntegration")]
public class PostgresIntegrationCollection : ICollectionFixture<PostgresContainerFixture> { }
