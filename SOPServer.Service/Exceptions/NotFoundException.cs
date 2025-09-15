using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Exceptions
{
    public class NotFoundException : BaseErrorResponseException
    {
        public NotFoundException(string message) : base(message, 404)
        {
        }
    }
}
