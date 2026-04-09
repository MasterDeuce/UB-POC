using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class WorkInstructionJobConfiguration : IEntityTypeConfiguration<WorkInstructionJob>
{
    public void Configure(EntityTypeBuilder<WorkInstructionJob> builder)
    {
        builder.ToTable("WorkInstructionJobs");

        builder.HasKey(job => job.Id);

        builder.Property(job => job.ProjectNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(job => job.Status)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(job => job.ExternalReference)
            .HasMaxLength(100);

        builder.Property(job => job.RequestPayloadJson)
            .IsRequired();

        builder.Property(job => job.CreatedAtUtc)
            .IsRequired();

        builder.Property(job => job.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasOne(job => job.Project)
            .WithMany(project => project.WorkInstructionJobs)
            .HasForeignKey(job => job.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(job => job.ProjectNumber);
        builder.HasIndex(job => job.Status);
        builder.HasIndex(job => job.CreatedAtUtc);
        builder.HasIndex(job => new { job.ProjectId, job.CreatedAtUtc });
    }
}
