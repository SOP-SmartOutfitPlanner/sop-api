using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.CategoryModels;

namespace SOPServer.Service.Mappers
{
    public class CategoryMapperProfile : Profile
    {
        public CategoryMapperProfile()
        {
            CreateMap<Category, CategoryModel>()
                .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : null));

            CreateMap<CategoryModel, Category>();
            CreateMap<Category, CategoryItemModel>();
            CreateMap<CategoryItemModel, Category>();

            CreateMap<CategoryUpdateModel, Category>();

            CreateMap<CategoryCreateModel, Category>();

            CreateMap<Pagination<Category>, Pagination<CategoryModel>>()
                .ConvertUsing<PaginationConverter<Category, CategoryModel>>();
        }
    }
}
