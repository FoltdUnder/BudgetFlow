namespace BudgetFlow.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public bool IsRevoked { get; private set; }

    public User User { get; private set; } = null!;

    private RefreshToken() { }

    public RefreshToken(Guid userId, string token, DateTime expiresAtUtc)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
        IsRevoked = false;
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke()
    {
        IsRevoked = true;
    }
}