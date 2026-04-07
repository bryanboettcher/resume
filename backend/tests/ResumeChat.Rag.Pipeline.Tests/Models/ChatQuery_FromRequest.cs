using ResumeChat.Rag.Models;
using Shouldly;

namespace ResumeChat.Rag.Pipeline.Tests.Models;

public abstract class ChatQuery_FromRequest
{
    public class When_created_from_request : ChatQuery_FromRequest
    {
        private ChatQuery _result = null!;

        [SetUp]
        public void SetUp()
        {
            var request = new ChatRequest("Tell me about Bryan's Kubernetes work");
            _result = ChatQuery.FromRequest(request);
        }

        [Test]
        public void OriginalMessage_equals_request_message()
            => _result.OriginalMessage.ShouldBe("Tell me about Bryan's Kubernetes work");

        [Test]
        public void ProcessedMessage_equals_request_message()
            => _result.ProcessedMessage.ShouldBe("Tell me about Bryan's Kubernetes work");

        [Test]
        public void MinScore_is_null()
            => _result.MinScore.ShouldBeNull();
    }
}
