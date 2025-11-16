using ImageProcessing.Domain.Entities.DetectTargets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageProcessing.Infrastructure.Persistence.Configurations
{
    public sealed class DetectTargetConfiguration : IEntityTypeConfiguration<DetectTarget>
    {
        public void Configure(EntityTypeBuilder<DetectTarget> b)
        {
            b.ToTable("detect_target");
            b.HasKey(x => x.Id);
            b.Property(x => x.CameraKey).HasMaxLength(256).IsRequired();
            b.Property(x => x.Targets).HasMaxLength(1000).IsRequired();
            b.Property(x => x.CreatedUtc).IsRequired();
        }
    }
}
