using FluentAssertions;
using Market.Application.DTOs.Requests;
using Market.Application.Exceptions;
using Market.Application.Services;
using Market.Domain.Entities;
using Market.Domain.Enums;
using Market.Tests.Unit.Helpers;
using Xunit;

namespace Market.Tests.Unit.Services;

public class TraderServiceTests
{
    [Fact]
    public async Task Create_SetsActiveStatusAndCreatesZeroCreditBalance()
    {
        var db = TestDbContextFactory.Create();
        var svc = new TraderService(db);

        var result = await svc.CreateAsync(new CreateTraderRequest("TestTrader"));

        result.Status.Should().Be(TraderStatus.Active);
        db.TraderCreditBalances.Should().HaveCount(1);
        db.TraderCreditBalances.First().Credits.Should().Be(0m);
    }

    [Fact]
    public async Task Delete_SetsStatusToInactive()
    {
        var db = TestDbContextFactory.Create();
        var svc = new TraderService(db);
        var created = await svc.CreateAsync(new CreateTraderRequest("Alice"));

        await svc.DeleteAsync(created.Id);

        var trader = db.Traders.First();
        trader.Status.Should().Be(TraderStatus.Inactive);
    }

    [Fact]
    public async Task Delete_NonExistentTrader_Throws()
    {
        var db = TestDbContextFactory.Create();
        var svc = new TraderService(db);

        var act = async () => await svc.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AdjustCredits_PositiveAmount_IncreasesBalance()
    {
        var db = TestDbContextFactory.Create();
        var svc = new TraderService(db);
        var trader = await svc.CreateAsync(new CreateTraderRequest("Bob"));

        await svc.AdjustCreditsAsync(trader.Id, new AdjustCreditsRequest(500m, "Initial funding"));

        var credit = db.TraderCreditBalances.First();
        credit.Credits.Should().Be(500m);
    }

    [Fact]
    public async Task AdjustCredits_WouldGoNegative_Throws()
    {
        var db = TestDbContextFactory.Create();
        var svc = new TraderService(db);
        var trader = await svc.CreateAsync(new CreateTraderRequest("Carol"));

        var act = async () => await svc.AdjustCreditsAsync(trader.Id, new AdjustCreditsRequest(-100m, "Debit"));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*negative*");
    }

    [Fact]
    public async Task AdjustAssetBalance_FirstTimeBalance_CreatesRow()
    {
        var db = TestDbContextFactory.Create();
        var assetType = new AssetType { Id = Guid.NewGuid(), Code = "IRON", Name = "Iron", UnitName = "ingot", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.AssetTypes.Add(assetType);
        await db.SaveChangesAsync();

        var svc = new TraderService(db);
        var trader = await svc.CreateAsync(new CreateTraderRequest("Dave"));

        await svc.AdjustAssetBalanceAsync(trader.Id, new AdjustAssetBalanceRequest(assetType.Id, 100m, "Grant"));

        db.TraderAssetBalances.Should().HaveCount(1);
        db.TraderAssetBalances.First().TotalQuantity.Should().Be(100m);
    }
}
