namespace BudgetFlow.Domain.Entities;

public sealed class Wallet
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Currency { get; private set; } = string.Empty;

    public decimal Balance { get; private set; }

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public bool IsEmpty => Balance == 0m;

    public User User { get; private set; } = null!;
    public ICollection<Transaction> Transactions { get; private set; } = new List<Transaction>();

    private Wallet()
    {
    }

    public Wallet(Guid userId, string name, string currency, decimal initialBalance = 0m)
    {
        UserId = userId;
        Name = name;
        Currency = currency.ToUpperInvariant();
        Balance = initialBalance;
    }

    public void Rename(string name)
    {
        Name = name;
    }

    public void ChangeCurrency(string currency)
    {
        Currency = currency.ToUpperInvariant();
    }

    public void SetBalance(decimal balance)
    {
        Balance = balance;
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        Balance += amount;
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        if (amount > Balance)
        {
            throw new InvalidOperationException("Insufficient wallet balance.");
        }

        Balance -= amount;
    }
}
