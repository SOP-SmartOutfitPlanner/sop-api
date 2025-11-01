using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.QDrantModels;
using System;
using System.Collections.Generic;
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
        Task<List<QDrantSearchModels>> SearchSimilarityByUserId(List<float> embedding, long userId, SlotItem slotItem);
    }
}
