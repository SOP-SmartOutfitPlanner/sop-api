using AutoMapper;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.SaveOutfitFromPostModels;

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

            CreateMap<SaveOutfitFromPostCreateModel, SaveOutfitFromPost>();
        }
    }
}
