using Library.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Api.Data.Configurations
{
    public sealed class UserStatsVersionConfiguration : IEntityTypeConfiguration<UserStatsVersion>
    {
        public void Configure(EntityTypeBuilder<UserStatsVersion> builder)
        {
            builder.HasKey(x => x.UserId);

            builder.Property(x => x.Version)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired();

            builder.Property(x => x.RowVersion)
                .IsConcurrencyToken()
                .ValueGeneratedNever();
        }
    }
}

 

