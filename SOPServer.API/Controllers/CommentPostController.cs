using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.CommentPostModels;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/comment-posts")]
    [ApiController]
    public class CommentPostController : BaseController
    {
        private readonly ICommentPostService _commentPostService;
        
        public CommentPostController(ICommentPostService commentPostService)
        {
            _commentPostService = commentPostService;
        }

        /// <summary>
        /// Get comments by parent comment ID with pagination
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters</param>
        /// <param name="id">Parent comment ID</param>
        /// <returns>List of child comments</returns>
        [HttpGet("parent/{id}")]
        public Task<IActionResult> GetCommentsByParentId([FromQuery] PaginationParameter paginationParameter, long id)
        {
            return ValidateAndExecute(async () => await _commentPostService.GetCommentByParentId(paginationParameter, id));
        }

        /// <summary>
        /// Create a new comment on a post
        /// </summary>
        /// <param name="model">Comment creation model</param>
        /// <returns>Created comment</returns>
        [HttpPost]
        public Task<IActionResult> CreateComment([FromBody] CreateCommentPostModel model)
        {
            return ValidateAndExecute(async () => await _commentPostService.CreateNewComment(model));
        }

        /// <summary>
        /// Delete a comment
        /// </summary>
        /// <param name="id">Comment ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteComment(int id)
        {
            return ValidateAndExecute(async () => await _commentPostService.DeleteCommentPost(id));
        }
    }
}
