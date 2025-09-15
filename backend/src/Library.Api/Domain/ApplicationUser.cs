using System;
using Microsoft.AspNetCore.Identity;

namespace Library.Api.Domain
{
    public sealed class ApplicationUser : IdentityUser<Guid>
    {
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}


