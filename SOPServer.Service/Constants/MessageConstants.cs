using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Constants
{
    public class MessageConstants
    {
        public const string INTERNAL_SERVER_ERROR = "Internal Server Error";
        public const string MIMETYPE_NOT_VALID = "Mime type is not valid";
        public const string IMAGE_EXTENSION_NOT_SUPPORT = "Image extension is not supported";
        public const string IMAGE_IS_LARGE = "Image is very large";
        public const string IMAGE_IS_NOT_VALID = "Image is not valid";
        public const string IMAGE_IS_VALID = "Image is valid";
        public const string IMAGE_VALIDATION_FAILED = "Image validation failed after multiple attempts";
        public const string IMAGE_ANALYSIS_FAILED = "Image analysis failed after multiple attempts";
        public const string GET_SUMMARY_IMAGE_SUCCESS = "Get summary image successfully";
        public const string DELETE_FILE_SUCCESS = "Delete file successfully";

        public const string ITEM_NOT_EXISTED = "Item is not existed";
        public const string ITEM_CREATE_SUCCESS = "Item created successfully";
        public const string DELETE_ITEM_SUCCESS = "Item deleted successfully";
        public const string GET_LIST_ITEM_SUCCESS = "Get list item successfully";
        public const string ITEM_ALREADY_EXISTS = "Item already exists";
        public const string ITEM_UPDATE_SUCCESS = "Item updated successfully";
        public const string ITEM_GET_SUCCESS = "Get item successfully";

        public const string GET_USER_BY_EMAIL_SUCCESS = "Get user by email successfully";
        public const string USER_NOT_EXIST = "User is not exist";
        public const string USER_UPDATE_SUCCESS = "User updated successfully";
        public const string GET_USER_SUCCESS = "Get list user successfully";
        public const string USER_DELETE_SUCCESS = "User deleted successfully";
        public const string USER_ALREADY_DELETED = "User has already been deleted";
        public const string USER_ADDRESS_UPDATE_SUCCESS = "User address updated successfully";
        public const string USER_MUST_LOGIN_WITH_GOOGLE = "User must login with google";
        public const string EMAIL_OR_PASSWORD_INCORRECT = "Email or password incorrect";
        public const string USER_FORBIDDEN = "User forbidden access to the system";
        public const string EMAIL_EXISTED = "Email is existed";
        public const string PASSWORD_DOES_NOT_MATCH = "Password does not match";
        public const string USER_MUST_LOGIN_WITH_PASSWORD = "User must login with email and password";
        public const string USER_NOT_VERIFY = "User not verify email";
        public const string USER_ALREADY_VERIFY = "User has already verified email";
        public const string ONBOARDING_SUCCESS = "Onboarding data saved successfully";
        public const string INVALID_USER_TOKEN = "Invalid user token";
        public const string ONBOARDING_ALREADY_COMPLETED_MSG = "Onboarding has already been completed";

        public const string TOKEN_NOT_VALID = "Token not valid";
        public const string LOGIN_SUCCESS_MESSAGE = "Login successfully";
        public const string LOGIN_GOOGLE_SUCCESS_MESSAGE = "Login with google successfully";
        public const string TOKEN_REFRESH_SUCCESS_MESSAGE = "Token refresh successfully";
        public const string FILE_NOT_FOUND = "File not found";
        public const string UPLOAD_FILE_SUCCESS = "File uploaded successfully";

        // Category related messages used by CategoryService
        public const string GET_LIST_CATEGORY_SUCCESS = "Get list categories successfully";
        public const string GET_CATEGORY_BY_ID_SUCCESS = "Get category by id successfully";
        public const string GET_CATEGORY_BY_PARENTID_SUCCESS = "Get categories by parent id successfully";
        public const string GET_ROOT_CATEGORIES_SUCCESS = "Get root categories successfully";
        public const string DELETE_CATEGORY_SUCCESS = "Category deleted successfully";
        public const string UPDATE_CATEGORY_SUCCESS = "Category updated successfully";
        public const string CATEGORY_HAS_CHILDREN = "Category has active children and cannot be deleted";
        public const string CATEGORY_CREATE_SUCCESS = "Category created successfully";
        public const string CATEGORY_NOT_EXIST = "Category is not existed";
        public const string CATEGORY_PARENT_NOT_EXIST = "Category is not existed";
        public const string CATEGORY_MAX_DEPTH_EXCEEDED = "Cannot create category: maximum depth of 2 levels exceeded. Child categories can only be created under root categories";

        // Season related messages used by SeasonService
        public const string GET_SEASON_BY_ID_SUCCESS = "Get season by id successfully";
        public const string DELETE_SEASON_SUCCESS = "Season deleted successfully";
        public const string UPDATE_SEASON_SUCCESS = "Season updated successfully";
        public const string SEASON_HAS_ITEM = "Season has active items and cannot be deleted";
        public const string SEASON_CREATE_SUCCESS = "Season created successfully";
        public const string SEASON_NOT_EXIST = "Season is not existed";
        public const string GET_LIST_SEASON_SUCCESS = "Get list season successfully";

        public const string CALL_REM_BACKGROUND_FAIL = "Call remove background service fail";

        public const string REM_BACKGROUND_IMAGE_FAIL = "Remove background image fail";

        // Post related messages
        public const string POST_CREATE_SUCCESS = "Post created successfully";
        public const string POST_UPDATE_SUCCESS = "Post updated successfully";
        public const string POST_DELETE_SUCCESS = "Post deleted successfully";
        public const string POST_GET_SUCCESS = "Post retrieved successfully";
        public const string POST_NOT_FOUND = "Post not found";
        public const string GET_LIST_POST_BY_USER_SUCCESS = "Get list posts by user successfully";
        public const string GET_TOP_CONTRIBUTORS_SUCCESS = "Get top contributors successfully";
        public const string GET_LIST_POST_SUCCESS = "Get post successfully";

        public const string OTP_SENT_SUCCESS = "OTP sent successfully to your gmail";
        public const string OTP_VERIFY_SUCCESS = "Verify OTP successfully";
        public const string OTP_INVALID = "OTP is invalid or expired!";
        public const string OTP_TOO_MANY_ATTEMPTS = "You request OTP too many time, please retry after 15 minutes!";
        public const string EMAIL_SEND_FAILED = "Send email failed, please retry";

        // New constants moved from UserService
        public const string USER_ALREADY_REGISTERED_OTP_SENT = "User is already registered. Please check your mail for new OTP";
        public const string OTP_HAS_BEEN_SENT_TO_GMAIL = "OTP has been sent to your gmail";
        public const string REGISTERED_SUCCESS_OTP_SENT = "Successfully registered. Please check your mail for new OTP.";
        public const string REGISTER_SUCCESS_VI = "Registration successful! Please check your email to verify your account.";
        public const string OTP_SENT_VI = "OTP has been sent to your email";
        public const string INVALID_TOKEN_CLAIMS = "Invalid token claims";
        public const string LOGGED_OUT = "Logged out";
        public const string WELCOME_EMAIL_SUBJECT = "Welcome to Smart Outfit Planner";

        // Occasion related messages used by OccasionService
        public const string GET_OCCASION_BY_ID_SUCCESS = "Get occasion by id successfully";
        public const string GET_LIST_OCCASION_SUCCESS = "Get list occasion successfully";
        public const string DELETE_OCCASION_SUCCESS = "Occasion deleted successfully";
        public const string UPDATE_OCCASION_SUCCESS = "Occasion updated successfully";
        public const string OCCASION_HAS_ITEM = "Occasion has active items and cannot be deleted";
        public const string OCCASION_CREATE_SUCCESS = "Occasion created successfully";
        public const string OCCASION_NOT_EXIST = "Occasion is not existed";
        public const string ADD_OCCASIONS_TO_ITEM_SUCCESS = "Occasions added to item successfully";
        public const string ITEM_OCCASION_ALREADY_EXISTS = "One or more occasions are already added to this item";
        public const string REMOVE_OCCASION_FROM_ITEM_SUCCESS = "Occasion removed from item successfully";
        public const string ITEM_OCCASION_NOT_FOUND = "This occasion is not associated with the item";
        public const string REPLACE_OCCASIONS_FOR_ITEM_SUCCESS = "Occasions replaced for item successfully";

        // Outfit related messages
        public const string OUTFIT_NOT_FOUND = "Outfit not found";
        public const string OUTFIT_GET_SUCCESS = "Outfit retrieved successfully";
        public const string OUTFIT_CREATE_SUCCESS = "Outfit created successfully";
        public const string OUTFIT_UPDATE_SUCCESS = "Outfit updated successfully";
        public const string OUTFIT_DELETE_SUCCESS = "Outfit deleted successfully";
        public const string GET_LIST_OUTFIT_SUCCESS = "Get list outfit successfully";
        public const string OUTFIT_TOGGLE_FAVORITE_SUCCESS = "Outfit favorite status toggled successfully";
        public const string OUTFIT_TOGGLE_SAVE_SUCCESS = "Outfit save status toggled successfully";
        public const string OUTFIT_DUPLICATE_ITEMS = "An outfit with the same combination of items already exists";
        public const string OUTFIT_ACCESS_DENIED = "You don't have permission to access this outfit";

        // Reset Password related messages
        public const string RESET_PASSWORD_REQUEST_SENT = "If the email exists, a password reset OTP has been sent";
        public const string RESET_PASSWORD_OTP_VERIFIED = "OTP verified successfully. Reset token has been generated";
        public const string RESET_PASSWORD_SUCCESS = "Password has been reset successfully";
        public const string RESET_TOKEN_INVALID = "Reset token is invalid or expired";
        public const string RESET_TOKEN_ALREADY_USED = "Reset token has already been used";
        public const string USER_MUST_USE_GOOGLE_LOGIN = "This account uses Google login. Password reset is not available";
        public const string PASSWORD_RESET_SUBJECT_MAIL = "Password Reset Successful - SOP";

        // User Profile related messages
        public const string GET_USER_PROFILE_SUCCESS = "Get user profile successfully";
        public const string GET_USER_BY_ID_SUCCESS = "Get user by id successfully";

        // Style related messages used by StyleService
        public const string GET_STYLE_BY_ID_SUCCESS = "Get style by id successfully";
        public const string GET_LIST_STYLE_SUCCESS = "Get list style successfully";
        public const string DELETE_STYLE_SUCCESS = "Style deleted successfully";
        public const string UPDATE_STYLE_SUCCESS = "Style updated successfully";
        public const string STYLE_HAS_ITEM = "Style has active items and cannot be deleted";
        public const string STYLE_HAS_USER = "Style has active users and cannot be deleted";
        public const string STYLE_CREATE_SUCCESS = "Style created successfully";
        public const string STYLE_NOT_EXIST = "Style is not existed";
        public const string ADD_STYLES_TO_ITEM_SUCCESS = "Styles added to item successfully";
        public const string ITEM_STYLE_ALREADY_EXISTS = "One or more styles are already added to this item";
        public const string REMOVE_STYLE_FROM_ITEM_SUCCESS = "Style removed from item successfully";
        public const string ITEM_STYLE_NOT_FOUND = "This style is not associated with the item";
        public const string REPLACE_STYLES_FOR_ITEM_SUCCESS = "Styles replaced for item successfully";

        // Season-Item relationship messages
        public const string ADD_SEASONS_TO_ITEM_SUCCESS = "Seasons added to item successfully";
        public const string ITEM_SEASON_ALREADY_EXISTS = "One or more seasons are already added to this item";
        public const string REMOVE_SEASON_FROM_ITEM_SUCCESS = "Season removed from item successfully";
        public const string ITEM_SEASON_NOT_FOUND = "This season is not associated with the item";
        public const string REPLACE_SEASONS_FOR_ITEM_SUCCESS = "Seasons replaced for item successfully";

        public const string GET_AISETTING_SUCCESS = "Get AI Settings successfully";
        public const string AISETTING_NOT_EXIST = "AI setting does not exist";
        public const string AISETTING_ALREADY_EXIST = "AI setting with this type already exists";
        public const string AISETTING_CREATE_SUCCESSFULLY = "AI setting create successfully";
        public const string AISETTING_UPDATE_SUCCESSFULLY = "AI setting update successfully";
        public const string AISETTING_DELETE_SUCCESSFULLY = "AI setting delete successfully";

        // Job related messages
        public const string GET_JOB_SUCCESS = "Get job successfully";
        public const string GET_LIST_JOB_SUCCESS = "Get list job successfully";
        public const string JOB_NOT_EXIST = "Job does not exist";
        public const string JOB_ALREADY_EXIST = "Job with this name already exists";
        public const string JOB_CREATE_SUCCESS = "Job created successfully";
        public const string JOB_UPDATE_SUCCESS = "Job updated successfully";
        public const string JOB_DELETE_SUCCESS = "Job deleted successfully";

        // LikePost related messages
        public const string LIKE_POST_SUCCESS = "Like post successfully";
        public const string UNLIKE_POST_SUCCESS = "Unlike post successfully";
        public const string ALREADY_LIKE_POST = "You have already liked this post";
        public const string LIKE_POST_NOT_FOUND = "Like not found";

        // CommentPost related messages
        public const string COMMENT_CREATE_SUCCESS = "Comment created successfully";
        public const string COMMENT_DELETE_SUCCESS = "Comment deleted successfully";
        public const string COMMENT_NOT_FOUND = "Comment not found";
        public const string COMMENT_GET_SUCCESS = "Get comment successfully";
        public const string GET_LIST_COMMENT_SUCCESS = "Get list comment successfully";
        public const string PARENT_COMMENT_NOT_FOUND = "Parent comment not found";
        public const string COMMENT_CANNOT_REPLY_MORE_THAN_ONE_LEVEL = "Cannot reply to a reply comment";

        // Follower related messages
        public const string FOLLOW_USER_SUCCESS = "Follow user successfully";
        public const string UNFOLLOW_USER_SUCCESS = "Unfollow user successfully";
        public const string ALREADY_FOLLOWING = "You are already following this user";
        public const string FOLLOWER_NOT_FOUND = "Follower relationship not found";
        public const string GET_FOLLOWER_COUNT_SUCCESS = "Get follower count successfully";
        public const string GET_FOLLOWING_COUNT_SUCCESS = "Get following count successfully";
        public const string CHECK_FOLLOWING_STATUS_SUCCESS = "Check following status successfully";
        public const string CANNOT_FOLLOW_YOURSELF = "You cannot follow yourself";
        public const string GET_FOLLOWERS_LIST_SUCCESS = "Get followers list successfully";
        public const string GET_FOLLOWING_LIST_SUCCESS = "Get following list successfully";

        // UserOccasion related messages
        public const string USER_OCCASION_NOT_FOUND = "User occasion not found";
        public const string USER_OCCASION_GET_SUCCESS = "User occasion retrieved successfully";
        public const string USER_OCCASION_CREATE_SUCCESS = "User occasion created successfully";
        public const string USER_OCCASION_UPDATE_SUCCESS = "User occasion updated successfully";
        public const string USER_OCCASION_DELETE_SUCCESS = "User occasion deleted successfully";
        public const string GET_LIST_USER_OCCASION_SUCCESS = "Get list user occasion successfully";
        public const string USER_OCCASION_ACCESS_DENIED = "You don't have permission to access this occasion";

        // Newsfeed related messages
        public const string NEWSFEED_GET_SUCCESS = "Newsfeed retrieved successfully";
        public const string NEWSFEED_EMPTY = "No posts available in your feed";
        public const string NEWSFEED_REFRESH_SUCCESS = "Newsfeed refreshed successfully";

        // OutfitCalendar related messages
        public const string OUTFIT_CALENDAR_NOT_FOUND = "Outfit calendar entry not found";
        public const string OUTFIT_CALENDAR_GET_SUCCESS = "Outfit calendar entry retrieved successfully";
        public const string OUTFIT_CALENDAR_CREATE_SUCCESS = "Outfit calendar entry created successfully";
        public const string OUTFIT_CALENDAR_UPDATE_SUCCESS = "Outfit calendar entry updated successfully";
        public const string OUTFIT_CALENDAR_DELETE_SUCCESS = "Outfit calendar entry deleted successfully";
        public const string GET_LIST_OUTFIT_CALENDAR_SUCCESS = "Get list outfit calendar successfully";
        public const string OUTFIT_CALENDAR_ACCESS_DENIED = "You don't have permission to access this outfit calendar entry";
        public const string OUTFIT_CALENDAR_ALREADY_EXISTS = "An outfit is already scheduled for this date";

        public static string GET_USER_STATS_SUCCESS = "Get user stats successfully";
    }
}
