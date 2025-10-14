using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.SeasonModels;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/seasons")]
    [ApiController]
    public class SeasonController : BaseController
    {
        private readonly ISeasonService _seasonService;

        public SeasonController(ISeasonService seasonService)
        {
            _seasonService = seasonService;
        }

        [HttpGet]
        public Task<IActionResult> GetSeasons([FromQuery] PaginationParameter paginationParameter)
        {
            return ValidateAndExecute(async () => await _seasonService.GetSeasonPaginationAsync(paginationParameter));
        }

        [HttpGet("{id}")]
        public Task<IActionResult> GetSeasonById(long id)
        {
            return ValidateAndExecute(async () => await _seasonService.GetSeasonByIdAsync(id));
        }


        [HttpPost]
        public Task<IActionResult> CreateSeason(SeasonCreateModel model)
        {
            return ValidateAndExecute(async () => await _seasonService.CreateSeasonAsync(model));
        }

        [HttpPut]
        public Task<IActionResult> UpdateSeason(SeasonUpdateModel model)
        {
            return ValidateAndExecute(async () => await _seasonService.UpdateSeasonByIdAsync(model));
        }

        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteSeason(long id)
        {
            return ValidateAndExecute(async () => await _seasonService.DeleteSeasonByIdAsync(id));
        }
    }
}
