using AnalyzerCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnalyzerCore.Application.Tests.Common;

/// <summary>
/// Factory for creating in-memory database contexts for testing.
/// </summary>
public static class TestDbContextFactory
{
    public static ApplicationDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    public static async Task<ApplicationDbContext> CreateWithDataAsync(
        string? databaseName = null,
        Func<ApplicationDbContext, Task>? seedAction = null)
    {
        var context = Create(databaseName);

        if (seedAction is not null)
        {
            await seedAction(context);
            await context.SaveChangesAsync();
        }

        return context;
    }
}
