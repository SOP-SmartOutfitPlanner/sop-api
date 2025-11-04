using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SOPServer.Repository.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CreatedBy
    {
        SYSTEM = 0,
        USER = 1
    }
}
