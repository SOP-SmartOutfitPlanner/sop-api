using AutoMapper;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.ReportCommunityModels;

namespace SOPServer.Service.Mappers
{
    public class ReportCommunityMapperProfile : Profile
    {
        public ReportCommunityMapperProfile()
        {
            CreateMap<ReportCommunityCreateModel, ReportCommunity>();
            CreateMap<ReportCommunity, ReportCommunityModel>();
        }
    }
}
