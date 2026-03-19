using BudgetFlow.Application.AuditLogs;
using BudgetFlow.Application.Authentication;
using BudgetFlow.Application.Categories;
using BudgetFlow.Application.Dashboard;
using BudgetFlow.Application.Transactions;
using BudgetFlow.Application.Users;
using BudgetFlow.Application.Wallets;
using BudgetFlow.Infrastructure.AuditLogs;
using BudgetFlow.Infrastructure.Auth;
using BudgetFlow.Infrastructure.Categories;
using BudgetFlow.Infrastructure.Dashboard;
using BudgetFlow.Infrastructure.Persistence;
using BudgetFlow.Infrastructure.Transactions;
using BudgetFlow.Infrastructure.Users;
using BudgetFlow.Infrastructure.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<DemoDataOptions>(
            configuration.GetSection(DemoDataOptions.SectionName));

        services.AddScoped<AppDbInitializer>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IUserDashboardService, UserDashboardService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<ITransactionService, TransactionService>();

        return services;
    }
}
