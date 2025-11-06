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

        public NotFoundException(string message, object data) : base(message, 404, data)
        {
        }
    }
}
