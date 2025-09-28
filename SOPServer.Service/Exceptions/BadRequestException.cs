using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Exceptions
{
    public class BadRequestException : BaseErrorResponseException
    {
        public BadRequestException(string message) : base(message, 400)
        {
        }
    }
}
