using System;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.7")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("Infrastructure.Persistence.Entities.Project", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uniqueidentifier");

            b.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("datetime2");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)");

            b.Property<string>("ProjectNumber")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");

            b.HasKey("Id");

            b.HasIndex("ProjectNumber")
                .IsUnique();

            b.ToTable("Projects");
        });

        modelBuilder.Entity("Infrastructure.Persistence.Entities.WorkInstructionExecution", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uniqueidentifier");

            b.Property<DateTime?>("CompletedAtUtc")
                .HasColumnType("datetime2");

            b.Property<string>("ErrorMessage")
                .HasMaxLength(1000)
                .HasColumnType("nvarchar(1000)");

            b.Property<string>("Outcome")
                .IsRequired()
                .HasMaxLength(30)
                .HasColumnType("nvarchar(30)");

            b.Property<byte[]>("RowVersion")
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("rowversion");

            b.Property<DateTime>("StartedAtUtc")
                .HasColumnType("datetime2");

            b.Property<Guid>("WorkInstructionJobId")
                .HasColumnType("uniqueidentifier");

            b.HasKey("Id");

            b.HasIndex("StartedAtUtc");

            b.HasIndex("WorkInstructionJobId");

            b.ToTable("WorkInstructionExecutions");
        });

        modelBuilder.Entity("Infrastructure.Persistence.Entities.WorkInstructionJob", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uniqueidentifier");

            b.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("datetime2");

            b.Property<string>("ExternalReference")
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

            b.Property<Guid>("ProjectId")
                .HasColumnType("uniqueidentifier");

            b.Property<string>("ProjectNumber")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");

            b.Property<string>("RequestPayloadJson")
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            b.Property<byte[]>("RowVersion")
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("rowversion");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(30)
                .HasColumnType("nvarchar(30)");

            b.Property<DateTime?>("UpdatedAtUtc")
                .HasColumnType("datetime2");

            b.HasKey("Id");

            b.HasIndex("CreatedAtUtc");

            b.HasIndex("ProjectId", "CreatedAtUtc");

            b.HasIndex("ProjectNumber");

            b.HasIndex("Status");

            b.ToTable("WorkInstructionJobs");
        });

        modelBuilder.Entity("Infrastructure.Persistence.Entities.WorkInstructionExecution", b =>
        {
            b.HasOne("Infrastructure.Persistence.Entities.WorkInstructionJob", "WorkInstructionJob")
                .WithMany("Executions")
                .HasForeignKey("WorkInstructionJobId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("WorkInstructionJob");
        });

        modelBuilder.Entity("Infrastructure.Persistence.Entities.WorkInstructionJob", b =>
        {
            b.HasOne("Infrastructure.Persistence.Entities.Project", "Project")
                .WithMany("WorkInstructionJobs")
                .HasForeignKey("ProjectId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.Navigation("Project");
        });

        modelBuilder.Entity("Infrastructure.Persistence.Entities.Project", b =>
        {
            b.Navigation("WorkInstructionJobs");
        });

        modelBuilder.Entity("Infrastructure.Persistence.Entities.WorkInstructionJob", b =>
        {
            b.Navigation("Executions");
        });
#pragma warning restore 612, 618
    }
}
