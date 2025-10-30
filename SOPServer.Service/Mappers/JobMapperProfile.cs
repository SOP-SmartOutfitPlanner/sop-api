using AutoMapper;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.JobModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Mappers
{
    public class JobMapperProfile : Profile
    {
        public JobMapperProfile()
        {
            CreateMap<Job, JobModel>().ReverseMap();
            CreateMap<JobRequestModel, Job>();
        }
    }
}
