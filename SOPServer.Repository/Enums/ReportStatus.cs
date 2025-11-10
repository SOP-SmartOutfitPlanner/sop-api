using System.Text.Json.Serialization;

namespace SOPServer.Repository.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReportStatus
    {
        PENDING = 0,
        REVIEWED = 1,
        RESOLVED = 2,
        REJECTED = 3
    }
}
