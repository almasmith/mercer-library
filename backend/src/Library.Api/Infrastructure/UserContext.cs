using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Library.Api.Infrastructure
{
    internal static class UserContext
    {
        internal static Guid GetUserId(HttpContext httpContext)
        {
            if (httpContext is null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var user = httpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            var idValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrWhiteSpace(idValue))
            {
                throw new UnauthorizedAccessException("User identifier claim not found.");
            }

            if (Guid.TryParse(idValue, out var userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("User identifier claim is invalid.");
        }
    }
}


