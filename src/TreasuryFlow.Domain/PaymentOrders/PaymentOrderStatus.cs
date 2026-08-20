namespace TreasuryFlow.Domain.PaymentOrders;

public enum PaymentOrderStatus
{
    Draft = 1,
    Pending = 2,
    Processing = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6
}