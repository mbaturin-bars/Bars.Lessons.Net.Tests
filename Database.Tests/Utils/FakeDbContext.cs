using Database.AspNetCoreExample.Services.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Database.Tests.Utils;

/// <summary>
/// Фейковый DbContext для эмуляции работы UserDbContext.
/// </summary>
/// <param name="options"></param>
public class FakeDbContext(DbContextOptions options) : UserDbContext(null!, options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //do nothing 
    }

    public static FakeDbContext Create()
    {
        var contextOptions = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase("FakeDbContextTest")
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new FakeDbContext(contextOptions);
    }
}