using System;
using System.Collections.Generic;
using System.Linq;
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

        public const string ITEM_NOT_EXISTED = "Item is not existed";
        public const string DELETE_ITEM_SUCCESS = "Item deleted successfully";

        public const string GET_USER_BY_EMAIL_SUCCESS = "Get user by email successfully";
        public const string USER_NOT_EXIST = "User is not exist";
        public const string USER_UPDATE_SUCCESS = "User updated successfully";
        public const string GET_USER_SUCCESS = "Get list user successfully";
        public const string USER_DELETE_SUCCESS = "User deleted successfully";
        public const string USER_ADDRESS_UPDATE_SUCCESS = "User address updated successfully";
        public const string USER_MUST_LOGIN_WITH_GOOGLE = "User must login with google";
        public const string EMAIL_OR_PASSWORD_INCORRECT = "Email or password incorrect";
        public const string USER_FORBIDDEN = "User forbidden access to the system";

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

        public const string CALL_REM_BACKGROUND_FAIL = "Call remove background service fail";

        public const string REM_BACKGROUND_IMAGE_FAIL = "Remove background image fail";
        
    }
}
