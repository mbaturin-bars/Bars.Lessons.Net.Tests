using Database.AspNetCoreExample.Controllers;
using Database.AspNetCoreExample.Models;
using Database.AspNetCoreExample.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Database.Tests.Test;

public class UserControllerTests
{
    [Test]
    public async Task List_JustReturnList()
    {
        var records = new List<UserInfo>
        {
            new()
            {
                Login = "test", Id = long.MaxValue, CreationDate = DateTime.Now
            }
        };
        var userService = Substitute.For<IUserService>();

        userService
            .GetListAsync(Arg.Is<CancellationToken>(o => o.IsCancellationRequested))
            .ThrowsAsync<TaskCanceledException>();

        userService
            .GetListAsync(Arg.Is<CancellationToken>(o => !o.IsCancellationRequested))
            .Returns(_ => records);

        var controller = new UserController(userService);
        var result = await controller.GetListAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(userService.ReceivedCalls().ToList(), Has.Count.EqualTo(1));
            Assert.That(result, Is.Not.Null);
        });
        Assert.That(result, Has.Count.EqualTo(records.Count).And.EquivalentTo(records));
    }

    [Test]
    public async Task Get_WithExistedUser_ReturnsOk()
    {
        var userInfo = new UserInfo { Login = "test", Id = long.MaxValue, CreationDate = DateTime.Now };
        var userService = Substitute.For<IUserService>();
        userService
            .GetAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => userInfo);

        var controller = new UserController(userService);
        var result = await controller.GetAsync(userInfo.Id, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null.And.AssignableTo<OkObjectResult>());
            Assert.That(userService.ReceivedCalls().ToList(), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Get_WithNotExistedUser_ReturnsNotFound()
    {
        var userService = Substitute.For<IUserService>();
        userService
            .GetAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => (UserInfo?)null);

        var controller = new UserController(userService);
        var result = await controller.GetAsync(long.MaxValue, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null.And.AssignableTo<NotFoundObjectResult>());
            Assert.That(userService.ReceivedCalls().ToList(), Has.Count.EqualTo(1));
        });
    }
    
    [Test]
    public async Task Create_JustReturnId()
    {
        const long expectedId = long.MaxValue;
        var userService = Substitute.For<IUserService>();
        userService
            .CreateAsync(Arg.Any<UserCreationInfo>(), Arg.Any<CancellationToken>())
            .Returns(_ => expectedId);

        var controller = new UserController(userService);
        var result = await controller.CreateAsync(new UserCreationInfo("test"),CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expectedId));
            Assert.That(userService.ReceivedCalls().ToList(), Has.Count.EqualTo(1));
        });
    }
    
    [Test]
    public void Update_JustCallService()
    {
        var record = new UserInfo { Login = "test", Id = long.MaxValue, CreationDate = DateTime.Now };
        var userService = Substitute.For<IUserService>();

        var controller = new UserController(userService);

        Assert.DoesNotThrowAsync(async () => await controller.UpdateAsync(record, CancellationToken.None));
        Assert.That(userService.ReceivedCalls().ToList(), Has.Count.EqualTo(1));
    }
    
    [Test]
    public async Task Delete_ExistedUser_ReturnsOk()
    {
        var userInfo = new UserInfo { Login = "test", Id = long.MaxValue, CreationDate = DateTime.Now };
        var userService = Substitute.For<IUserService>();
        userService
            .DeleteAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => userInfo);

        var controller = new UserController(userService);
        var result = await controller.DeleteAsync(userInfo.Id, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null.And.AssignableTo<OkObjectResult>());
            Assert.That(userService.ReceivedCalls().ToList(), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Delete_NotExistedUser_ReturnsNotFound()
    {
        var userService = Substitute.For<IUserService>();
        userService
            .DeleteAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => (UserInfo?)null);

        var controller = new UserController(userService);
        var result = await controller.DeleteAsync(long.MaxValue, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null.And.AssignableTo<NotFoundObjectResult>());
            Assert.That(userService.ReceivedCalls().ToList(), Has.Count.EqualTo(1));
        });
    }
}