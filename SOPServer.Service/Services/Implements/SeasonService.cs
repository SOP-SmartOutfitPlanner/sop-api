using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.SeasonModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace SOPServer.Service.Services.Implements
{
    public class SeasonService : ISeasonService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SeasonService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> GetSeasonPaginationAsync(PaginationParameter paginationParameter)
        {
            var seasons = await _unitOfWork.SeasonRepository.ToPaginationIncludeAsync(paginationParameter,
                orderBy: q => q.OrderByDescending(x => x.CreatedDate));

            var models = _mapper.Map<Pagination<SeasonModel>>(seasons);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_SEASON_SUCCESS,
                Data = new ModelPaging
                {
                    Data = models,
                    MetaData = new
                    {
                        models.TotalCount,
                        models.PageSize,
                        models.CurrentPage,
                        models.TotalPages,
                        models.HasNext,
                        models.HasPrevious
                    }
                }
            };
        }

        public async Task<BaseResponseModel> GetSeasonByIdAsync(long id)
        {
            var season = await _unitOfWork.SeasonRepository.GetByIdIncludeAsync(id, 
                include: q => q.Include(x => x.ItemSeasons)
                              .ThenInclude(x => x.Item)
                              .ThenInclude(x => x.Category)
);
            if (season == null)
            {
                throw new NotFoundException(MessageConstants.SEASON_NOT_EXIST);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_SEASON_BY_ID_SUCCESS,
                Data = _mapper.Map<SeasonDetailModel>(season)
            };
        }

        public async Task<BaseResponseModel> CreateSeasonAsync(SeasonCreateModel model)
        {

            var entity = _mapper.Map<Season>(model);
            await _unitOfWork.SeasonRepository.AddAsync(entity);
            await _unitOfWork.SaveAsync();

            var created = await _unitOfWork.SeasonRepository.GetByIdAsync(entity.Id);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.SEASON_CREATE_SUCCESS,
                Data = _mapper.Map<SeasonModel>(created)
            };
        }

        public async Task<BaseResponseModel> DeleteSeasonByIdAsync(long id)
        {
            var season = await _unitOfWork.SeasonRepository.GetByIdIncludeAsync(id, include: q => q.Include(x => x.ItemSeasons));
            if (season == null)
            {
                throw new NotFoundException(MessageConstants.SEASON_NOT_EXIST);
            }
            
            // Check if season has active items
            if (season.ItemSeasons != null)
            {
                var activeItems = season.ItemSeasons.Where(c => !c.IsDeleted).ToList();
                if (activeItems.Any())
                {
                    throw new BadRequestException(MessageConstants.SEASON_HAS_ITEM);
                }
            }
            
            _unitOfWork.SeasonRepository.SoftDeleteAsync(season);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.DELETE_SEASON_SUCCESS
            };
        }

        public async Task<BaseResponseModel> UpdateSeasonByIdAsync(SeasonUpdateModel model)
        {
            var season = await _unitOfWork.SeasonRepository.GetByIdAsync(model.Id);
            if (season == null)
            {
                throw new NotFoundException(MessageConstants.SEASON_NOT_EXIST);
            }

            // Map the updated fields to the existing entity
            _mapper.Map(model, season);
            _unitOfWork.SeasonRepository.UpdateAsync(season);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.SeasonRepository.GetByIdAsync(model.Id);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.UPDATE_SEASON_SUCCESS,
                Data = _mapper.Map<SeasonModel>(updated)
            };
        }
    }
}
