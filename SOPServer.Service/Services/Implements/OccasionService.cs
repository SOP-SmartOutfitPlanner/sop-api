using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.OccasionModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class OccasionService : IOccasionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OccasionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> GetOccasionPaginationAsync(PaginationParameter paginationParameter)
        {
            var occasions = await _unitOfWork.OccasionRepository.ToPaginationIncludeAsync(paginationParameter,
         orderBy: q => q.OrderByDescending(x => x.CreatedDate));

            var models = _mapper.Map<Pagination<OccasionModel>>(occasions);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_OCCASION_SUCCESS,
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

        public async Task<BaseResponseModel> GetOccasionByIdAsync(long id)
        {
            var occasion = await _unitOfWork.OccasionRepository.GetByIdIncludeAsync(id,
                            include: q => q.Include(x => x.ItemOccasions)
                     .ThenInclude(x => x.Item)
                .ThenInclude(x => x.Category));

            if (occasion == null)
            {
                throw new NotFoundException(MessageConstants.OCCASION_NOT_EXIST);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_OCCASION_BY_ID_SUCCESS,
                Data = _mapper.Map<OccasionModel>(occasion)
            };
        }

        public async Task<BaseResponseModel> CreateOccasionAsync(OccasionCreateModel model)
        {
            var entity = _mapper.Map<Occasion>(model);
            await _unitOfWork.OccasionRepository.AddAsync(entity);
            await _unitOfWork.SaveAsync();

            var created = await _unitOfWork.OccasionRepository.GetByIdAsync(entity.Id);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.OCCASION_CREATE_SUCCESS,
                Data = _mapper.Map<OccasionModel>(created)
            };
        }

        public async Task<BaseResponseModel> DeleteOccasionByIdAsync(long id)
        {
            var occasion = await _unitOfWork.OccasionRepository.GetByIdIncludeAsync(id,
             include: q => q.Include(x => x.ItemOccasions));

            if (occasion == null)
            {
                throw new NotFoundException(MessageConstants.OCCASION_NOT_EXIST);
            }

            // Check if occasion has active items
            if (occasion.ItemOccasions != null)
            {
                var activeItems = occasion.ItemOccasions.Where(c => !c.IsDeleted).ToList();
                if (activeItems.Any())
                {
                    throw new BadRequestException(MessageConstants.OCCASION_HAS_ITEM);
                }
            }

            _unitOfWork.OccasionRepository.SoftDeleteAsync(occasion);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.DELETE_OCCASION_SUCCESS
            };
        }

        public async Task<BaseResponseModel> UpdateOccasionByIdAsync(OccasionUpdateModel model)
        {
            var occasion = await _unitOfWork.OccasionRepository.GetByIdAsync(model.Id);
            if (occasion == null)
            {
                throw new NotFoundException(MessageConstants.OCCASION_NOT_EXIST);
            }

            // Map the updated fields to the existing entity
            _mapper.Map(model, occasion);
            _unitOfWork.OccasionRepository.UpdateAsync(occasion);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.OccasionRepository.GetByIdAsync(model.Id);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.UPDATE_OCCASION_SUCCESS,
                Data = _mapper.Map<OccasionModel>(updated)
            };
        }
    }
}
