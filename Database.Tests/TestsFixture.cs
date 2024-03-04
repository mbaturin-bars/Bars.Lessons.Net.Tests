using System.Data.Common;
using Dapper;
using Database.AspNetCoreExample.Services.EntityFramework;
using Database.Tests.Utils;
using Npgsql;
using NUnit.Framework;

namespace Database.Tests;

[SetUpFixture]
public static class TestsFixture
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Username=postgres;Password=postgres;Database=test-db";
    public static DbDataSource DbDataSource { get; private set; } = null!;
    public static UserDbContext UserDbContext { get; } =  FakeDbContext.Create();

    [OneTimeSetUp]
    public static async Task BaseSetUp()
    {
        Console.WriteLine($"Используется строка подключения {ConnectionString}");
        DbDataSource = NpgsqlDataSource.Create(ConnectionString);
        await RecreateTestTables();
    }

    [OneTimeTearDown]
    public static  async Task BaseTearDown()
    {
        await DbDataSource.DisposeAsync();
        await UserDbContext.DisposeAsync();
    }

    private static async Task RecreateTestTables()
    {
        await using var connection = await DbDataSource.OpenConnectionAsync();
        const string createTableCommandText = """
                                              DROP TABLE IF EXISTS user_info;
                                              CREATE TABLE IF NOT EXISTS user_info (
                                                  id bigserial PRIMARY KEY, 
                                                  login VARCHAR(100) NOT NULL, 
                                                  created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now()
                                              )
                                              """;

        await connection.ExecuteAsync(createTableCommandText);
    }
}