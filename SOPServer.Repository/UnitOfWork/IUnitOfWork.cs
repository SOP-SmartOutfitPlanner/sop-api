using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        int Save();
        void Commit();
        void Rollback();
        Task SaveAsync();
    }
}
