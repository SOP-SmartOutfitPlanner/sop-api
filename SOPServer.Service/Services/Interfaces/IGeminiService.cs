using Microsoft.AspNetCore.Http;
using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IGeminiService
    {
        Task<bool> ImageValidation(IFormFile file);
    }
}
