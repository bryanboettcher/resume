using Microsoft.EntityFrameworkCore;
using ResumeChat.Storage.Entities;

namespace ResumeChat.Storage;

public sealed class ResumeChatDbContext(DbContextOptions<ResumeChatDbContext> options) : DbContext(options)
{
    public DbSet<InteractionEntity> Interactions => Set<InteractionEntity>();
    public DbSet<CorpusDocumentEntity> CorpusDocuments => Set<CorpusDocumentEntity>();
    public DbSet<CorpusChunkEntity> CorpusChunks => Set<CorpusChunkEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InteractionEntity>(entity =>
        {
            entity.ToTable("interactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.OriginalQuery).HasColumnName("original_query");
            entity.Property(e => e.ProcessedQuery).HasColumnName("processed_query");
            entity.Property(e => e.ResponseText).HasColumnName("response_text");
            entity.Property(e => e.RetrievedDocuments).HasColumnName("retrieved_documents").HasColumnType("jsonb");
            entity.Property(e => e.RetrievalMs).HasColumnName("retrieval_ms");
            entity.Property(e => e.CompletionMs).HasColumnName("completion_ms");
            entity.Property(e => e.TotalMs).HasColumnName("total_ms");
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.ModelName).HasColumnName("model_name");
            entity.Property(e => e.IsThreat).HasColumnName("is_threat");
            entity.Property(e => e.ThreatScore).HasColumnName("threat_score");
            entity.Property(e => e.QueryHash).HasColumnName("query_hash");
            entity.Property(e => e.CacheHit).HasColumnName("cache_hit");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");

            entity.HasIndex(e => e.QueryHash).HasDatabaseName("ix_interactions_query_hash");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_interactions_created_at");
        });

        modelBuilder.Entity<CorpusDocumentEntity>(entity =>
        {
            entity.ToTable("corpus_documents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.SourceFile).HasColumnName("source_file");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.ContentText).HasColumnName("content_text");
            entity.Property(e => e.ContentHash).HasColumnName("content_hash");
            entity.Property(e => e.Tags).HasColumnName("tags");
            entity.Property(e => e.LastModified).HasColumnName("last_modified");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.SourceFile).IsUnique().HasDatabaseName("ix_corpus_documents_source_file");
            entity.HasIndex(e => e.ContentHash).HasDatabaseName("ix_corpus_documents_content_hash");
        });

        modelBuilder.Entity<CorpusChunkEntity>(entity =>
        {
            entity.ToTable("corpus_chunks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.ChunkIndex).HasColumnName("chunk_index");
            entity.Property(e => e.SectionHeading).HasColumnName("section_heading");
            entity.Property(e => e.ChunkText).HasColumnName("chunk_text");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Document)
                  .WithMany(d => d.Chunks)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex })
                  .IsUnique()
                  .HasDatabaseName("ix_corpus_chunks_document_id_chunk_index");
        });
    }
}
