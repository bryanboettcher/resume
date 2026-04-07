using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ResumeChat.Storage;

public sealed class MigrationHostedService : IHostedService
{
    private readonly IDbContextFactory<ResumeChatDbContext> _factory;
    private readonly ILogger<MigrationHostedService> _logger;

    public MigrationHostedService(
        IDbContextFactory<ResumeChatDbContext> factory,
        ILogger<MigrationHostedService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        const int maxRetries = 10;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await using var context = await _factory.CreateDbContextAsync(ct);
                _logger.LogInformation("Applying database schema (attempt {Attempt})...", attempt);
                await context.Database.EnsureCreatedAsync(ct);
                _logger.LogInformation("Database schema ready");
                return;
            }
            catch (Exception ex) when (attempt < maxRetries && !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Database not ready (attempt {Attempt}/{MaxRetries}), retrying in 2s...",
                    attempt, maxRetries);
                await Task.Delay(2000, ct);
            }
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
