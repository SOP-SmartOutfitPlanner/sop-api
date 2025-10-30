using AutoMapper;

namespace SOPServer.Service.Mappers
{
    /// <summary>
    /// Main mapper configuration profile that consolidates all domain-specific mapper profiles.
    /// This profile acts as a central entry point for all AutoMapper configurations.
    /// Register only this profile in Program.cs using: AddAutoMapper(typeof(MapperConfigProfile).Assembly)
    /// </summary>
    public class MapperConfigProfile : Profile
    {
        public MapperConfigProfile()
        {
            // Include all separate mapper profiles
            // AutoMapper will automatically discover and load all Profile classes in the assembly
            // Including:
            // - ItemMapperProfile
            // - CategoryMapperProfile
            // - PostMapperProfile
            // - OutfitMapperProfile
            // - SeasonMapperProfile
            // - UserMapperProfile
            // - OccasionMapperProfile
            // - StyleMapperProfile
            // - PaginationConverter
        }
    }
}
