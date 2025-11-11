using System.Text.Json.Serialization;

namespace SOPServer.Repository.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReportType
    {
        POST = 0,
        COMMENT = 1
    }
}
