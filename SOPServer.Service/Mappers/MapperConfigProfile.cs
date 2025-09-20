using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.ItemModels;

namespace SOPServer.Service.Mappers
{
    public class MapperConfigProfile : Profile
    {
        public MapperConfigProfile()
        {
            CreateMap<ItemModel, Item>().ReverseMap();
        }

        public class PaginationConverter<TSource, TDestination> : ITypeConverter<Pagination<TSource>, Pagination<TDestination>>
        {
            public Pagination<TDestination> Convert(Pagination<TSource> source, Pagination<TDestination> destination, ResolutionContext context)
            {
                var mappedItems = context.Mapper.Map<List<TDestination>>(source);
                return new Pagination<TDestination>(mappedItems, source.TotalCount, source.CurrentPage, source.PageSize);
            }
        }
    }
}
