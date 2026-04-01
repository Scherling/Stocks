using Market.Application.DTOs.Requests;
using Market.Application.DTOs.Responses;
using Market.Application.Exceptions;
using Market.Application.Interfaces;
using Market.Domain.Entities;
using Market.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Market.Application.Services;

public class TraderService(IMarketDbContext db) : ITraderService
{
    public async Task<PaginatedResponse<TraderResponse>> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Traders.OrderBy(t => t.Name);
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => MapToResponse(t))
            .ToListAsync(ct);

        return new PaginatedResponse<TraderResponse>(items, total, page, pageSize);
    }

    public async Task<TraderResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var trader = await db.Traders.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Trader), id);
        return MapToResponse(trader);
    }

    public async Task<TraderResponse> CreateAsync(CreateTraderRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Trader name cannot be empty.");

        var now = DateTime.UtcNow;
        var trader = new Trader
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Status = TraderStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        var creditBalance = new TraderCreditBalance
        {
            Id = Guid.NewGuid(),
            TraderId = trader.Id,
            Credits = 0m
        };

        db.Traders.Add(trader);
        db.TraderCreditBalances.Add(creditBalance);
        await db.SaveChangesAsync(ct);

        return MapToResponse(trader);
    }

    public async Task<TraderResponse> UpdateAsync(Guid id, UpdateTraderRequest request, CancellationToken ct = default)
    {
        var trader = await db.Traders.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Trader), id);

        if (trader.Status == TraderStatus.Inactive)
            throw new ValidationException("Cannot update an inactive trader.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Trader name cannot be empty.");

        trader.Name = request.Name.Trim();
        trader.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return MapToResponse(trader);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var trader = await db.Traders.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Trader), id);

        trader.Status = TraderStatus.Inactive;
        trader.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<TraderBalancesResponse> GetBalancesAsync(Guid id, CancellationToken ct = default)
    {
        var trader = await db.Traders.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Trader), id);

        var credit = await db.TraderCreditBalances
            .FirstOrDefaultAsync(c => c.TraderId == id, ct);

        var assetBalances = await db.TraderAssetBalances
            .Where(b => b.TraderId == id)
            .Join(db.AssetTypes, b => b.AssetTypeId, a => a.Id,
                (b, a) => new AssetBalanceResponse(
                    b.AssetTypeId, a.Slug, a.Name,
                    b.TotalQuantity, b.ReservedQuantity, b.TotalQuantity - b.ReservedQuantity))
            .ToListAsync(ct);

        return new TraderBalancesResponse(id, credit?.Credits ?? 0m, assetBalances);
    }

    public async Task AdjustCreditsAsync(Guid id, AdjustCreditsRequest request, CancellationToken ct = default)
    {
        var trader = await db.Traders.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Trader), id);

        var credit = await db.TraderCreditBalances
            .FirstOrDefaultAsync(c => c.TraderId == id, ct);

        if (credit is null)
        {
            credit = new TraderCreditBalance { Id = Guid.NewGuid(), TraderId = id, Credits = 0m };
            db.TraderCreditBalances.Add(credit);
        }

        if (credit.Credits + request.Amount < 0)
            throw new ValidationException("Credit adjustment would result in a negative balance.");

        credit.Credits += request.Amount;

        var ledger = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            TraderId = id,
            EntryType = LedgerEntryType.CreditAdjustment,
            CreditDelta = request.Amount,
            Metadata = request.Reason,
            CreatedAt = DateTime.UtcNow
        };
        db.LedgerEntries.Add(ledger);

        await db.SaveChangesAsync(ct);
    }

    public async Task AdjustAssetBalanceAsync(Guid id, AdjustAssetBalanceRequest request, CancellationToken ct = default)
    {
        var trader = await db.Traders.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Trader), id);

        var assetType = await db.AssetTypes.FindAsync([request.AssetTypeId], ct)
            ?? throw new NotFoundException(nameof(AssetType), request.AssetTypeId);

        var balance = await db.TraderAssetBalances
            .FirstOrDefaultAsync(b => b.TraderId == id && b.AssetTypeId == request.AssetTypeId, ct);

        if (balance is null)
        {
            if (request.Amount < 0)
                throw new ValidationException("Cannot reduce a non-existent asset balance.");

            balance = new TraderAssetBalance
            {
                Id = Guid.NewGuid(),
                TraderId = id,
                AssetTypeId = request.AssetTypeId,
                TotalQuantity = 0m,
                ReservedQuantity = 0m
            };
            db.TraderAssetBalances.Add(balance);
        }

        if (balance.TotalQuantity + request.Amount < 0)
            throw new ValidationException("Asset adjustment would result in a negative total balance.");

        if (balance.AvailableQuantity + request.Amount < 0)
            throw new ValidationException("Asset adjustment would reduce available quantity below zero (some quantity is reserved in active orders).");

        balance.TotalQuantity += request.Amount;

        var now = DateTime.UtcNow;
        var ledger = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            TraderId = id,
            AssetTypeId = request.AssetTypeId,
            EntryType = LedgerEntryType.AssetAdjustment,
            QuantityDelta = request.Amount,
            Metadata = request.Reason,
            CreatedAt = now
        };
        var transfer = new AssetTransfer
        {
            Id = Guid.NewGuid(),
            ToTraderId = request.Amount > 0 ? id : null,
            FromTraderId = request.Amount < 0 ? id : null,
            AssetTypeId = request.AssetTypeId,
            Quantity = Math.Abs(request.Amount),
            CreatedAt = now
        };
        db.LedgerEntries.Add(ledger);
        db.AssetTransfers.Add(transfer);

        await db.SaveChangesAsync(ct);
    }

    private static TraderResponse MapToResponse(Trader t) =>
        new(t.Id, t.Name, t.Status, t.CreatedAt, t.UpdatedAt);
}
