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
        public const string FILE_NOT_FOUND = "File not found";
        public const string UPLOAD_FILE_SUCCESS = "File uploaded successfully";
    }
}
