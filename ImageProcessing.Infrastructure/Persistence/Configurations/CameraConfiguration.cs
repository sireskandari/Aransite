using ImageProcessing.Domain.Entities.Cameras;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageProcessing.Infrastructure.Persistence.Configurations
{
    public sealed class CameraConfiguration : IEntityTypeConfiguration<Camera>
    {
        public void Configure(EntityTypeBuilder<Camera> b)
        {
            b.ToTable("camera");
            b.HasKey(x => x.Id);
            b.Property(x => x.Key).HasMaxLength(256).IsRequired();
            b.Property(x => x.Location).HasMaxLength(256).IsRequired();
            b.Property(x => x.RTSP).HasMaxLength(500).IsRequired();
            b.Property(x => x.CreatedUtc).IsRequired();
            b.Property(x => x.IsActive).IsRequired();
        }
    }
}
