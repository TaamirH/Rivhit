using System.Security.Claims;

namespace Rivhit.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static string GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("Authenticated user id claim is missing.");
        }

        return id;
    }
}

