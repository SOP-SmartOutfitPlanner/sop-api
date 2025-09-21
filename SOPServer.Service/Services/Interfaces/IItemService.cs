using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.ResultModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IItemService
    {
        Task<BaseResponseModel> DeleteItemByIdAsync(long id);
        Task<BaseResponseModel> GetSummaryItem(IFormFile file);
        Task<BaseResponseModel> AddNewItem(ItemCreateModel model);
        Task<BaseResponseModel> GetItemById(long id);
        Task<BaseResponseModel> GetItemPaginationAsync(PaginationParameter paginationParameter);
        Task<BaseResponseModel> GetItemByUserPaginationAsync(PaginationParameter paginationParameter, long userId);
        Task<BaseResponseModel> UpdateItemAsync(long id, ItemCreateModel model);
        Task<BaseResponseModel> GetItemByIdAsync(long id);
        Task<BaseResponseModel> GetItemsAsync(long userId, PaginationParameter pagination);
    }
}
