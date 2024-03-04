using System.Data.Common;
using Dapper;
using Database.AspNetCoreExample.Controllers;
using Database.AspNetCoreExample.Models;
using Database.AspNetCoreExample.Services.Dapper;
using NUnit.Framework;

namespace Database.Tests.Test;

public class DapperUserServiceTests
{
    private DapperUserService _dapperUserService;

    [SetUp]
    public void SetUp() => _dapperUserService = new DapperUserService(TestsFixture.DbDataSource);

    [Test]
    public async Task Create_CorrectData_CreatesUserAndReturnId()
    {
        var userInfo = new UserCreationInfo(nameof(Create_CorrectData_CreatesUserAndReturnId));

        var result = await _dapperUserService.CreateAsync(userInfo);

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(result, Is.GreaterThan(0),
                "Идентификатор созданного пользователя не может быть меньше или равен нулю");
            Assert.That(await EnsureUserIsExists(result, userInfo.Login),
                $"Созданный пользователь с идентификатором {result} " +
                $"и логином {userInfo.Login} не найден в базе данных");
        });
    }

    [Test]
    public async Task List_WithPreparedUsers_GetAll()
    {
        const int count = 10;
        const string loginPrefix = nameof(List_WithPreparedUsers_GetAll);
        await PrepareUsers(loginPrefix, count);

        var result = await _dapperUserService.GetListAsync();

        Assert.That(result, Is.Not.Null);

        var filtered = result
            .Where(x => x.Login.StartsWith(loginPrefix))
            .ToList();
        Assert.That(filtered, Has.Count.EqualTo(count));
    }

    [Test]
    public void Create_UserInfoIsNull_Throw()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _dapperUserService.CreateAsync(null));

    [Test]
    public async Task Update_ExistedUser_Success()
    {
        const string login = nameof(Update_ExistedUser_Success);
        var userId = await PrepareUser(login);

        const string newLogin = $"{login}_UPDATED";

        await Assert.MultipleAsync(async () =>
        {
            Assert.DoesNotThrowAsync(async () => await _dapperUserService.UpdateAsync(
                new UserInfo { Login = newLogin, CreationDate = DateTime.Now, Id = userId }));
            Assert.That(!await EnsureUserIsExists(userId, login),
                "Пользователь со старым логином не должен существовать в базе данных");
            Assert.That(await EnsureUserIsExists(userId, newLogin),
                "Не найдено пользователя с обновлённым логином");
        });
    }

    [Test]
    public async Task Update_NotExistedUser_DoNothing()
    {
        const string login = nameof(Update_NotExistedUser_DoNothing);

        Assert.DoesNotThrowAsync(async () => await _dapperUserService.UpdateAsync(
            new UserInfo { Login = login, CreationDate = DateTime.Now, Id = long.MaxValue }));
        Assert.That(!await EnsureUserIsExists(login));
    }

    [Test]
    public void Update_UserInfoIsNull_Throw()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _dapperUserService.UpdateAsync(null));

    [Test]
    public async Task Delete_ExistedUser_RemoveAndReturnsUserInfo()
    {
        const string login = nameof(Delete_ExistedUser_RemoveAndReturnsUserInfo);
        var userId = await PrepareUser(login);


        await Assert.MultipleAsync(async () =>
        {
            var result = await _dapperUserService.DeleteAsync(userId);
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.Login, Is.EqualTo(login));
            Assert.That(!await EnsureUserIsExists(userId, login),
                "Удалённый пользователь не должен существовать в базе данных");
        });
    }

    [Test]
    public async Task Delete_NotExistedUser_ReturnsNull()
    {
        const long id = long.MaxValue;
        var result = await _dapperUserService.DeleteAsync(id);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Delete_InvalidUserId_Throw([Values(-1, 0)] long userId)
        => Assert.ThrowsAsync<ArgumentException>(() => _dapperUserService.DeleteAsync(userId));

    private static async Task<bool> EnsureUserIsExists(long userId, string login)
    {
        await using var connection = await TestsFixture.DbDataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<bool>(
            "SELECT EXISTS (SELECT FROM user_info WHERE id = @userId AND login = @login)",
            new { userId, login });
    }

    private static async Task<bool> EnsureUserIsExists(string login)
    {
        await using var connection = await TestsFixture.DbDataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<bool>(
            "SELECT EXISTS (SELECT FROM user_info WHERE login = @login)",
            new { login });
    }

    private static async Task PrepareUsers(string loginPrefix, int count)
    {
        await using var connection = await TestsFixture.DbDataSource.OpenConnectionAsync();
        for (var i = 0; i < count; i++)
        {
            await PrepareUser(loginPrefix + i, connection);
        }
    }

    private static async Task<long> PrepareUser(string login)
    {
        await using var connection = await TestsFixture.DbDataSource.OpenConnectionAsync();
        return await PrepareUser(login, connection);
    }

    private static async Task<long> PrepareUser(string login, DbConnection connection) =>
        await connection.QuerySingleAsync<long>("INSERT INTO user_info(login) VALUES (@Login) RETURNING id",
            new { login });
}