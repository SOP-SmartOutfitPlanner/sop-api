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
    public interface IQdrantService
    {
        Task EnsureCollectionExistsAsync();
        Task<bool> UpSertItem(List<float> embedding, Dictionary<string, object> payload, long id);
        Task<bool> DeleteItem(long id);
        [Description("Search for a top 5 item in user wardobe match with text")]
        Task<List<QDrantSearchModels>> SearchSimilarityByUserId(string descriptionItem, long userId);
        
        Task<List<QDrantSearchModels>> SearchSimilarityItemSystem([Description("Description item to find similarity")] string descriptionItem);
        
        /// <summary>
        /// Search for items by similarity and return only item IDs and scores (no DB queries)
        /// </summary>
        Task<List<ItemSearchResult>> SearchItemIdsByUserId(string descriptionItem, long userId);
    }
}
