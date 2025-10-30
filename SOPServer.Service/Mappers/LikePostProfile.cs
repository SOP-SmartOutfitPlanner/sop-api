using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.LikePostModels;
using SOPServer.Service.BusinessModels.OccasionModels;

namespace SOPServer.Service.Mappers
{
    public class LikePostProfile : Profile
    {
        public LikePostProfile()
        {
            CreateMap<LikePost, LikePostModel>().ReverseMap();
            CreateMap<LikePost, CreateLikePostModel>().ReverseMap();
        }
    }
}
