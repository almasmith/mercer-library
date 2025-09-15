using Library.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Api.Data.Configurations
{
    public sealed class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
    {
        public void Configure(EntityTypeBuilder<Favorite> builder)
        {
            builder.HasKey(f => new { f.UserId, f.BookId });

            builder.Property(f => f.CreatedAt)
                .IsRequired();

            builder.HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Book)
                .WithMany()
                .HasForeignKey(f => f.BookId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}



