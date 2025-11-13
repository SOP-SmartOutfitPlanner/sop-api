using AutoMapper;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.DashboardModels;
using System.Linq;

namespace SOPServer.Service.Mappers
{
    /// <summary>
    /// AutoMapper profile for Stylist Dashboard models
    /// </summary>
    public class StylistDashboardMapperProfile : Profile
    {
        public StylistDashboardMapperProfile()
        {
            // Map Collection entity to TopCollectionModel for stylist dashboard
            CreateMap<Collection, TopCollectionModel>()
                .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => 
                    src.LikeCollections != null ? src.LikeCollections.Count(lc => !lc.IsDeleted) : 0))
                .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => 
                    src.CommentCollections != null ? src.CommentCollections.Count(cc => !cc.IsDeleted) : 0))
                .ForMember(dest => dest.SaveCount, opt => opt.MapFrom(src => 
                    src.SaveCollections != null ? src.SaveCollections.Count(sc => !sc.IsDeleted) : 0))
                .ForMember(dest => dest.TotalEngagement, opt => opt.MapFrom(src =>
                    (src.LikeCollections != null ? src.LikeCollections.Count(lc => !lc.IsDeleted) : 0) +
                    (src.CommentCollections != null ? src.CommentCollections.Count(cc => !cc.IsDeleted) : 0) +
                    (src.SaveCollections != null ? src.SaveCollections.Count(sc => !sc.IsDeleted) : 0)));
        }
    }
}
