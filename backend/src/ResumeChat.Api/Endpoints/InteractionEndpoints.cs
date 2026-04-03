using ResumeChat.Storage.Repositories;

namespace ResumeChat.Api.Endpoints;

public static class InteractionEndpoints
{
    public static void MapTo(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/interactions", HandleList)
            .Produces(StatusCodes.Status200OK);

        app.MapGet("/api/admin/interactions/{id:long}", HandleGet)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/admin/interactions/search", HandleSearch)
            .Produces(StatusCodes.Status200OK);

        app.MapDelete("/api/admin/interactions/{id:long}", HandlePurge)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/admin/interactions/{id:long}/expire", HandleExpire)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleList(
        int limit = 20,
        IInteractionRepository? interactions = null,
        CancellationToken ct = default)
    {
        var results = await interactions!.GetRecentAsync(Math.Clamp(limit, 1, 100), ct).ConfigureAwait(false);
        return Results.Ok(results.Select(FormatInteraction));
    }

    private static async Task<IResult> HandleGet(
        long id,
        IInteractionRepository? interactions = null,
        CancellationToken ct = default)
    {
        var interaction = await interactions!.GetByIdAsync(id, ct).ConfigureAwait(false);
        return interaction is null ? Results.NotFound() : Results.Ok(FormatInteraction(interaction));
    }

    private static async Task<IResult> HandleSearch(
        string query,
        int limit = 20,
        IInteractionRepository? interactions = null,
        CancellationToken ct = default)
    {
        var results = await interactions!.SearchAsync(query, Math.Clamp(limit, 1, 100), ct).ConfigureAwait(false);
        return Results.Ok(results.Select(FormatInteraction));
    }

    private static async Task<IResult> HandlePurge(
        long id,
        IInteractionRepository? interactions = null,
        CancellationToken ct = default)
    {
        return await interactions!.PurgeAsync(id, ct).ConfigureAwait(false)
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static async Task<IResult> HandleExpire(
        long id,
        IInteractionRepository? interactions = null,
        CancellationToken ct = default)
    {
        return await interactions!.ExpireAsync(id, ct).ConfigureAwait(false)
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static object FormatInteraction(Storage.Entities.InteractionEntity e) => new
    {
        e.Id,
        e.OriginalQuery,
        e.ProcessedQuery,
        e.ResponseText,
        e.RetrievedDocuments,
        e.RetrievalMs,
        e.CompletionMs,
        e.TotalMs,
        e.Provider,
        e.ModelName,
        e.IsThreat,
        e.ThreatScore,
        e.QueryHash,
        e.CacheHit,
        e.CreatedAt,
        e.ExpiresAt
    };
}
