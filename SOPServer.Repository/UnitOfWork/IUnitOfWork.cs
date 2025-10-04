using SOPServer.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IItemRepository ItemRepository { get; }
        ICategoryRepository CategoryRepository { get; }
        IItemOccasionRepository ItemOccasionRepository { get; }
        IItemSeasonRepository ItemSeasonRepository { get; }
        IItemStyleRepository ItemStyleRepository { get; }
        IUserRepository UserRepository { get; }

        int Save();
        void Commit();
        void Rollback();
        Task SaveAsync();
    }
}
