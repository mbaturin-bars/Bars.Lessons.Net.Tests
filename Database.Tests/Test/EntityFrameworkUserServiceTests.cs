using Database.AspNetCoreExample.Controllers;
using Database.AspNetCoreExample.Models;
using Database.AspNetCoreExample.Services.EntityFramework;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Database.Tests.Test;

public class EntityFrameworkUserServiceTests
{
    private EntityFrameworkUserService _efUserService;

    [SetUp]
    public void SetUp() => _efUserService = new EntityFrameworkUserService(TestsFixture.UserDbContext);

    [Test]
    public async Task Create_CorrectData_CreatesUserAndReturnId()
    {
        var userInfo = new UserCreationInfo(nameof(Create_CorrectData_CreatesUserAndReturnId));

        var result = await _efUserService.CreateAsync(userInfo);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.GreaterThan(0),
                "Идентификатор созданного пользователя не может быть меньше или равен нулю");
            Assert.That(TestsFixture.UserDbContext.Users.Any(x => x.Id == result && x.Login == userInfo.Login),
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

        var result = await _efUserService.GetListAsync();

        Assert.That(result, Is.Not.Null);

        var filtered = result
            .Where(x => x.Login.StartsWith(loginPrefix))
            .ToList();
        Assert.That(filtered, Has.Count.EqualTo(count));
    }

    [Test]
    public void Create_UserInfoIsNull_Throw()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _efUserService.CreateAsync(null!));

    [Test]
    public async Task Update_NotExistedUser_DoNothing()
    {
        const string login = nameof(Update_NotExistedUser_DoNothing);

        Assert.DoesNotThrowAsync(async () => await _efUserService.UpdateAsync(
            new UserInfo { Login = login, CreationDate = DateTime.Now, Id = long.MaxValue }));
        Assert.That(!await EnsureUserIsExists(login));
    }


    [Test]
    public void Update_UserInfoIsNull_Throw()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _efUserService.UpdateAsync(null!));

    [Test]
    public async Task Delete_ExistedUser_RemoveAndReturnsUserInfo()
    {
        const string login = nameof(Delete_ExistedUser_RemoveAndReturnsUserInfo);
        var userId = await PrepareUser(login);


        await Assert.MultipleAsync(async () =>
        {
            var result = await _efUserService.DeleteAsync(userId);
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
        var result = await _efUserService.DeleteAsync(id);
        Assert.That(result, Is.Null);
    }

    /// <param name="userId"></param>
    [Test]
    public void Delete_InvalidUserId_Throw([Values(-1, 0)] long userId)
        => Assert.ThrowsAsync<ArgumentException>(() => _efUserService.DeleteAsync(userId));

    private static Task<bool> EnsureUserIsExists(long userId, string login)
        => TestsFixture.UserDbContext.Users.AnyAsync(x => x.Id == userId && x.Login == login);

    private static Task<bool> EnsureUserIsExists(string login)
        => TestsFixture.UserDbContext.Users.AnyAsync(x => x.Login == login);


    private static async Task PrepareUsers(string loginPrefix, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await PrepareUser(loginPrefix + count);
        }
    }

    private static async Task<long> PrepareUser(string login)
    {
        var userInfo = await TestsFixture.UserDbContext.Users.AddAsync(
            new UserInfo { Login = login });
        await TestsFixture.UserDbContext.SaveChangesAsync();
        return userInfo.Entity.Id;
    }
}