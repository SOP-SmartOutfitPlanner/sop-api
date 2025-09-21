
using Microsoft.Extensions.Configuration;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Utils
{
    public static class AuthenTokenUtils
    {
        public static string GenerateAccessToken(User user, Role role, IConfiguration configuration)
        {

            var authClaims = new List<Claim>();

            if (role != null)
            {
                authClaims.Add(new Claim(ClaimTypes.Email, user.Email));
                authClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
                authClaims.Add(new Claim(ClaimTypes.Role, role.ToString()));
                authClaims.Add(new Claim("UserId", user.Id.ToString()));
                authClaims.Add(new Claim("FirstTime", user.IsFirstTime.ToString()));
            }
            var accessToken = GenerateJsonWebToken.CreateToken(authClaims, configuration, DateTime.UtcNow);
            return new JwtSecurityTokenHandler().WriteToken(accessToken);
        }

        public static string GenerateRefreshToken(User user, IConfiguration configuration)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
            };
            var refreshToken = GenerateJsonWebToken.CreateRefreshToken(claims, configuration, DateTime.UtcNow);
            return new JwtSecurityTokenHandler().WriteToken(refreshToken).ToString();
        }
    }
}
