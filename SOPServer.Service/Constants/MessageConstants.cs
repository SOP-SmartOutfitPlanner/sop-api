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
        public const string GET_CATEGORY_BY_ID_SUCCESS = "Get category by id successfully";
        public const string GET_CATEGORY_BY_PARENTID_SUCCESS = "Get categories by parent id successfully";
        public const string DELETE_CATEGORY_SUCCESS = "Category deleted successfully";
        public const string UPDATE_CATEGORY_SUCCESS = "Category updated successfully";
        public const string CATEGORY_HAS_CHILDREN = "Category has active children and cannot be deleted";
        public const string CATEGORY_CREATE_SUCCESS = "Category created successfully";
        public const string CATEGORY_NOT_EXIST = "Category is not existed";
        public const string CATEGORY_PARENT_NOT_EXIST = "Category is not existed";

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
        public const string POST_DELETE_SUCCESS = "Post deleted successfully";
        public const string POST_GET_SUCCESS = "Post retrieved successfully";
        public const string POST_NOT_FOUND = "Post not found";
        
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

        // Outfit related messages
        public const string OUTFIT_NOT_FOUND = "Outfit not found";
        public const string OUTFIT_GET_SUCCESS = "Outfit retrieved successfully";
        public const string OUTFIT_TOGGLE_FAVORITE_SUCCESS = "Outfit favorite status toggled successfully";
        public const string OUTFIT_MARK_USED_SUCCESS = "Outfit marked as used successfully";

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

        public const string GET_AISETTING_SUCCESS = "Get AI Settings successfully";
        public const string AISETTING_NOT_EXIST = "AI setting does not exist";
        public const string AISETTING_ALREADY_EXIST = "AI setting with this type already exists";
        public const string AISETTING_CREATE_SUCCESSFULLY = "AI setting create successfully";
        public const string AISETTING_UPDATE_SUCCESSFULLY = "AI setting update successfully";
        public const string AISETTING_DELETE_SUCCESSFULLY = "AI setting delete successfully";

    }
}
