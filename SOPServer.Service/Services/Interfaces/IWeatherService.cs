using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Models;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IWeatherService
    {
        Task<BaseResponseModel> GetWeatherAsync(string cityName, int cnt);
    }
}