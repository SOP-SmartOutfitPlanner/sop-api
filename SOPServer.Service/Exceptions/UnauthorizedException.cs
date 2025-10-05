using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Exceptions
{
    internal class UnauthorizedException : BaseErrorResponseException
    {
        public UnauthorizedException(string message) : base(message, 401)
        {
        }
    }
}
