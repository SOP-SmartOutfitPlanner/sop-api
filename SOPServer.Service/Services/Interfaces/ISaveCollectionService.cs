using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.SaveCollectionModels;

namespace SOPServer.Service.Services.Interfaces
{
    public interface ISaveCollectionService
    {
        Task<BaseResponseModel> ToggleSaveCollectionAsync(CreateSaveCollectionModel model);
        Task<BaseResponseModel> GetSavedCollectionsByUserAsync(PaginationParameter paginationParameter, long userId);
        Task<BaseResponseModel> CheckIfCollectionSavedAsync(long userId, long collectionId);
    }
}
