using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SOPServer.Service.BusinessModels.VirtualTryOnModels
{
    /// <summary>
    /// Response model from the external batch try-on service
    /// </summary>
    public class ExternalBatchTryOnResponse
    {
        /// <summary>
        /// Total time taken to process all items
        /// </summary>
        [JsonPropertyName("total_time")]
        public string? TotalTime { get; set; }

        /// <summary>
        /// List of individual try-on results
        /// </summary>
        [JsonPropertyName("results")]
        public List<ExternalBatchTryOnResult>? Results { get; set; }
    }

    /// <summary>
    /// Individual result from the external batch try-on service
    /// </summary>
    public class ExternalBatchTryOnResult
    {
        /// <summary>
        /// Unique identifier matching the request
        /// </summary>
        [JsonPropertyName("uuid")]
        public int Uuid { get; set; }

        /// <summary>
        /// Whether the try-on was successful
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// URL of the generated try-on image (if successful)
        /// </summary>
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        /// <summary>
        /// Error message (if failed)
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        /// <summary>
        /// Time taken to process this item
        /// </summary>
        [JsonPropertyName("time")]
        public string? Time { get; set; }
    }
}
