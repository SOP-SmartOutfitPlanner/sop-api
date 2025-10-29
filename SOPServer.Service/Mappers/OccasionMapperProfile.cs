using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.OccasionModels;

namespace SOPServer.Service.Mappers
{
    public class OccasionMapperProfile : Profile
    {
        public OccasionMapperProfile()
        {
            CreateMap<Occasion, OccasionModel>();

            CreateMap<OccasionModel, Occasion>();

            CreateMap<OccasionUpdateModel, Occasion>();

            CreateMap<OccasionCreateModel, Occasion>();

            CreateMap<Pagination<Occasion>, Pagination<OccasionModel>>()
                .ConvertUsing<PaginationConverter<Occasion, OccasionModel>>();
        }
    }
}
