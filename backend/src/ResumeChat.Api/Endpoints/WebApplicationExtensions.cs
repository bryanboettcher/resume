namespace ResumeChat.Api.Endpoints;

public static class WebApplicationExtensions
{
    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        ChatEndpoints.MapTo(app);
        IngestionEndpoints.MapTo(app);
        return app;
    }
}
