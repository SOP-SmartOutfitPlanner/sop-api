using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface IUserSuspensionRepository : IGenericRepository<UserSuspension>
    {
        Task<UserSuspension?> GetActiveSuspensionAsync(long userId);
        Task<List<UserSuspension>> GetSuspensionHistoryAsync(long userId);
    }
}
