using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.CategoryModels;
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
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> GetCategoryPaginationAsync(PaginationParameter paginationParameter)
        {
            var categories = await _unitOfWork.CategoryRepository.ToPaginationIncludeAsync(paginationParameter,
                include: query => query.Include(x => x.Parent),
                orderBy: q => q.OrderByDescending(x => x.CreatedDate));

            var models = _mapper.Map<Pagination<CategoryModel>>(categories);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_ITEM_SUCCESS,
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

        public async Task<BaseResponseModel> GetCategoryByIdAsync(long id)
        {
            var category = await _unitOfWork.CategoryRepository.GetByIdIncludeAsync(id, include: q => q.Include(x => x.Parent));
            if (category == null)
            {
                throw new NotFoundException(MessageConstants.CATEGORY_NOT_EXIST);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_CATEGORY_BY_ID_SUCCESS,
                Data = _mapper.Map<CategoryModel>(category)
            };
        }

        public async Task<BaseResponseModel> GetCategoriesByParentIdAsync(long parentId, PaginationParameter paginationParameter)
        {
            var categories = await _unitOfWork.CategoryRepository.ToPaginationIncludeAsync(paginationParameter,
                include: query => query.Include(x => x.Parent),
              filter: x => x.ParentId == parentId,
                orderBy: q => q.OrderByDescending(x => x.CreatedDate));

            var models = _mapper.Map<Pagination<CategoryModel>>(categories);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_CATEGORY_BY_PARENTID_SUCCESS,
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

        public async Task<BaseResponseModel> GetRootCategoriesPaginationAsync(PaginationParameter paginationParameter)
        {
            var categories = await _unitOfWork.CategoryRepository.ToPaginationIncludeAsync(paginationParameter,
 filter: x => x.ParentId == null,
          orderBy: q => q.OrderByDescending(x => x.CreatedDate));

            var models = _mapper.Map<Pagination<CategoryModel>>(categories);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_ROOT_CATEGORIES_SUCCESS,
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

        public async Task<BaseResponseModel> CreateCategoryAsync(CategoryCreateModel model)
        {
            // if ParentId provided, ensure parent exists
            if (model.ParentId.HasValue)
            {
                var parent = await _unitOfWork.CategoryRepository.GetByIdAsync(model.ParentId.Value);
                if (parent == null)
                {
                    throw new NotFoundException(MessageConstants.CATEGORY_PARENT_NOT_EXIST);
                }
            }

            var entity = _mapper.Map<Category>(model);
            await _unitOfWork.CategoryRepository.AddAsync(entity);
            await _unitOfWork.SaveAsync();

            var created = await _unitOfWork.CategoryRepository.GetByIdIncludeAsync(entity.Id, include: q => q.Include(x => x.Parent));

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.CATEGORY_CREATE_SUCCESS,
                Data = _mapper.Map<CategoryModel>(created)
            };
        }

        public async Task<BaseResponseModel> DeleteCategoryByIdAsync(long id)
        {
            // fetch category including its children
            var category = await _unitOfWork.CategoryRepository.GetByIdIncludeAsync(id, include: q => q.Include(x => x.InverseParent));
            if (category == null)
            {
                throw new NotFoundException(MessageConstants.CATEGORY_NOT_EXIST);
            }

            // if this is a parent category (no ParentId), ensure all children are already soft-deleted
            if (category.ParentId == null)
            {
                var activeChildren = category.InverseParent?.Where(c => !c.IsDeleted).ToList();
                if (activeChildren != null && activeChildren.Any())
                {
                    throw new BadRequestException(MessageConstants.CATEGORY_HAS_CHILDREN);
                }
            }

            // otherwise (either a child or parent with no active children) allow soft delete
            _unitOfWork.CategoryRepository.SoftDeleteAsync(category);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.DELETE_CATEGORY_SUCCESS
            };
        }

        public async Task<BaseResponseModel> UpdateCategoryByIdAsync(CategoryUpdateModel model)
        {
            var category = await _unitOfWork.CategoryRepository.GetByIdAsync(model.Id);
            if (category == null)
            {
                throw new NotFoundException(MessageConstants.CATEGORY_NOT_EXIST);
            }

            // map changes
            _unitOfWork.CategoryRepository.UpdateAsync(_mapper.Map<Category>(model));
            _unitOfWork.Save();

            var updated = await _unitOfWork.CategoryRepository.GetByIdIncludeAsync(model.Id, include: q => q.Include(x => x.Parent));

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.UPDATE_CATEGORY_SUCCESS,
                Data = _mapper.Map<CategoryModel>(updated)
            };
        }
    }
}
