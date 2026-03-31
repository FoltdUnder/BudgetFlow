namespace BudgetFlow.Application.Common.Exceptions;

public sealed class ExpiredRefreshTokenException : Exception
{
    public ExpiredRefreshTokenException(string message) : base(message)
    {
    }
}
