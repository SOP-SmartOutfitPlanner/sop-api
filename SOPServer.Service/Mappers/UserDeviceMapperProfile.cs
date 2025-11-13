using AutoMapper;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.UserDeviceModels;

namespace SOPServer.Service.Mappers
{
    public class UserDeviceMapperProfile : Profile
    {
        public UserDeviceMapperProfile()
        {
            CreateMap<UserDevice, UserDeviceModel>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedDate));

            CreateMap<CreateUserDeviceModel, UserDevice>();
        }
    }
}
