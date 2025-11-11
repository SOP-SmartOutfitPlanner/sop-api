using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.CommentCollectionModels;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/comment-collections")]
    [ApiController]
    public class CommentCollectionController : BaseController
    {
        private readonly ICommentCollectionService _commentCollectionService;

        public CommentCollectionController(ICommentCollectionService commentCollectionService)
        {
            _commentCollectionService = commentCollectionService;
        }

        /// <summary>
        /// Get comments by collection ID with pagination
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters</param>
        /// <param name="id">Collection ID</param>
        /// <returns>List of comments for the collection</returns>
        [HttpGet("collection/{id}")]
        public Task<IActionResult> GetCommentsByCollectionId([FromQuery] PaginationParameter paginationParameter, long id)
        {
            return ValidateAndExecute(async () => await _commentCollectionService.GetCommentsByCollectionId(paginationParameter, id));
        }

        /// <summary>
        /// Create a new comment on a collection
        /// </summary>
        /// <param name="model">Comment creation model</param>
        /// <returns>Created comment</returns>
        [HttpPost]
        public Task<IActionResult> CreateComment([FromBody] CreateCommentCollectionModel model)
        {
            return ValidateAndExecute(async () => await _commentCollectionService.CreateCommentCollection(model));
        }

        /// <summary>
        /// Update an existing comment
        /// </summary>
        /// <param name="id">Comment ID</param>
        /// <param name="model">Comment update model</param>
        /// <returns>Updated comment</returns>
        [HttpPut("{id}")]
        public Task<IActionResult> UpdateComment(long id, [FromBody] UpdateCommentCollectionModel model)
        {
            return ValidateAndExecute(async () => await _commentCollectionService.UpdateCommentCollection(id, model));
        }

        /// <summary>
        /// Delete a comment
        /// </summary>
        /// <param name="id">Comment ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteComment(long id)
        {
            return ValidateAndExecute(async () => await _commentCollectionService.DeleteCommentCollection(id));
        }
    }
}
