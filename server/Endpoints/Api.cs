namespace Leaf.Endpoints;

public static class Api
{
    public static IResult Error(string message, int status = StatusCodes.Status400BadRequest) =>
        Results.Json(new { error = message }, statusCode: status);
}
