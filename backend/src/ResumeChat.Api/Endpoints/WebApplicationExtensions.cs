namespace ResumeChat.Api.Endpoints;

public static class WebApplicationExtensions
{
    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        ChatEndpoints.MapTo(app);
        return app;
    }
}
