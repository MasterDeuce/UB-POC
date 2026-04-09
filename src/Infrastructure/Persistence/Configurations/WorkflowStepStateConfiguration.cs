using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class WorkflowStepStateConfiguration : IEntityTypeConfiguration<WorkflowStepState>
{
    public void Configure(EntityTypeBuilder<WorkflowStepState> builder)
    {
        builder.ToTable("WorkflowStepStates");

        builder.HasKey(state => state.Id);

        builder.Property(state => state.Workflow)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(state => state.StepName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(state => state.Status)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(state => state.LastError)
            .HasMaxLength(2000);

        builder.Property(state => state.LastAttemptedAtUtc)
            .IsRequired();

        builder.Property(state => state.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasOne(state => state.WorkInstructionJob)
            .WithMany()
            .HasForeignKey(state => state.WorkInstructionJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(state => new { state.WorkInstructionJobId, state.Workflow, state.StepName })
            .IsUnique();
    }
}
