using FluentAssertions;
using Market.Application.DTOs.Requests;
using Market.Application.Exceptions;
using Market.Application.Services;
using Market.Domain.Entities;
using Market.Domain.Enums;
using Market.Tests.Unit.Helpers;
using Xunit;

namespace Market.Tests.Unit.Services;

public class QuoteServiceTests
{
    private static (TestMarketDbContext db, QuoteService svc) Setup()
    {
        var db = TestDbContextFactory.Create();
        return (db, new QuoteService(db));
    }

    [Fact]
    public async Task GetQuote_ExactFill_ReturnsFillableWithCorrectCost()
    {
        var (db, svc) = Setup();
        var assetType = new AssetType { Id = Guid.NewGuid(), Code = "IRON", Name = "Iron", UnitName = "ingot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var seller = new Trader { Id = Guid.NewGuid(), Name = "Seller", Status = TraderStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.AssetTypes.Add(assetType);
        db.Traders.Add(seller);
        db.SellOrders.Add(new SellOrder
        {
            Id = Guid.NewGuid(), TraderId = seller.Id, AssetTypeId = assetType.Id,
            OriginalQuantity = 100m, RemainingQuantity = 100m, UnitPrice = 10m,
            Status = SellOrderStatus.Open, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await svc.GetQuoteAsync(new QuoteRequest(assetType.Id, 50m), default);

        result.IsFillable.Should().BeTrue();
        result.TotalCost.Should().Be(500m);
        result.AverageUnitPrice.Should().Be(10m);
        result.AvailableQuantity.Should().Be(100m);
        result.FillPreview.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetQuote_InsufficientQuantity_ReturnsUnfillable()
    {
        var (db, svc) = Setup();
        var assetType = new AssetType { Id = Guid.NewGuid(), Code = "IRON", Name = "Iron", UnitName = "ingot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var seller = new Trader { Id = Guid.NewGuid(), Name = "Seller", Status = TraderStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.AssetTypes.Add(assetType);
        db.Traders.Add(seller);
        db.SellOrders.Add(new SellOrder
        {
            Id = Guid.NewGuid(), TraderId = seller.Id, AssetTypeId = assetType.Id,
            OriginalQuantity = 10m, RemainingQuantity = 10m, UnitPrice = 5m,
            Status = SellOrderStatus.Open, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await svc.GetQuoteAsync(new QuoteRequest(assetType.Id, 100m), default);

        result.IsFillable.Should().BeFalse();
        result.TotalCost.Should().BeNull();
        result.AvailableQuantity.Should().Be(10m);
    }

    [Fact]
    public async Task GetQuote_FifoAtSamePrice_UsesCreationOrder()
    {
        var (db, svc) = Setup();
        var assetType = new AssetType { Id = Guid.NewGuid(), Code = "GRAIN", Name = "Grain", UnitName = "bushel", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var seller = new Trader { Id = Guid.NewGuid(), Name = "Seller", Status = TraderStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.AssetTypes.Add(assetType);
        db.Traders.Add(seller);

        var t1 = DateTime.UtcNow;
        var t2 = t1.AddSeconds(1);

        var order1 = new SellOrder { Id = Guid.NewGuid(), TraderId = seller.Id, AssetTypeId = assetType.Id, OriginalQuantity = 50m, RemainingQuantity = 50m, UnitPrice = 5m, Status = SellOrderStatus.Open, CreatedAt = t1, UpdatedAt = t1 };
        var order2 = new SellOrder { Id = Guid.NewGuid(), TraderId = seller.Id, AssetTypeId = assetType.Id, OriginalQuantity = 50m, RemainingQuantity = 50m, UnitPrice = 5m, Status = SellOrderStatus.Open, CreatedAt = t2, UpdatedAt = t2 };
        db.SellOrders.AddRange(order1, order2);
        await db.SaveChangesAsync();

        var result = await svc.GetQuoteAsync(new QuoteRequest(assetType.Id, 60m), default);

        result.IsFillable.Should().BeTrue();
        result.FillPreview[0].SellOrderId.Should().Be(order1.Id); // Oldest first (FIFO)
        result.FillPreview[0].Quantity.Should().Be(50m);
        result.FillPreview[1].SellOrderId.Should().Be(order2.Id);
        result.FillPreview[1].Quantity.Should().Be(10m);
    }

    [Fact]
    public async Task GetQuote_WithBuyerTraderId_ExcludesBuyerOwnOrders()
    {
        var (db, svc) = Setup();
        var assetType = new AssetType { Id = Guid.NewGuid(), Code = "WOOD", Name = "Wood", UnitName = "plank", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var buyer = new Trader { Id = Guid.NewGuid(), Name = "Buyer", Status = TraderStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.AssetTypes.Add(assetType);
        db.Traders.Add(buyer);
        // Only order belongs to the buyer themselves
        db.SellOrders.Add(new SellOrder
        {
            Id = Guid.NewGuid(), TraderId = buyer.Id, AssetTypeId = assetType.Id,
            OriginalQuantity = 100m, RemainingQuantity = 100m, UnitPrice = 3m,
            Status = SellOrderStatus.Open, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await svc.GetQuoteAsync(new QuoteRequest(assetType.Id, 10m, buyer.Id), default);

        result.IsFillable.Should().BeFalse(); // Buyer's own order excluded
        result.AvailableQuantity.Should().Be(0m);
    }

    [Fact]
    public async Task GetQuote_SpanningMultipleOrders_ComputesCorrectAveragePrice()
    {
        var (db, svc) = Setup();
        var assetType = new AssetType { Id = Guid.NewGuid(), Code = "GRAIN", Name = "Grain", UnitName = "bushel", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var seller = new Trader { Id = Guid.NewGuid(), Name = "Seller", Status = TraderStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.AssetTypes.Add(assetType);
        db.Traders.Add(seller);
        db.SellOrders.AddRange(
            new SellOrder { Id = Guid.NewGuid(), TraderId = seller.Id, AssetTypeId = assetType.Id, OriginalQuantity = 50m, RemainingQuantity = 50m, UnitPrice = 4m, Status = SellOrderStatus.Open, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new SellOrder { Id = Guid.NewGuid(), TraderId = seller.Id, AssetTypeId = assetType.Id, OriginalQuantity = 50m, RemainingQuantity = 50m, UnitPrice = 6m, Status = SellOrderStatus.Open, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await svc.GetQuoteAsync(new QuoteRequest(assetType.Id, 100m), default);

        result.IsFillable.Should().BeTrue();
        result.TotalCost.Should().Be(500m); // 50*4 + 50*6 = 200+300
        result.AverageUnitPrice.Should().Be(5m); // (200+300)/100
        result.MinUnitPrice.Should().Be(4m);
        result.MaxUnitPrice.Should().Be(6m);
        result.OrdersConsumed.Should().Be(2);
    }
}
