using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.SaveOutfitFromPostModels;
using System.Collections.Generic;
using System.Linq;

namespace SOPServer.Service.Mappers
{
    public class SaveOutfitFromPostProfile : Profile
    {
        public SaveOutfitFromPostProfile()
        {
            CreateMap<SaveOutfitFromPost, SaveOutfitFromPostModel>()
                .ForMember(dest => dest.OutfitName, opt => opt.MapFrom(src => src.Outfit != null ? src.Outfit.Name : null))
                .ForMember(dest => dest.OutfitDescription, opt => opt.MapFrom(src => src.Outfit != null ? src.Outfit.Description : null))
                .ForMember(dest => dest.PostBody, opt => opt.MapFrom(src => src.Post != null ? src.Post.Body : null))
                .ForMember(dest => dest.PostUserId, opt => opt.MapFrom(src => src.Post != null ? src.Post.UserId : null))
                .ForMember(dest => dest.PostUserDisplayName, opt => opt.MapFrom(src => src.Post != null && src.Post.User != null ? src.Post.User.DisplayName : null));

            CreateMap<SaveOutfitFromPost, SaveOutfitFromPostDetailedModel>()
                .ForMember(dest => dest.Outfit, opt => opt.MapFrom(src => src.Outfit))
                .ForMember(dest => dest.PostBody, opt => opt.MapFrom(src => src.Post != null ? src.Post.Body : null))
                .ForMember(dest => dest.PostUserId, opt => opt.MapFrom(src => src.Post != null ? src.Post.UserId : null))
                .ForMember(dest => dest.PostUserDisplayName, opt => opt.MapFrom(src => src.Post != null && src.Post.User != null ? src.Post.User.DisplayName : null));

            CreateMap<Pagination<SaveOutfitFromPost>, Pagination<SaveOutfitFromPostDetailedModel>>()
                .ConvertUsing<PaginationConverter<SaveOutfitFromPost, SaveOutfitFromPostDetailedModel>>();

            CreateMap<SaveOutfitFromPostCreateModel, SaveOutfitFromPost>();
        }
    }
}
