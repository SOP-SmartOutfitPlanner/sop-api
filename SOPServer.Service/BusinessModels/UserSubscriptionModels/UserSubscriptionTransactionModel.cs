using SOPServer.Repository.Enums;

namespace SOPServer.Service.BusinessModels.UserSubscriptionModels
{
    public class UserSubscriptionTransactionModel
    {
        public long Id { get; set; }
        public long UserSubscriptionId { get; set; }
        public long UserId { get; set; }
        public int TransactionCode { get; set; }
        public decimal Price { get; set; }
        public TransactionStatus Status { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
