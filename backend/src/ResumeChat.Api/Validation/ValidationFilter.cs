using FluentValidation;

namespace ResumeChat.Api.Validation;

public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.Arguments.FirstOrDefault(a => a is T) is not T argument)
            return await next(context).ConfigureAwait(false);

        var result = await validator.ValidateAsync(argument).ConfigureAwait(false);
        if (!result.IsValid)
            return Results.BadRequest(result.Errors.Select(e => e.ErrorMessage));

        return await next(context).ConfigureAwait(false);
    }
}
