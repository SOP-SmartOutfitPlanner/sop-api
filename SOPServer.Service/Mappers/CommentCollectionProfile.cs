using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.CommentCollectionModels;

namespace SOPServer.Service.Mappers
{
    public class CommentCollectionProfile : Profile
    {
        public CommentCollectionProfile()
        {
    CreateMap<CommentCollection, CommentCollectionModel>()
     .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src =>
   src.User != null ? src.User.DisplayName : null))
         .ForMember(dest => dest.UserAvatarUrl, opt => opt.MapFrom(src =>
   src.User != null ? src.User.AvtUrl : null));

 CreateMap<CreateCommentCollectionModel, CommentCollection>();
         CreateMap<UpdateCommentCollectionModel, CommentCollection>();

     CreateMap<Pagination<CommentCollection>, Pagination<CommentCollectionModel>>()
    .ConvertUsing<PaginationConverter<CommentCollection, CommentCollectionModel>>();
        }
    }
}
