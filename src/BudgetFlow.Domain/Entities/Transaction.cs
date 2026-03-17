using BudgetFlow.Domain.Types;

namespace BudgetFlow.Domain.Entities;

public sealed class Transaction
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid UserId { get; private set; }

    public Guid WalletId { get; private set; }

    public Guid CategoryId { get; private set; }

    public CategoryType Type { get; private set; }

    public decimal Amount { get; private set; }

    public string? Note { get; private set; }

    public DateTime Date { get; private set; }

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    public User User { get; private set; } = null!;

    public Wallet Wallet { get; private set; } = null!;

    public Category Category { get; private set; } = null!;

    private Transaction()
    {
    }

    public Transaction(
        Guid userId,
        Guid walletId,
        Guid categoryId,
        CategoryType type,
        decimal amount,
        DateTime date,
        string? note = null)
    {
        UserId = userId;
        WalletId = walletId;
        CategoryId = categoryId;
        Type = type;
        Date = date;

        SetAmount(amount);
        SetNote(note);
    }

    public void SetAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        Amount = amount;
        Touch();
    }

    public void SetNote(string? note)
    {
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        Touch();
    }

    public void SetDate(DateTime date)
    {
        Date = date;
        Touch();
    }

    public void ChangeCategory(Guid categoryId, CategoryType categoryType)
    {
        EnsureMatchingType(Type, categoryType);

        CategoryId = categoryId;
        Touch();
    }

    public void ChangeType(CategoryType type, CategoryType categoryType)
    {
        EnsureMatchingType(type, categoryType);

        Type = type;
        Touch();
    }

    public void ValidateOwnership(Guid walletUserId, Guid categoryUserId)
    {
        if (walletUserId != UserId)
        {
            throw new InvalidOperationException("Transaction wallet must belong to the same user.");
        }

        if (categoryUserId != UserId)
        {
            throw new InvalidOperationException("Transaction category must belong to the same user.");
        }
    }

    public void ValidateCategoryType(CategoryType categoryType)
    {
        EnsureMatchingType(Type, categoryType);
    }

    private static void EnsureMatchingType(CategoryType transactionType, CategoryType categoryType)
    {
        if (transactionType != categoryType)
        {
            throw new InvalidOperationException("Category type must match transaction type.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
