using Library.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Api.Data.Configurations
{
    public sealed class BookReadConfiguration : IEntityTypeConfiguration<BookRead>
    {
        public void Configure(EntityTypeBuilder<BookRead> builder)
        {
            builder.HasKey(br => br.Id);

            builder.Property(br => br.BookId)
                .IsRequired();

            builder.Property(br => br.UserId)
                .IsRequired();

            builder.Property(br => br.OccurredAt)
                .IsRequired();

            builder.HasOne(br => br.User)
                .WithMany()
                .HasForeignKey(br => br.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(br => br.Book)
                .WithMany()
                .HasForeignKey(br => br.BookId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(br => new { br.UserId, br.OccurredAt })
                .HasDatabaseName("IX_BookReads_UserId_OccurredAt");

            builder.HasIndex(br => new { br.BookId, br.OccurredAt })
                .HasDatabaseName("IX_BookReads_BookId_OccurredAt");
        }
    }
}




