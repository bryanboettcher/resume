using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ResumeChat.Api.Tests;

public abstract class ChatEndpoints_MapTo
{
    protected ApiFactory Factory = null!;
    protected HttpClient Client = null!;
    protected HttpResponseMessage Response = null!;

    [OneTimeSetUp]
    public void BaseOneTimeSetUp()
    {
        Factory = new ApiFactory();
    }

    [OneTimeTearDown]
    public void BaseOneTimeTearDown()
    {
        Factory.Dispose();
    }

    [SetUp]
    public void BaseSetUp()
    {
        Client = Factory.CreateClient();
    }

    [TearDown]
    public void BaseTearDown()
    {
        Response?.Dispose();
        Client.Dispose();
    }

    public class When_health_requested_without_api_key : ChatEndpoints_MapTo
    {
        [SetUp]
        public async Task Act()
        {
            Response = await Client.GetAsync("/api/chat/health");
        }

        [Test]
        public void It_should_return_200()
        {
            Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    public class When_chat_has_no_api_key_header : ChatEndpoints_MapTo
    {
        [SetUp]
        public async Task Act()
        {
            Response = await Client.PostAsJsonAsync("/api/chat", new { Message = "hi" });
        }

        [Test]
        public void It_should_return_401()
        {
            Response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }

    public class When_chat_has_wrong_api_key : ChatEndpoints_MapTo
    {
        [SetUp]
        public async Task Act()
        {
            Client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");
            Response = await Client.PostAsJsonAsync("/api/chat", new { Message = "hi" });
        }

        [Test]
        public void It_should_return_401()
        {
            Response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }

    public class When_chat_has_valid_api_key : ChatEndpoints_MapTo
    {
        private string _body = null!;

        [SetUp]
        public async Task Act()
        {
            Client.DefaultRequestHeaders.Add("X-Api-Key", ApiFactory.TestApiKey);
            Response = await Client.PostAsJsonAsync("/api/chat", new { Message = "hello world" });
            _body = await Response.Content.ReadAsStringAsync();
        }

        [Test]
        public void It_should_return_200()
        {
            Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Test]
        public void It_should_return_event_stream()
        {
            Response.Content.Headers.ContentType.ShouldNotBeNull();
            Response.Content.Headers.ContentType.MediaType.ShouldBe("text/event-stream");
        }

        [Test]
        public void It_should_stream_first_word()
        {
            _body.ShouldContain("data: hello \n\n");
        }

        [Test]
        public void It_should_stream_second_word()
        {
            _body.ShouldContain("data: world \n\n");
        }

        [Test]
        public void It_should_terminate_with_done()
        {
            _body.ShouldContain("data: [DONE]\n\n");
        }
    }

    public class When_api_key_not_configured : ChatEndpoints_MapTo
    {
        [Test]
        public void It_should_fail_startup()
        {
            Should.Throw<Exception>(() =>
            {
                using var noKeyFactory = new WebApplicationFactory<Program>()
                    .WithWebHostBuilder(builder => builder.UseSetting("ApiKey:Key", ""));

                _ = noKeyFactory.CreateClient();
            });
        }
    }
}
