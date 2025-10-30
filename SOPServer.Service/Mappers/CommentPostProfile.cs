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
                    src.ParentComment != null ? src.ParentComment.Comment : null));

            CreateMap<CreateCommentPostModel, CommentPost>();

            CreateMap<Pagination<CommentPost>, Pagination<CommentPostModel>>()
                .ConvertUsing<PaginationConverter<CommentPost, CommentPostModel>>();
        }
    }
}
