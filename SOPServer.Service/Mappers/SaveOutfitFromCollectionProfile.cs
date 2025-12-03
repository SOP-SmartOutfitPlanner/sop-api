using AutoMapper;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.SaveOutfitFromCollectionModels;

namespace SOPServer.Service.Mappers
{
    public class SaveOutfitFromCollectionProfile : Profile
    {
        public SaveOutfitFromCollectionProfile()
        {
            CreateMap<SaveOutfitFromCollection, SaveOutfitFromCollectionModel>()
                .ForMember(dest => dest.OutfitName, opt => opt.MapFrom(src => src.Outfit != null ? src.Outfit.Name : null))
                .ForMember(dest => dest.OutfitDescription, opt => opt.MapFrom(src => src.Outfit != null ? src.Outfit.Description : null))
                .ForMember(dest => dest.CollectionTitle, opt => opt.MapFrom(src => src.Collection != null ? src.Collection.Title : null))
                .ForMember(dest => dest.CollectionUserId, opt => opt.MapFrom(src => src.Collection != null ? src.Collection.UserId : null))
                .ForMember(dest => dest.CollectionUserDisplayName, opt => opt.MapFrom(src => src.Collection != null && src.Collection.User != null ? src.Collection.User.DisplayName : null));

            CreateMap<SaveOutfitFromCollection, SaveOutfitFromCollectionDetailedModel>()
                .ForMember(dest => dest.Outfit, opt => opt.MapFrom(src => src.Outfit))
                .ForMember(dest => dest.CollectionTitle, opt => opt.MapFrom(src => src.Collection != null ? src.Collection.Title : null))
                .ForMember(dest => dest.CollectionUserId, opt => opt.MapFrom(src => src.Collection != null ? src.Collection.UserId : null))
                .ForMember(dest => dest.CollectionUserDisplayName, opt => opt.MapFrom(src => src.Collection != null && src.Collection.User != null ? src.Collection.User.DisplayName : null));

            CreateMap<SaveOutfitFromCollectionCreateModel, SaveOutfitFromCollection>();
        }
    }
}
