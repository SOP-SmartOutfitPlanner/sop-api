using SOPServer.Service.BusinessModels.ResultModels;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IWeatherService
    {
        Task<BaseResponseModel> GetCitiesByName(string cityName, int limit = 5);
        Task<BaseResponseModel> GetWeatherByCoordinates(double latitude, double longitude, int? cnt);
    }
}