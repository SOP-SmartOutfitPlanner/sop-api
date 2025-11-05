using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.StyleModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class StyleService : IStyleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public StyleService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> GetStylePaginationAsync(PaginationParameter paginationParameter)
        {
            var styles = await _unitOfWork.StyleRepository.ToPaginationIncludeAsync(
                paginationParameter,
                filter: x => !x.IsDeleted &&
                    (string.IsNullOrWhiteSpace(paginationParameter.Search) ||
                     x.Name.Contains(paginationParameter.Search)),
                orderBy: q => q.OrderByDescending(x => x.CreatedDate));

            var models = _mapper.Map<Pagination<StyleModel>>(styles);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_STYLE_SUCCESS,
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

        public async Task<BaseResponseModel> GetStyleByIdAsync(long id)
        {
            var style = await _unitOfWork.StyleRepository.GetByIdIncludeAsync(id,
    include: q => q.Include(x => x.ItemStyles)
         .ThenInclude(x => x.Item)
    .ThenInclude(x => x.Category));

            if (style == null)
            {
                throw new NotFoundException(MessageConstants.STYLE_NOT_EXIST);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_STYLE_BY_ID_SUCCESS,
                Data = _mapper.Map<StyleDetailModel>(style)
            };
        }

        public async Task<BaseResponseModel> CreateStyleAsync(StyleCreateModel model)
        {
            var entity = _mapper.Map<Style>(model);
            entity.CreatedBy = CreatedBy.SYSTEM; // Admin creates system styles
            await _unitOfWork.StyleRepository.AddAsync(entity);
            await _unitOfWork.SaveAsync();

            var created = await _unitOfWork.StyleRepository.GetByIdAsync(entity.Id);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.STYLE_CREATE_SUCCESS,
                Data = _mapper.Map<StyleModel>(created)
            };
        }

        public async Task<BaseResponseModel> UpdateStyleByIdAsync(StyleUpdateModel model)
        {
            var style = await _unitOfWork.StyleRepository.GetByIdAsync(model.Id);
            if (style == null)
            {
                throw new NotFoundException(MessageConstants.STYLE_NOT_EXIST);
            }

            // Map the updated fields to the existing entity
            _mapper.Map(model, style);
            _unitOfWork.StyleRepository.UpdateAsync(style);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.StyleRepository.GetByIdAsync(model.Id);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.UPDATE_STYLE_SUCCESS,
                Data = _mapper.Map<StyleModel>(updated)
            };
        }

        public async Task<BaseResponseModel> DeleteStyleByIdAsync(long id)
        {
            var style = await _unitOfWork.StyleRepository.GetByIdIncludeAsync(id,
   include: q => q.Include(x => x.ItemStyles)
        .Include(x => x.UserStyles));

            if (style == null)
            {
                throw new NotFoundException(MessageConstants.STYLE_NOT_EXIST);
            }

            // Check if style has active items
            if (style.ItemStyles != null)
            {
                var activeItems = style.ItemStyles.Where(c => !c.IsDeleted).ToList();
                if (activeItems.Any())
                {
                    throw new BadRequestException(MessageConstants.STYLE_HAS_ITEM);
                }
            }

            // Check if style has active users
            if (style.UserStyles != null)
            {
                var activeUsers = style.UserStyles.Where(c => !c.IsDeleted).ToList();
                if (activeUsers.Any())
                {
                    throw new BadRequestException(MessageConstants.STYLE_HAS_USER);
                }
            }

            _unitOfWork.StyleRepository.SoftDeleteAsync(style);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.DELETE_STYLE_SUCCESS
            };
        }
    }
}
