namespace Market.Domain.Enums;

public enum LedgerEntryType
{
    CreditAdjustment,
    AssetAdjustment,
    SellOrderCreatedReservation,
    SellOrderReservationReleased,
    TradeBuyerCreditDebit,
    TradeSellerCreditCredit,
    TradeBuyerAssetCredit,
    TradeSellerAssetDebit,
    OrderCompleted,
    OrderCancelled
}
