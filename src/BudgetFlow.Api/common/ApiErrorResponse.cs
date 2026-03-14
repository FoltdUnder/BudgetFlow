namespace BudgetFlow.Api.Common;

public sealed class ApiErrorResponse
{
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? TraceId { get; init; }
    public Dictionary<string, string[]>? Errors { get; init; }
}