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
        public object? Data { get; }

        public BaseErrorResponseException(string message, int httpStatusCode) : base(message)
        {
            HttpStatusCode = httpStatusCode;
        }

        public BaseErrorResponseException(string message, int httpStatusCode, object data) : base(message)
        {
            HttpStatusCode = httpStatusCode;
            Data = data;
        }
    }
}
