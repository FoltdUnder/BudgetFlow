namespace BudgetFlow.Api.Common;

public sealed class ApiErrorResponse
{
    public string Message { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public int Status { get; init; }
}
