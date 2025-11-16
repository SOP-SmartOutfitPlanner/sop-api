using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Enums
{
    public enum SubscriptionPlanStatus
    {
        DRAFT = 0,      // Plan is being drafted, not visible to users
        ACTIVE = 1,     // Plan is active and available for purchase by all users
        INACTIVE = 2,   // Plan is inactive, but old users who had it can rebuy
        ARCHIVED = 3    // Plan is completely closed, no one can buy it
    }
}
