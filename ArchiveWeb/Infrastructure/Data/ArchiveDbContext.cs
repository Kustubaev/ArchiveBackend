using ArchiveWeb.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArchiveWeb.Infrastructure.Data;

public class ArchiveDbContext : DbContext
{
    public ArchiveDbContext(DbContextOptions<ArchiveDbContext> options) 
        : base(options)
    {
    }

    public DbSet<ArchiveConfiguration> ArchiveConfigurations { get; set; }
    public DbSet<Letter> Letters { get; set; }
    public DbSet<Box> Boxes { get; set; }
    public DbSet<FileArchive> FileArchives { get; set; }
    public DbSet<ArchiveHistory> ArchiveHistories { get; set; }
    public DbSet<Applicant> Applicants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ArchiveConfiguration
        modelBuilder.Entity<ArchiveConfiguration>(entity =>
        {
            entity.ToTable("archive_configurations");
            entity.HasKey(e => e.Id);
        });

        // Letter
        modelBuilder.Entity<Letter>(entity =>
        {
            entity.ToTable("letters");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Value)
                .IsUnique()
                .HasDatabaseName("uk_letter_value");

            entity.Property(e => e.Value)
                .HasMaxLength(1)
                .IsRequired();
        });

        // Box
        modelBuilder.Entity<Box>(entity =>
        {
            entity.ToTable("boxes");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Number)
                .IsUnique()
                .HasDatabaseName("uk_box_number");
        });

        // FileArchive
        modelBuilder.Entity<FileArchive>(entity =>
        {
            entity.ToTable("file_archives");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FullName)
                .HasDatabaseName("idx_file_archives_fullName");
            entity.HasIndex(e => e.FirstLetterSurname)
                .HasDatabaseName("idx_file_archives_letterValue");
            entity.HasIndex(e => e.BoxId)
                .HasDatabaseName("idx_file_archives_box_id");
            entity.HasIndex(e => e.LetterId)
                .HasDatabaseName("idx_file_archives_letter_id");
            entity.HasIndex(e => e.IsDeleted)
                .HasDatabaseName("idx_file_archives_is_deleted");
            entity.HasIndex(e => e.ApplicantId)
                .IsUnique()
                .HasDatabaseName("uk_applicant");

            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.FileNumberForArchive)
                .HasMaxLength(6)
                .IsRequired();

            entity.HasOne(e => e.Box)
                .WithMany(b => b.FileArchives)
                .HasForeignKey(e => e.BoxId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.Letter)
                .WithMany(l => l.FileArchives)
                .HasForeignKey(e => e.LetterId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Applicant)
                .WithOne(a => a.FileArchive)
                .HasForeignKey<FileArchive>(e => e.ApplicantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Applicant
        modelBuilder.Entity<Applicant>(entity =>
        {
            entity.ToTable("applicants");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email)
                //.IsUnique()
                .HasDatabaseName("idx_applicant_email");
            entity.HasIndex(e => e.PhoneNumber)
                .HasDatabaseName("idx_applicant_phone");

            entity.Property(e => e.Surname)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.FirstName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Patronymic)
                .HasMaxLength(255);
            
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(e => e.EducationLevel)
                .HasConversion<int>();
            
            entity.Property(e => e.StudyForm)
                .HasConversion<int>();
        });

        // ArchiveHistory
        modelBuilder.Entity<ArchiveHistory>(entity =>
        {
            entity.ToTable("archive_history");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FileArchiveId)
                .HasDatabaseName("idx_archive_history_file_id");
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("idx_archive_history_created_at");
            
            entity.Property(e => e.Reason)
                .HasMaxLength(500);
            
            entity.HasOne(e => e.FileArchive)
                .WithMany(f => f.History)
                .HasForeignKey(e => e.FileArchiveId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.ActualLetter)
                .WithMany(l => l.HistoryEntries)
                .HasForeignKey(e => e.NewLetterId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ActualBox)
                .WithMany(b => b.HistoryEntries)
                .HasForeignKey(e => e.NewBoxId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}

