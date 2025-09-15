using Library.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Api.Data.Configurations
{
    public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
    {
        public void Configure(EntityTypeBuilder<Book> builder)
        {
            builder.HasKey(b => b.Id);

            builder.Property(b => b.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(b => b.Author)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(b => b.Genre)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(b => b.PublishedDate)
                .IsRequired();

            builder.Property(b => b.Rating)
                .IsRequired();

            builder.Property(b => b.OwnerUserId)
                .IsRequired();

            builder.Property(b => b.CreatedAt)
                .IsRequired();

            builder.Property(b => b.UpdatedAt)
                .IsRequired();

            builder.Property(b => b.RowVersion)
                .IsRowVersion();

            builder.HasIndex(b => new { b.OwnerUserId, b.Genre })
                .HasDatabaseName("IX_Books_OwnerUserId_Genre");

            builder.HasIndex(b => new { b.OwnerUserId, b.PublishedDate })
                .HasDatabaseName("IX_Books_OwnerUserId_PublishedDate");

            builder.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Books_Rating_Range",
                    "Rating >= 1 AND Rating <= 5"));
        }
    }
}


