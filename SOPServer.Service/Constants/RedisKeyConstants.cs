using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Constants
{
    public static class RedisKeyConstants
    {
        public static string GetOtpKey(string email) => $"otp:{email}";
        public static string GetOtpAttemptKey(string email) => $"otp_attempt:{email}";
        public static string GetVerifiedEmailKey(string email) => $"verified_email:{email}";
        public static string GetAccessTokenKey(long userId) => $"token:access:{userId}";
        public static string GetRefreshTokenKey(long userId) => $"token:refresh:{userId}";
    }
}
