using System.Threading;
using System.Threading.Tasks;
using Library.Api.Domain;

namespace Library.Api.Services;

public interface IJwtTokenService
{
	/// <summary>
	/// Creates a signed JWT for the specified user.
	/// </summary>
	/// <param name="user">The application user.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Tuple containing the token string and expiresIn (seconds).</returns>
	Task<(string token, int expiresIn)> CreateTokenAsync(ApplicationUser user, CancellationToken cancellationToken);
}

