using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(project => project.Id);

        builder.Property(project => project.ProjectNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(project => project.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(project => project.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(project => project.ProjectNumber)
            .IsUnique();
    }
}
