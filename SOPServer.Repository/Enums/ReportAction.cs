using System.Text.Json.Serialization;

namespace SOPServer.Repository.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReportAction
    {
        NONE = 0,
        HIDE = 1,
        DELETE = 2,
        WARN = 3,
        SUSPEND = 4
    }
}
