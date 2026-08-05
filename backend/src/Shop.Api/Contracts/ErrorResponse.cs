namespace Shop.Api.Contracts;

/// <summary>
/// Wraps error messages so they serialize as JSON. Returning a bare string from an action
/// (e.g. BadRequest("...")) hits ASP.NET Core's built-in StringOutputFormatter, which writes
/// it as text/plain instead of JSON — surprising for any client expecting a JSON body.
/// </summary>
public record ErrorResponse(string Error);

/// <summary>
/// Returned (with 202 Accepted) when a payment hasn't resolved yet — not rejected, just not
/// confirmed on-chain. Kept distinct from ErrorResponse so a poller doesn't have to guess
/// whether an "error" field means "keep trying" or "stop".
/// </summary>
public record PendingResponse(string Reason)
{
    public string Status => "pending";
}
