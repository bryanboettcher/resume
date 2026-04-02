using Microsoft.EntityFrameworkCore;

namespace ResumeChat.Corpus.Cli;

sealed class CorpusDbContext(DbContextOptions<CorpusDbContext> options) : DbContext(options)
{
    public DbSet<SourceFileEntity> SourceFiles => Set<SourceFileEntity>();
    public DbSet<FileAnalysisEntity> FileAnalyses => Set<FileAnalysisEntity>();
    public DbSet<FileTagEntity> FileTags => Set<FileTagEntity>();
    public DbSet<FileRelationshipEntity> FileRelationships => Set<FileRelationshipEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SourceFileEntity>(e =>
        {
            e.ToTable("source_files");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
            e.Property(x => x.Repo).HasColumnName("repo").IsRequired();
            e.Property(x => x.Branch).HasColumnName("branch").IsRequired().HasDefaultValue("main");
            e.Property(x => x.FilePath).HasColumnName("file_path").IsRequired();
            e.Property(x => x.Language).HasColumnName("language");
            e.Property(x => x.ContentText).HasColumnName("content_text").IsRequired();
            e.Property(x => x.ContentHash).HasColumnName("content_hash").IsRequired();
            e.Property(x => x.LineCount).HasColumnName("line_count");
            e.Property(x => x.SizeBytes).HasColumnName("size_bytes");
            e.Property(x => x.ScannedAt).HasColumnName("scanned_at").HasDefaultValueSql("NOW()");
            e.HasIndex(x => new { x.Repo, x.Branch }).HasDatabaseName("idx_source_files_repo_branch");
            e.HasIndex(x => x.Language).HasDatabaseName("idx_source_files_language");
            e.HasIndex(x => x.ContentHash).HasDatabaseName("idx_source_files_content_hash");
            e.HasAlternateKey(x => new { x.Repo, x.Branch, x.FilePath });
        });

        modelBuilder.Entity<FileAnalysisEntity>(e =>
        {
            e.ToTable("file_analysis");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
            e.Property(x => x.SourceFileId).HasColumnName("source_file_id");
            e.Property(x => x.Analyzer).HasColumnName("analyzer").IsRequired();
            e.Property(x => x.AnalysisType).HasColumnName("analysis_type").IsRequired();
            e.Property(x => x.ContentText).HasColumnName("content_text").IsRequired();
            e.Property(x => x.AnalyzedAt).HasColumnName("analyzed_at").HasDefaultValueSql("NOW()");
            e.HasIndex(x => new { x.Analyzer, x.AnalysisType }).HasDatabaseName("idx_file_analysis_type");
            e.HasOne<SourceFileEntity>().WithMany().HasForeignKey(x => x.SourceFileId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FileTagEntity>(e =>
        {
            e.ToTable("file_tags");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
            e.Property(x => x.SourceFileId).HasColumnName("source_file_id");
            e.Property(x => x.Tag).HasColumnName("tag").IsRequired();
            e.Property(x => x.Confidence).HasColumnName("confidence");
            e.Property(x => x.Analyzer).HasColumnName("analyzer").IsRequired();
            e.HasIndex(x => x.Tag).HasDatabaseName("idx_file_tags_tag");
            e.HasOne<SourceFileEntity>().WithMany().HasForeignKey(x => x.SourceFileId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FileRelationshipEntity>(e =>
        {
            e.ToTable("file_relationships");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
            e.Property(x => x.SourceFileId).HasColumnName("source_file_id");
            e.Property(x => x.RelatedFileId).HasColumnName("related_file_id");
            e.Property(x => x.RelationshipType).HasColumnName("relationship_type").IsRequired();
            e.HasOne<SourceFileEntity>().WithMany().HasForeignKey(x => x.SourceFileId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<SourceFileEntity>().WithMany().HasForeignKey(x => x.RelatedFileId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

sealed class SourceFileEntity
{
    public long Id { get; set; }
    public string Repo { get; set; } = string.Empty;
    public string Branch { get; set; } = "main";
    public string FilePath { get; set; } = string.Empty;
    public string? Language { get; set; }
    public string ContentText { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public int SizeBytes { get; set; }
    public DateTimeOffset ScannedAt { get; set; }
}

sealed class FileAnalysisEntity
{
    public long Id { get; set; }
    public long SourceFileId { get; set; }
    public string Analyzer { get; set; } = string.Empty;
    public string AnalysisType { get; set; } = string.Empty;
    public string ContentText { get; set; } = string.Empty;
    public DateTimeOffset AnalyzedAt { get; set; }
}

sealed class FileTagEntity
{
    public long Id { get; set; }
    public long SourceFileId { get; set; }
    public string Tag { get; set; } = string.Empty;
    public float? Confidence { get; set; }
    public string Analyzer { get; set; } = string.Empty;
}

sealed class FileRelationshipEntity
{
    public long Id { get; set; }
    public long SourceFileId { get; set; }
    public long RelatedFileId { get; set; }
    public string RelationshipType { get; set; } = string.Empty;
}
