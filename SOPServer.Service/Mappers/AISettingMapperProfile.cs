using AutoMapper;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.AISettingModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Mappers
{
    public class AISettingMapperProfile : Profile
    {
        public AISettingMapperProfile()
        {
            CreateMap<AISetting, AISettingModel>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString())).ReverseMap();

            CreateMap<AISettingRequestModel, AISetting>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type));
        }
    }
}
