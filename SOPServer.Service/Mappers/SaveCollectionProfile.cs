using AutoMapper;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.SaveCollectionModels;

namespace SOPServer.Service.Mappers
{
    public class SaveCollectionProfile : Profile
    {
        public SaveCollectionProfile()
        {
            CreateMap<SaveCollection, SaveCollectionModel>().ReverseMap();
            CreateMap<CreateSaveCollectionModel, SaveCollection>();
        }
    }
}
