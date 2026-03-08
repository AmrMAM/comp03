using AMWatch.Application.Commands;
using AMWatch.Application.Interfaces;
using AMWatch.Domain.Entities;
using AMWatch.Domain.Interfaces;

namespace AMWatch.Application.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public AuthService(IUserRepository userRepository, IJwtTokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<Guid> RegisterAsync(RegisterUserCommand command, string passwordHash)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = command.Username,
            Email = command.Email,
            PasswordHash = passwordHash
        };

        await _userRepository.AddAsync(user);
        return user.Id;
    }

    public async Task<string?> LoginAsync(LoginUserCommand command, Func<string, string, bool> verifyPassword)
    {
        var user = await _userRepository.GetByEmailAsync(command.Email);
        if (user is null || !verifyPassword(command.Password, user.PasswordHash))
        {
            return null;
        }

        return _tokenGenerator.Generate(user);
    }
}
