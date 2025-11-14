using SOPServer.Repository.UnitOfWork;

namespace SOPServer.Service.Utils
{
    public class ContentVisibilityHelper
    {
        private readonly IUnitOfWork _unitOfWork;

        public ContentVisibilityHelper(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Determines if a user has admin privileges
        /// </summary>
        /// <param name="userId">User ID to check</param>
        /// <returns>True if user is an admin, false otherwise</returns>
        public async Task<bool> IsUserAdminAsync(long userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null) return false;

            return user.Role.ToString().ToUpper() == "ADMIN";
        }

        /// <summary>
        /// Determines if a requester can view hidden content
        /// Owner and Admin can view hidden content
        /// </summary>
        /// <param name="contentOwnerId">ID of the content owner</param>
        /// <param name="requesterId">ID of the requester (nullable)</param>
        /// <param name="isRequesterAdmin">Whether the requester is an admin</param>
        /// <returns>True if requester is authorized to view hidden content</returns>
        public static bool CanViewHiddenContent(long contentOwnerId, long? requesterId, bool isRequesterAdmin)
        {
            if (!requesterId.HasValue) return false;

            // Owner can view their own content
            if (contentOwnerId == requesterId.Value) return true;

            // Admin can view all content
            if (isRequesterAdmin) return true;

            return false;
        }
    }
}
