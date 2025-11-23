using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.QDrantModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    // [GenerateJsonSchema(GoogleFunctionTool = true, MeaiFunctionTool = true, ToolName = "QdrantService")]
    public interface IQdrantService
    {
        Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default);
        Task<bool> UpSertItem(List<float> embedding, Dictionary<string, object> payload, long id, CancellationToken cancellationToken = default);
        Task<bool> DeleteItem(long id, CancellationToken cancellationToken = default);
        Task<List<QDrantSearchModels>> SearchSimilarityByUserId(string descriptionItem, long userId, CancellationToken cancellationToken = default);

        Task<List<QDrantSearchModels>> SearchSimilarityItemSystem(List<string> descriptionItems, CancellationToken cancellationToken = default);
        
        Task<List<ItemSearchResult>> SearchItemIdsByUserId(
            List<string> descriptionItems,
            long userId,
            CancellationToken cancellationToken = default);

        [Description("Search for items in system wardrobe with compact AI-optimized string format for easy processing")]
        Task<List<ItemForAISelection>> SearchSimilarityItemSystemCompact(
            [Description("List of item descriptions to find similarities (multiple category)")] List<string> descriptionItems,
            CancellationToken cancellationToken = default);

        [Description("Search for items by user ID with compact AI-optimized string format for easy processing")]
        Task<List<ItemForAISelection>> SearchItemsByUserIdCompact(
            [Description("List of item descriptions to find similarities (multiple category)")] List<string> descriptionItems,
            [Description("User ID to filter items")] long userId,
            CancellationToken cancellationToken = default);
    }
}
