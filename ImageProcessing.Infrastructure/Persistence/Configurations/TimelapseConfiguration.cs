using ImageProcessing.Domain.Entities.Cameras;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageProcessing.Infrastructure.Persistence.Configurations
{
    public sealed class TimelapseConfiguration : IEntityTypeConfiguration<Domain.Entities.Timelapse.Timelapse>
    {
        public void Configure(EntityTypeBuilder<Domain.Entities.Timelapse.Timelapse> b)
        {
            b.ToTable("timelapse");
            b.HasKey(x => x.Id);
            b.Property(x => x.FileSize).HasMaxLength(256).IsRequired();
            b.Property(x => x.FileFormat).HasMaxLength(256).IsRequired();
            b.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
            b.Property(x => x.CreatedUtc).IsRequired();
        }
    }
}
