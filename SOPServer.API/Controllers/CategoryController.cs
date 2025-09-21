using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.CategoryModels;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/categories")]
    [ApiController]
    public class CategoryController : BaseController
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public Task<IActionResult> GetCategories([FromQuery] PaginationParameter paginationParameter)
        {
            return ValidateAndExecute(async () => await _categoryService.GetCategoryPaginationAsync(paginationParameter));
        }

        [HttpGet("{id}")]
        public Task<IActionResult> GetCategoryById(long id)
        {
            return ValidateAndExecute(async () => await _categoryService.GetCategoryByIdAsync(id));
        }

        [HttpGet("parent/{parentId}")]
        public Task<IActionResult> GetCategoriesByParentId([FromQuery] PaginationParameter paginationParameter, long parentId)
        {
            return ValidateAndExecute(async () => await _categoryService.GetCategoriesByParentIdAsync(parentId, paginationParameter));
        }

        [HttpPost]
        public Task<IActionResult> CreateCategory(CategoryCreateModel model)
        {
            return ValidateAndExecute(async () => await _categoryService.CreateCategoryAsync(model));
        }

        [HttpPut]
        public Task<IActionResult> UpdateCategory(CategoryUpdateModel model)
        {
            return ValidateAndExecute(async () => await _categoryService.UpdateCategoryByIdAsync(model));
        }

        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteCategory(long id)
        {
            return ValidateAndExecute(async () => await _categoryService.DeleteCategoryByIdAsync(id));
        }
    }
}
