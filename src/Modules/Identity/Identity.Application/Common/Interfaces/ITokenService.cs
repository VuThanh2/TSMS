using Identity.Domain.Entities;

namespace Identity.Application.Common.Interfaces;

public interface ITokenService {
    string GenerateToken(AppUser user);
}