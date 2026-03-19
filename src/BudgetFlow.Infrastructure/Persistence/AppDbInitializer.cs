using BudgetFlow.Domain.Entities;
using BudgetFlow.Domain.Types;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BudgetFlow.Infrastructure.Persistence;

public sealed class AppDbInitializer
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AppDbInitializer> _logger;

    public AppDbInitializer(
        AppDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        ILogger<AppDbInitializer> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);

        if (await _dbContext.Users.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Skipping demo seed because users already exist.");
            return;
        }

        var now = DateTime.UtcNow;

        var admin = CreateUser("Admin Demo", "admin@budgetflow.local", "Admin123!", Roles.Admin);
        var alice = CreateUser("Alice Carter", "alice@budgetflow.local", "User123!");
        var bob = CreateUser("Bob Stone", "bob@budgetflow.local", "User123!");

        var users = new[] { admin, alice, bob };

        var adminWallet = new Wallet(admin.Id, "Operations", "USD");
        var aliceMainWallet = new Wallet(alice.Id, "Daily Wallet", "USD");
        var aliceSavingsWallet = new Wallet(alice.Id, "Emergency Fund", "USD");
        var bobMainWallet = new Wallet(bob.Id, "Main Card", "EUR");
        var bobCashWallet = new Wallet(bob.Id, "Cash", "EUR");

        var wallets = new[]
        {
            adminWallet,
            aliceMainWallet,
            aliceSavingsWallet,
            bobMainWallet,
            bobCashWallet
        };

        var categories = new[]
        {
            CreateCategory(admin.Id, "Subscriptions", CategoryType.Expense, now),
            CreateCategory(admin.Id, "Consulting", CategoryType.Income, now),
            CreateCategory(alice.Id, "Salary", CategoryType.Income, now),
            CreateCategory(alice.Id, "Freelance", CategoryType.Income, now),
            CreateCategory(alice.Id, "Groceries", CategoryType.Expense, now),
            CreateCategory(alice.Id, "Rent", CategoryType.Expense, now),
            CreateCategory(alice.Id, "Transport", CategoryType.Expense, now),
            CreateCategory(bob.Id, "Salary", CategoryType.Income, now),
            CreateCategory(bob.Id, "Food", CategoryType.Expense, now),
            CreateCategory(bob.Id, "Travel", CategoryType.Expense, now),
            CreateCategory(bob.Id, "Bonus", CategoryType.Income, now)
        };

        var categoryMap = categories.ToDictionary(
            x => (x.UserId, x.Name, x.Type),
            x => x);

        var transactions = new[]
        {
            CreateTransaction(admin.Id, adminWallet, categoryMap[(admin.Id, "Consulting", CategoryType.Income)], CategoryType.Income, 1500m, now.AddDays(-12), "Monthly retainer"),
            CreateTransaction(admin.Id, adminWallet, categoryMap[(admin.Id, "Subscriptions", CategoryType.Expense)], CategoryType.Expense, 99m, now.AddDays(-10), "Tooling stack"),

            CreateTransaction(alice.Id, aliceMainWallet, categoryMap[(alice.Id, "Salary", CategoryType.Income)], CategoryType.Income, 3200m, now.AddDays(-15), "March salary"),
            CreateTransaction(alice.Id, aliceMainWallet, categoryMap[(alice.Id, "Groceries", CategoryType.Expense)], CategoryType.Expense, 180.45m, now.AddDays(-9), "Weekly groceries"),
            CreateTransaction(alice.Id, aliceMainWallet, categoryMap[(alice.Id, "Rent", CategoryType.Expense)], CategoryType.Expense, 1100m, now.AddDays(-7), "Apartment rent"),
            CreateTransaction(alice.Id, aliceMainWallet, categoryMap[(alice.Id, "Transport", CategoryType.Expense)], CategoryType.Expense, 62.30m, now.AddDays(-4), "Metro card top up"),
            CreateTransaction(alice.Id, aliceSavingsWallet, categoryMap[(alice.Id, "Freelance", CategoryType.Income)], CategoryType.Income, 850m, now.AddDays(-6), "Landing page project"),

            CreateTransaction(bob.Id, bobMainWallet, categoryMap[(bob.Id, "Salary", CategoryType.Income)], CategoryType.Income, 2400m, now.AddDays(-14), "Monthly salary"),
            CreateTransaction(bob.Id, bobMainWallet, categoryMap[(bob.Id, "Travel", CategoryType.Expense)], CategoryType.Expense, 420m, now.AddDays(-8), "Train tickets"),
            CreateTransaction(bob.Id, bobMainWallet, categoryMap[(bob.Id, "Food", CategoryType.Expense)], CategoryType.Expense, 95.25m, now.AddDays(-3), "Restaurant and groceries"),
            CreateTransaction(bob.Id, bobCashWallet, categoryMap[(bob.Id, "Bonus", CategoryType.Income)], CategoryType.Income, 300m, now.AddDays(-5), "Project completion bonus")
        };

        _dbContext.Users.AddRange(users);
        _dbContext.Wallets.AddRange(wallets);
        _dbContext.Categories.AddRange(categories);
        _dbContext.Transactions.AddRange(transactions);
        _dbContext.AuditLogs.Add(new AuditLog(
            admin.Id,
            "demo_seed_created",
            nameof(User),
            null,
            "Demo users, wallets, categories, and transactions were seeded."));

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded demo data for {UserCount} users.", users.Length);
    }

    private User CreateUser(string fullName, string email, string password, string role = Roles.User)
    {
        var user = new User(fullName, email, role);
        user.SetPasswordHash(_passwordHasher.HashPassword(user, password));
        return user;
    }

    private static Category CreateCategory(Guid userId, string name, CategoryType type, DateTime createdAtUtc)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Type = type,
            CreatedAtUtc = createdAtUtc
        };
    }

    private static Transaction CreateTransaction(
        Guid userId,
        Wallet wallet,
        Category category,
        CategoryType type,
        decimal amount,
        DateTime date,
        string note)
    {
        if (type == CategoryType.Income)
        {
            wallet.Deposit(amount);
        }
        else
        {
            wallet.Withdraw(amount);
        }

        return new Transaction(userId, wallet.Id, category.Id, type, amount, date, note);
    }
}
