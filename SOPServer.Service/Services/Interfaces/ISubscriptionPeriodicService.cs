namespace SOPServer.Service.Services.Interfaces
{
    public interface ISubscriptionPeriodicService
    {
        Task CheckAndDeactivateExpiredSubscriptionsAsync();
    }
}
