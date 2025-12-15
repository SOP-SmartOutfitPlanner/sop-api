using System.Collections.Generic;

namespace SOPServer.Service.BusinessModels.VirtualTryOnModels
{
    /// <summary>
    /// Response model for batch virtual try-on feature
    /// </summary>
    public class BatchVirtualTryOnResponse
    {
        /// <summary>
        /// Total time taken to process all items
        /// </summary>
        /// <example>5.2s</example>
        public string TotalTime { get; set; } = string.Empty;

        /// <summary>
        /// List of individual try-on results
        /// </summary>
        public List<BatchTryOnResult> Results { get; set; } = new List<BatchTryOnResult>();
    }

    /// <summary>
    /// Individual result for each try-on item in the batch
    /// </summary>
    public class BatchTryOnResult
    {
        /// <summary>
        /// Unique identifier matching the request
        /// </summary>
        /// <example>1</example>
        public int Uuid { get; set; }

        /// <summary>
        /// Whether the try-on was successful
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }

        /// <summary>
        /// URL of the generated try-on image (if successful)
        /// </summary>
        /// <example>https://example.com/result.jpg</example>
        public string? Url { get; set; }

        /// <summary>
        /// Error message (if failed)
        /// </summary>
        /// <example>null</example>
        public string? Error { get; set; }

        /// <summary>
        /// Time taken to process this item
        /// </summary>
        /// <example>1.5s</example>
        public string? Time { get; set; }
    }
}
