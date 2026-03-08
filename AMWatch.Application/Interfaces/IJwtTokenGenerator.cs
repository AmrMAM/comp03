using AMWatch.Domain.Entities;

namespace AMWatch.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}
