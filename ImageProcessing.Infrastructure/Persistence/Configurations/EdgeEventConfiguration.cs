using ImageProcessing.Domain.Entities.EdgeEvents;
using ImageProcessing.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageProcessing.Infrastructure.Persistence.Configurations
{
    public sealed class EdgeEventConfiguration : IEntityTypeConfiguration<EdgeEvent>
    {
        public void Configure(EntityTypeBuilder<EdgeEvent> b)
        {
            b.ToTable("edge_event");
            b.HasKey(x => x.Id);
            b.Property(x => x.CameraId).HasMaxLength(256).IsRequired();
            b.Property(x => x.Detections).HasMaxLength(200);
            b.Property(x => x.ComputeModel).HasMaxLength(200);
            b.Property(x => x.FrameAnnotatedUrl).HasMaxLength(500);
            b.Property(x => x.FrameRawUrl).HasMaxLength(500);
            b.Property(x => x.CaptureTimestampUtc).IsRequired();
            b.Property(x => x.CreatedUtc).IsRequired();
        }
    }
}
