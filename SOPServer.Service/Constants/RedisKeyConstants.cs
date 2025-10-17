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
        public static string GetAccessTokenKey(long userId, string jti) => $"token:access:{userId}:{jti}";
        public static string GetRefreshTokenKey(long userId, string jti) => $"token:refresh:{userId}:{jti}";
        public static string GetResetPasswordOtpKey(string email) => $"reset_password_otp:{email}";
        public static string GetResetPasswordAttemptKey(string email) => $"reset_password_attempt:{email}";
        public static string GetResetTokenKey(string email) => $"reset_token:{email}";
        public static string GetUsedResetTokenKey(string resetToken) => $"used_reset_token:{resetToken}";
    }
}
