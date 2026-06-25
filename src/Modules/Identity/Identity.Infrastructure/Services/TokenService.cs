using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Application.Common.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Services;

// Generate JWT access token cho user sau khi login thành công.
public class TokenService : ITokenService {
    private readonly JwtOptions _jwtOptions;
    private readonly UserManager<AppUser> _userManager;

    public TokenService(JwtOptions jwtOptions, UserManager<AppUser> userManager) {
        _jwtOptions = jwtOptions;
        _userManager = userManager;
    }

    public string GenerateToken(AppUser user) {
        var claims = new List<Claim> {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("fullName", user.FullName),
            new("isActive", user.IsActive.ToString()),
            // Dùng trực tiếp từ Domain entity thay vì query AspNetUserRoles.
            new(ClaimTypes.Role, user.Role.ToString())
        };
 
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
 
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes),
            signingCredentials: credentials);
 
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}