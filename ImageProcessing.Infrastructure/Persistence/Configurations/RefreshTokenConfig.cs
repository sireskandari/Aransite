using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ImageProcessing.Domain.Entities.Auth;

namespace ImageProcessing.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.Token).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Token).IsUnique();
        b.Property(x => x.CreatedUtc).HasPrecision(6);
        b.Property(x => x.ExpiresUtc).HasPrecision(6);
        b.Property(x => x.RevokedUtc).HasPrecision(6);
    }
}