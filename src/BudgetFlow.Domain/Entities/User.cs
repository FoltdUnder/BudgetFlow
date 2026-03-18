namespace BudgetFlow.Domain.Entities;

public sealed class User
{
    public ICollection<AuditLog> AuditLogs { get; private set; } = new List<AuditLog>();
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();
    public ICollection<Wallet> Wallets { get; private set; } = new List<Wallet>();
    public ICollection<Category> Categories { get; private set; } = new List<Category>();
    public ICollection<Transaction> Transactions { get; private set; } = new List<Transaction>();
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string FullName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string Role { get; private set; } = Roles.User;

    public bool IsBlocked { get; private set; }

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private User()
    {
    }

    public User(string fullName, string email, string role = Roles.User)
    {
        FullName = fullName;
        Email = email;
        Role = role;
    }

    public void Block()
    {
        IsBlocked = true;
    }

    public void Unblock()
    {
        IsBlocked = false;
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    public void ChangeRole(string role)
    {
        Role = role;
    }
}
