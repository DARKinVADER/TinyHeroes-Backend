using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
}
