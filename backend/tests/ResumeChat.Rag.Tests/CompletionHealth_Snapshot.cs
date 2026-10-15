using ResumeChat.Rag.Response;

namespace ResumeChat.Rag.Tests;

public abstract class CompletionHealth_Snapshot
{
    protected ManualTimeProvider Time = null!;
    protected CompletionHealth Subject = null!;
    protected CompletionHealthSnapshot Result = null!;

    [SetUp]
    public void BaseSetUp()
    {
        Time = new ManualTimeProvider();
        Subject = new CompletionHealth(Time);

        Arrange();

        Result = Subject.Snapshot();
    }

    protected virtual void Arrange() { }

    public class When_no_completion_recorded : CompletionHealth_Snapshot
    {
        [Test]
        public void It_should_be_healthy_with_no_history()
        {
            Result.ShouldBe(new CompletionHealthSnapshot(true, null, null, null));
        }
    }

    public class When_latest_completion_failed : CompletionHealth_Snapshot
    {
        protected override void Arrange()
        {
            Subject.RecordSuccess();
            Time.Now = Time.Now.AddMinutes(1);
            Subject.RecordFailure(new HttpRequestException("credit balance is too low"));
        }

        [Test]
        public void It_should_be_unhealthy()
        {
            Result.Healthy.ShouldBeFalse();
        }

        [Test]
        public void It_should_record_failure_time()
        {
            Result.LastFailure.ShouldBe(Time.Now);
        }

        [Test]
        public void It_should_record_exception_type_as_last_error()
        {
            Result.LastError.ShouldBe(nameof(HttpRequestException));
        }
    }

    public class When_success_follows_failure : CompletionHealth_Snapshot
    {
        protected override void Arrange()
        {
            Subject.RecordFailure(new InvalidOperationException());
            Time.Now = Time.Now.AddMinutes(1);
            Subject.RecordSuccess();
        }

        [Test]
        public void It_should_recover_to_healthy()
        {
            Result.Healthy.ShouldBeTrue();
        }
    }

    public class When_failure_carries_upstream_message : CompletionHealth_Snapshot
    {
        protected override void Arrange()
        {
            Subject.RecordFailure(new HttpRequestException("Your credit balance is too low"));
        }

        [Test]
        public void It_should_expose_exception_type_only()
        {
            Result.LastError.ShouldBe(nameof(HttpRequestException));
        }
    }

    protected sealed class ManualTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
