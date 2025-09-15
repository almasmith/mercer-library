using System;
using Library.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Data
{
    public sealed class LibraryDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) {}
    }
}


