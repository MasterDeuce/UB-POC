using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class WorkInstructionExecutionConfiguration : IEntityTypeConfiguration<WorkInstructionExecution>
{
    public void Configure(EntityTypeBuilder<WorkInstructionExecution> builder)
    {
        builder.ToTable("WorkInstructionExecutions");

        builder.HasKey(execution => execution.Id);

        builder.Property(execution => execution.StartedAtUtc)
            .IsRequired();

        builder.Property(execution => execution.Outcome)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(execution => execution.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(execution => execution.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasOne(execution => execution.WorkInstructionJob)
            .WithMany(job => job.Executions)
            .HasForeignKey(execution => execution.WorkInstructionJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(execution => execution.WorkInstructionJobId);
        builder.HasIndex(execution => execution.StartedAtUtc);
    }
}
