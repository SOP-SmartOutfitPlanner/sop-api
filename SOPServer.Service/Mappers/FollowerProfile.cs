using AutoMapper;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.FollowerModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Mappers
{
    public class FollowerProfile : Profile
    {
        public FollowerProfile()
        {
            CreateMap<CreateFollowerModel, Follower>();
            CreateMap<Follower, FollowerModel>();
        }
    }
}
