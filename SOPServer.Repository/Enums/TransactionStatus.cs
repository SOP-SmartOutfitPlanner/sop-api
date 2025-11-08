using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Enums
{
    public enum TransactionStatus
    {
        PENDING = 0,
        COMPLETED = 1,
        FAILED = 2,
        CANCELLED = 3
    }
}
