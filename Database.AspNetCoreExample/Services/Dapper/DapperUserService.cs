using System.Data;
using System.Data.Common;
using Dapper;
using Database.AspNetCoreExample.Controllers;
using Database.AspNetCoreExample.Models;

namespace Database.AspNetCoreExample.Services.Dapper;

/// <summary>
/// Сервис, позволяющий работать с пользователями в базе данных (реализация через Dapper).
/// </summary>
internal class DapperUserService : IUserService
{
    private readonly DbDataSource _dataSource;

    /// <summary>
    /// Создать экземпляр типа <see cref="DapperUserService"/>.
    /// </summary>
    /// <param name="dataSource">Источник соединений для БД PostgreSQL.</param>
    public DapperUserService(DbDataSource dataSource) => _dataSource = dataSource;

    /// <inheritdoc />
    public async Task<UserInfo?> GetAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await GetUserInternalAsync(userId, connection, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<UserInfo>> GetListAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        const string commandText = """
                                   SELECT
                                       id as "Id",
                                       login as "Login",
                                       created_on as "CreationDate" 
                                   FROM user_info
                                   """;

        var result =
            await connection.QueryAsync<UserInfo>(new CommandDefinition(commandText,
                cancellationToken: cancellationToken));
        return result.AsList();
    }

    /// <inheritdoc />
    public Task<long> CreateAsync(UserCreationInfo userInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userInfo);
        return CreateInternal(userInfo, cancellationToken);
    }

    private async Task<long> CreateInternal(UserCreationInfo userInfo, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        const string commandText = "INSERT INTO user_info(login) VALUES (@Login) RETURNING id";
        return await connection.QueryFirstOrDefaultAsync<long>(
            new CommandDefinition(commandText, userInfo, cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public Task UpdateAsync(UserInfo userInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userInfo);
        return UpdateInternal(userInfo, cancellationToken);
    }

    private async Task UpdateInternal(UserInfo userInfo, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        const string commandText = """
                                   UPDATE user_info SET
                                       login = @Login,
                                       created_on = @CreationDate 
                                   WHERE id = @Id
                                   """;
        await connection.ExecuteAsync(
            new CommandDefinition(commandText, userInfo, cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public Task<UserInfo?> DeleteAsync(long userId, CancellationToken cancellationToken = default)
        => userId <= 0
            ? throw new ArgumentException("", nameof(userId))
            : DeleteInternal(userId, cancellationToken);

    private async Task<UserInfo?> DeleteInternal(long userId, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        var userInfo = await GetUserInternalAsync(userId, connection, cancellationToken);

        if (userInfo == null)
        {
            return null;
        }

        const string commandText = "DELETE FROM user_info WHERE id = @userId";
        await connection.ExecuteAsync(
            new CommandDefinition(commandText, new { userId }, cancellationToken: cancellationToken));

        return userInfo;
    }


    /// <summary>
    /// Внутренний метод для получения информации о пользователе.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="connection">Соединение с базой данных.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// Информация о пользователе в виде <see cref="UserInfo"/>.
    /// Если пользователь не найден - возвращает <c>null</c>.
    /// </returns>
    private static async Task<UserInfo?> GetUserInternalAsync(long userId, IDbConnection connection,
        CancellationToken cancellationToken = default)
    {
        const string commandText = """
                                   SELECT
                                       id as "Id",
                                       login as "Login",
                                       created_on as "CreationDate" 
                                   FROM user_info
                                   WHERE id = @userId
                                   """;

        return await connection.QueryFirstOrDefaultAsync<UserInfo>(
            new CommandDefinition(commandText, new { userId }, cancellationToken: cancellationToken));
    }
}