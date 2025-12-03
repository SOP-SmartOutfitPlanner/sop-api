using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.SaveItemFromPostModels;
using System.Collections.Generic;
using System.Linq;

namespace SOPServer.Service.Mappers
{
    public class SaveItemFromPostProfile : Profile
    {
        public SaveItemFromPostProfile()
        {
            CreateMap<SaveItemFromPost, SaveItemFromPostModel>()
                .ForMember(dest => dest.ItemName, opt => opt.MapFrom(src => src.Item != null ? src.Item.Name : null))
                .ForMember(dest => dest.ItemImgUrl, opt => opt.MapFrom(src => src.Item != null ? src.Item.ImgUrl : null))
                .ForMember(dest => dest.PostBody, opt => opt.MapFrom(src => src.Post != null ? src.Post.Body : null))
                .ForMember(dest => dest.PostUserId, opt => opt.MapFrom(src => src.Post != null ? src.Post.UserId : null))
                .ForMember(dest => dest.PostUserDisplayName, opt => opt.MapFrom(src => src.Post != null && src.Post.User != null ? src.Post.User.DisplayName : null));

            CreateMap<SaveItemFromPost, SaveItemFromPostDetailedModel>()
                .ForMember(dest => dest.Item, opt => opt.MapFrom(src => src.Item))
                .ForMember(dest => dest.PostBody, opt => opt.MapFrom(src => src.Post != null ? src.Post.Body : null))
                .ForMember(dest => dest.PostUserId, opt => opt.MapFrom(src => src.Post != null ? src.Post.UserId : null))
                .ForMember(dest => dest.PostUserDisplayName, opt => opt.MapFrom(src => src.Post != null && src.Post.User != null ? src.Post.User.DisplayName : null));

            CreateMap<Pagination<SaveItemFromPost>, Pagination<SaveItemFromPostDetailedModel>>()
                .ConvertUsing<PaginationConverter<SaveItemFromPost, SaveItemFromPostDetailedModel>>();

            CreateMap<SaveItemFromPostCreateModel, SaveItemFromPost>();
        }
    }
}
