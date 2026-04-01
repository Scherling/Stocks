using FluentAssertions;
using Market.Application.DTOs.Requests;
using Market.Application.Exceptions;
using Market.Application.Services;
using Market.Domain.Entities;
using Market.Domain.Enums;
using Market.Tests.Unit.Helpers;
using Xunit;

namespace Market.Tests.Unit.Services;

public class SellOrderServiceTests
{
    private static (TestMarketDbContext db, SellOrderService svc, Trader trader, AssetType assetType) SetupBase()
    {
        var db = TestDbContextFactory.Create();
        var trader = new Trader { Id = Guid.NewGuid(), Name = "Alice", Status = TraderStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var assetType = new AssetType { Id = Guid.NewGuid(), Code = "IRON", Name = "Iron", UnitName = "ingot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Traders.Add(trader);
        db.AssetTypes.Add(assetType);
        db.TraderAssetBalances.Add(new TraderAssetBalance { Id = Guid.NewGuid(), TraderId = trader.Id, AssetTypeId = assetType.Id, TotalQuantity = 500m, ReservedQuantity = 0m });
        db.TraderCreditBalances.Add(new TraderCreditBalance { Id = Guid.NewGuid(), TraderId = trader.Id, Credits = 1000m });
        db.SaveChanges();
        return (db, new SellOrderService(db), trader, assetType);
    }

    [Fact]
    public async Task Create_ReservesInventoryOnSuccess()
    {
        var (db, svc, trader, assetType) = SetupBase();

        await svc.CreateAsync(new CreateSellOrderRequest(trader.Id, assetType.Id, 100m, 5m));

        var balance = db.TraderAssetBalances.First(b => b.TraderId == trader.Id);
        balance.ReservedQuantity.Should().Be(100m);
        balance.AvailableQuantity.Should().Be(400m);
    }

    [Fact]
    public async Task Create_InsufficientBalance_Throws()
    {
        var (db, svc, trader, assetType) = SetupBase();

        var act = async () => await svc.CreateAsync(new CreateSellOrderRequest(trader.Id, assetType.Id, 600m, 5m));

        await act.Should().ThrowAsync<InsufficientInventoryException>();
    }

    [Fact]
    public async Task Cancel_ReleasesReservedQuantity()
    {
        var (db, svc, trader, assetType) = SetupBase();
        var order = await svc.CreateAsync(new CreateSellOrderRequest(trader.Id, assetType.Id, 100m, 5m));

        await svc.CancelAsync(order.Id);

        var balance = db.TraderAssetBalances.First(b => b.TraderId == trader.Id);
        balance.ReservedQuantity.Should().Be(0m);
        balance.AvailableQuantity.Should().Be(500m);

        var cancelledOrder = db.SellOrders.First(o => o.Id == order.Id);
        cancelledOrder.Status.Should().Be(SellOrderStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_CompletedOrder_Throws()
    {
        var (db, svc, trader, assetType) = SetupBase();
        var now = DateTime.UtcNow;
        var completedOrder = new SellOrder
        {
            Id = Guid.NewGuid(), TraderId = trader.Id, AssetTypeId = assetType.Id,
            OriginalQuantity = 100m, RemainingQuantity = 0m, UnitPrice = 5m,
            Status = SellOrderStatus.Completed, CreatedAt = now, UpdatedAt = now
        };
        db.SellOrders.Add(completedOrder);
        await db.SaveChangesAsync();

        var act = async () => await svc.CancelAsync(completedOrder.Id);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*completed*");
    }

    [Fact]
    public async Task Update_IncreaseQuantity_ReservesAdditional()
    {
        var (db, svc, trader, assetType) = SetupBase();
        var order = await svc.CreateAsync(new CreateSellOrderRequest(trader.Id, assetType.Id, 100m, 5m));

        await svc.UpdateAsync(order.Id, new UpdateSellOrderRequest(null, 150m));

        var balance = db.TraderAssetBalances.First(b => b.TraderId == trader.Id);
        balance.ReservedQuantity.Should().Be(150m);
        balance.AvailableQuantity.Should().Be(350m);
    }

    [Fact]
    public async Task Update_DecreaseQuantity_ReleasesReservation()
    {
        var (db, svc, trader, assetType) = SetupBase();
        var order = await svc.CreateAsync(new CreateSellOrderRequest(trader.Id, assetType.Id, 100m, 5m));

        await svc.UpdateAsync(order.Id, new UpdateSellOrderRequest(null, 60m));

        var balance = db.TraderAssetBalances.First(b => b.TraderId == trader.Id);
        balance.ReservedQuantity.Should().Be(60m);
        balance.AvailableQuantity.Should().Be(440m);
    }

    [Fact]
    public async Task Create_InactiveTrader_Throws()
    {
        var db = TestDbContextFactory.Create();
        var trader = new Trader { Id = Guid.NewGuid(), Name = "Inactive", Status = TraderStatus.Inactive, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var assetType = new AssetType { Id = Guid.NewGuid(), Code = "WOOD", Name = "Wood", UnitName = "plank", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Traders.Add(trader);
        db.AssetTypes.Add(assetType);
        await db.SaveChangesAsync();
        var svc = new SellOrderService(db);

        var act = async () => await svc.CreateAsync(new CreateSellOrderRequest(trader.Id, assetType.Id, 10m, 1m));

        await act.Should().ThrowAsync<ValidationException>();
    }
}
