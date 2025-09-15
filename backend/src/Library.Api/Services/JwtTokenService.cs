using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Library.Api.Configuration;
using Library.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Library.Api.Services;

public sealed class JwtTokenService : IJwtTokenService
{
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly JwtOptions _jwtOptions;

	public JwtTokenService(UserManager<ApplicationUser> userManager, IOptions<JwtOptions> jwtOptions)
	{
		_userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
		_jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
	}

	public async Task<(string token, int expiresIn)> CreateTokenAsync(ApplicationUser user, CancellationToken cancellationToken)
	{
		if (user is null) throw new ArgumentNullException(nameof(user));

		var now = DateTimeOffset.UtcNow;
		var expires = now.AddMinutes(_jwtOptions.ExpiresMinutes);

		var claims = new List<Claim>
		{
			new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
			new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
			new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
		};

		if (!string.IsNullOrWhiteSpace(user.Email))
		{
			claims.Add(new Claim(ClaimTypes.Email, user.Email));
		}

		// Add role claims
		var roles = await _userManager.GetRolesAsync(user);
		foreach (var role in roles)
		{
			claims.Add(new Claim(ClaimTypes.Role, role));
		}

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var jwt = new JwtSecurityToken(
			issuer: _jwtOptions.Issuer,
			audience: _jwtOptions.Audience,
			claims: claims,
			notBefore: now.UtcDateTime,
			expires: expires.UtcDateTime,
			signingCredentials: creds
		);

		var token = new JwtSecurityTokenHandler().WriteToken(jwt);
		var expiresIn = (int)Math.Max(0, (expires - now).TotalSeconds);

		return (token, expiresIn);
	}
}

