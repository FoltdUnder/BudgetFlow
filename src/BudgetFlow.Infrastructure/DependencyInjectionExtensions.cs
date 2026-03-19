using BudgetFlow.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetFlow.Infrastructure;

public static class DependencyInjectionExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<AppDbInitializer>();
        await initializer.InitializeAsync(cancellationToken);
    }
}
