// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using CustomDatabase2.SqliteCustomParts.Sharding;
using Microsoft.Data.SqlClient;
using StatusGeneric;
using Test.TestHelpers;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests;

public class TestSqliteDatabaseSpecificMethods
{
    private readonly ITestOutputHelper _output;

    public TestSqliteDatabaseSpecificMethods(ITestOutputHelper output)
    {
        _output = output;
    }

    private DatabaseInformation SetupDatabaseInformation(bool nameIsNull)
    {
        return new DatabaseInformation
        {
            Name = "EntryName",
            DatabaseType = "Sqlite",
            DatabaseName = nameIsNull ? null : "TestDatabase",
            ConnectionName = "DefaultConnection"
        };
    }

    [Theory]
    [InlineData(true, "MyDir\\OriginalName.sqlite")]
    [InlineData(false, "MyDir\\TestDatabase.sqlite")]
    public void TestSetDatabaseInConnectionStringOk(bool nullName, string dbName)
    {
        //SETUP
        var service = new SqliteSpecificMethods(
            new AuthPermissionsOptions { PathToFolderToLock = TestData.GetTestDataDir() },
            new SqliteCombineDirAndDbName("MyDir\\"));

        //ATTEMPT
        var connectionString = service.SetDatabaseInConnectionString(SetupDatabaseInformation(nullName),
            "Data source={AppDir}\\OriginalName.sqlite");

        //VERIFY
        var builder = new SqlConnectionStringBuilder(connectionString);
        builder.DataSource.ShouldEqual(dbName);
    }

    [Fact]
    public void TestSetDatabaseInConnectionStringBad()
    {
        //SETUP
        var service = new SqliteSpecificMethods(
            new AuthPermissionsOptions { PathToFolderToLock = TestData.GetTestDataDir() },
            new SqliteCombineDirAndDbName("MyDir\\"));

        //ATTEMPT
        var ex = Assert.Throws<AuthPermissionsException>(() =>
            service.SetDatabaseInConnectionString(SetupDatabaseInformation(true),
            ""));

        //VERIFY
        ex.Message.ShouldEqual("The DatabaseName can't be null or empty when the connection string doesn't have a database defined.");
    }

    [Fact]
    public void TestChangeDatabaseInformationWithinDistributedLock()
    {
        //SETUP
        var service = new SqliteSpecificMethods(
            new AuthPermissionsOptions { PathToFolderToLock = TestData.GetTestDataDir() },
            new SqliteCombineDirAndDbName("MyDir"));
        var logs = new ConcurrentStack<string>();

        //ATTEMPT
        Parallel.ForEach(new string[] { "Name1", "Name2", "Name3" },
            name =>
            {
                var status = service.ChangeDatabaseInformationWithinDistributedLock(TestData.GetTestDataDir(),
                    () =>
                    {
                        logs.Push(name);
                        Thread.Sleep(10);
                        return new StatusGenericHandler();
                    });
                status.IsValid.ShouldBeTrue();
            });

        //VERIFY
        foreach (var log in logs)
        {
            _output.WriteLine(log);
        }
        logs.OrderBy(x => x).ToArray().ShouldEqual(new string[] { "Name1", "Name2", "Name3" });
    }

    [Fact]
    public async Task TestChangeDatabaseInformationWithinDistributedLockAsync()
    {
        //SETUP
        var service = new SqliteSpecificMethods(
            new AuthPermissionsOptions { PathToFolderToLock = TestData.GetTestDataDir() },
            new SqliteCombineDirAndDbName("MyDir"));
        var logs = new ConcurrentStack<string>();

        async Task TaskAsync(int i)
        {
            var status = service.ChangeDatabaseInformationWithinDistributedLock(TestData.GetTestDataDir(),
                () =>
                {
                    logs.Push(i.ToString());
                    Thread.Sleep(10);
                    return new StatusGenericHandler();
                });
        }

        //ATTEMPT
        await 3.NumTimesAsyncEnumerable().AsyncParallelForEach(TaskAsync);

        //VERIFY
        foreach (var log in logs)
        {
            _output.WriteLine(log);
        }
        logs.OrderBy(x => x).ToArray().ShouldEqual(new string[] { "1", "2", "3" });
    }

}