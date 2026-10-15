using ResumeChat.Rag.Response;

namespace ResumeChat.Rag.Tests;

[TestFixture]
public sealed class CompletionHealthTests
{
    private sealed class ManualTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Test]
    public void Snapshot_IsHealthy_BeforeAnyCompletion()
    {
        var health = new CompletionHealth(new ManualTimeProvider());

        health.Snapshot().ShouldBe(new CompletionHealthSnapshot(true, null, null, null));
    }

    [Test]
    public void Snapshot_IsUnhealthy_WhenLatestCompletionFailed()
    {
        var time = new ManualTimeProvider();
        var health = new CompletionHealth(time);

        health.RecordSuccess();
        time.Now = time.Now.AddMinutes(1);
        health.RecordFailure(new HttpRequestException("credit balance is too low"));

        var snapshot = health.Snapshot();
        snapshot.Healthy.ShouldBeFalse();
        snapshot.LastFailure.ShouldBe(time.Now);
        snapshot.LastError.ShouldBe(nameof(HttpRequestException));
    }

    [Test]
    public void Snapshot_RecoversToHealthy_AfterSubsequentSuccess()
    {
        var time = new ManualTimeProvider();
        var health = new CompletionHealth(time);

        health.RecordFailure(new InvalidOperationException());
        time.Now = time.Now.AddMinutes(1);
        health.RecordSuccess();

        health.Snapshot().Healthy.ShouldBeTrue();
    }

    [Test]
    public void LastError_ExposesExceptionTypeOnly_NotUpstreamMessage()
    {
        var health = new CompletionHealth(new ManualTimeProvider());

        health.RecordFailure(new HttpRequestException("Your credit balance is too low"));

        health.Snapshot().LastError.ShouldBe(nameof(HttpRequestException));
    }
}
