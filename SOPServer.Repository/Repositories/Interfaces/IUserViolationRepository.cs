using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface IUserViolationRepository : IGenericRepository<UserViolation>
    {
        Task<int> CountWarningsInPeriodAsync(long userId, DateTime since);
        Task<List<UserViolation>> GetViolationHistoryAsync(long userId);
    }
}
