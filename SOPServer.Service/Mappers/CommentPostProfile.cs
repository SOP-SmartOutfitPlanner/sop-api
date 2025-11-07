using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.CommentPostModels;

namespace SOPServer.Service.Mappers
{
    public class CommentPostProfile : Profile
    {
        public CommentPostProfile()
        {
            CreateMap<CommentPost, CommentPostModel>()
                .ForMember(dest => dest.CommentParent, opt => opt.MapFrom(src =>
                    src.ParentComment != null ? src.ParentComment.Comment : null))
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.DisplayName : null))
                .ForMember(dest => dest.UserAvatarUrl, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.AvtUrl : null))
                .ForMember(dest => dest.UserRole, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.Role.ToString() : null));

            CreateMap<CreateCommentPostModel, CommentPost>();

            CreateMap<Pagination<CommentPost>, Pagination<CommentPostModel>>()
                .ConvertUsing<PaginationConverter<CommentPost, CommentPostModel>>();
        }
    }
}
