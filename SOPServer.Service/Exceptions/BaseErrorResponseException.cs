using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Exceptions
{
    public class BaseErrorResponseException : Exception
    {
        public int HttpStatusCode { get; }

        public BaseErrorResponseException(string message, int httpStatusCode) : base(message)
        {
            HttpStatusCode = httpStatusCode;
        }
    }
}
