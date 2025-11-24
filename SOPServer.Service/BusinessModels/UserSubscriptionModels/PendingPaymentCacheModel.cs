namespace SOPServer.Service.BusinessModels.UserSubscriptionModels
{
    public class PendingPaymentCacheModel
    {
        public string QrCode { get; set; }
        public string PaymentUrl { get; set; }
        public decimal Amount { get; set; }
        public string SubscriptionPlanName { get; set; }
        public long UserSubscriptionId { get; set; }
        public long TransactionId { get; set; }
        public string Description { get; set; }
        public long? ExpiredAt { get; set; }
        public BankInfoModel BankInfo { get; set; }
    }
}
