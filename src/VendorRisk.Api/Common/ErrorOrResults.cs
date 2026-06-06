using ErrorOr;

namespace VendorRisk.Api.Common;

/// <summary>
/// Maps an <see cref="ErrorOr{T}"/> to an HTTP result, so endpoints can stay one-liners.
/// </summary>
public static class ErrorOrResults
{
    /// <summary>Returns 200 with the value on success, or a problem response derived from the errors.</summary>
    public static IResult ToApiResult<T>(this ErrorOr<T> result) =>
        result.Match(value => Results.Ok(value), ToProblem);

    /// <summary>As above, but lets the caller choose the success result (e.g. 201 Created).</summary>
    public static IResult ToApiResult<T>(this ErrorOr<T> result, Func<T, IResult> onSuccess) =>
        result.Match(onSuccess, ToProblem);

    private static IResult ToProblem(List<Error> errors)
    {
        if (errors.Count == 0)
        {
            return Results.Problem();
        }

        // All validation errors → 400 with a code → messages dictionary.
        if (errors.TrueForAll(e => e.Type == ErrorType.Validation))
        {
            Dictionary<string, string[]> failures = errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

            return Results.ValidationProblem(failures);
        }

        Error first = errors[0];
        int status = first.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Results.Problem(detail: first.Description, statusCode: status, title: first.Code);
    }
}
