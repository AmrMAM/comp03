using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace AMWatch.Presentation.Auth;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(_anonymous));

    public void Authenticate(string username)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, username)], "jwt");
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public void Logout() => NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
}
