using CSharpToJsonSchema;
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
    [GenerateJsonSchema(GoogleFunctionTool = true, MeaiFunctionTool = true)]
    public interface IQdrantService
    {
        Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default);
        Task<bool> UpSertItem(List<float> embedding, Dictionary<string, object> payload, long id, CancellationToken cancellationToken = default);
        Task<bool> DeleteItem(long id, CancellationToken cancellationToken = default);
        Task<List<QDrantSearchModels>> SearchSimilarityByUserId(string descriptionItem, long userId, CancellationToken cancellationToken = default);

        [Description("Search for a top 5 item in system wardobe match with text")]
        Task<List<QDrantSearchModels>> SearchSimilarityItemSystem([Description("Description item to find similarity")] string descriptionItem, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Search for items by similarity and return only item IDs and scores (no DB queries)
        /// </summary>
        Task<List<ItemSearchResult>> SearchItemIdsByUserId(string descriptionItem, long userId, CancellationToken cancellationToken = default);
    }
}
