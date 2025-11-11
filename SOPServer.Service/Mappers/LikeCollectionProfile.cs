using AutoMapper;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.LikeCollectionModels;

namespace SOPServer.Service.Mappers
{
    public class LikeCollectionProfile : Profile
{
        public LikeCollectionProfile()
        {
            CreateMap<LikeCollection, LikeCollectionModel>().ReverseMap();
     CreateMap<CreateLikeCollectionModel, LikeCollection>();
        }
    }
}
