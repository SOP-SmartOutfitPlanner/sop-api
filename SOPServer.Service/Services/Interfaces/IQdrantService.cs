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

        /// <summary>
        /// Search for items in system wardrobe matching with list text descriptions
        /// Returns concise string format: ID:x|Desc:x|Color:x|Style:x|Occasion:x|Season:x|Score:x
        /// </summary>
        [Description("Search for items in system wardrobe matching with list text descriptions")]
        Task<List<string>> SearchSimilarityItemSystem([Description("List of item descriptions to find similarities")] List<string> descriptionItems, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Search for items by similarity and return concise item information
        /// Returns string format: ID:x|Desc:x|Color:x|Style:x|Occasion:x|Season:x|Score:x
        /// </summary>
        Task<List<string>> SearchItemIdsByUserId(List<string> descriptionItems, long userId, CancellationToken cancellationToken = default);
    }
}
