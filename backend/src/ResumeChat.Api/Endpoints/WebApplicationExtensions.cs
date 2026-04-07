namespace ResumeChat.Api.Endpoints;

public static class WebApplicationExtensions
{
    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        ChatEndpoints.MapTo(app);
        IngestionEndpoints.MapTo(app);
        CorpusSyncEndpoints.MapTo(app);
        InteractionEndpoints.MapTo(app);

        if (app.Environment.IsDevelopment())
            DebugRetrievalEndpoints.MapTo(app);

        return app;
    }
}
