// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.SetupCode;
using CustomDatabase2.ShardingDataInDb.ShardingDb;
using CustomDatabase2.ShardingDataInDb;
using Test.StubClasses;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;
using System.Threading.Tasks;

namespace Test.UnitTests;

public class TestSetShardingDataViaDb
{
    private readonly ITestOutputHelper _output;

    public TestSetShardingDataViaDb(ITestOutputHelper output)
    {
        _output = output;
    }

    private IAccessDatabaseInformationVer5 SetupSetShardingDataViaDb(bool addExtraEntries = true)
    {
        var options = SqliteInMemory.CreateOptions<ShardingDataDbContext>();
        var shardingContext = new ShardingDataDbContext(options, new DatabaseInformationOptions(false));
        shardingContext.Database.EnsureCreated();

        if (addExtraEntries)
        {
            var setupShardings = new List<DatabaseInformation>
            {
                new (){ Name = "Other Database", DatabaseName = "MyDatabase1", ConnectionName = "UnitTestConnection", DatabaseType = nameof(AuthPDatabaseTypes.SqlServer) },
                new (){ Name = "PostgreSql1", ConnectionName = "PostgreSqlConnection", DatabaseName = "StubTest", DatabaseType = nameof(AuthPDatabaseTypes.PostgreSQL) }
            };

            shardingContext.AddRange(setupShardings);
            shardingContext.SaveChanges();
            shardingContext.ChangeTracker.Clear();
        }

        return new SetShardingDataViaDb(shardingContext, new StubConnectionsService(), new StubAuthLocalizer());
    }

    [Fact]
    public void TestReadShardingSettingsFile_InitialData()
    {
        //SETUP
        var service = SetupSetShardingDataViaDb(false);

        //ATTEMPT
        var databaseInfo = service.ReadAllShardingInformation();

        //VERIFY
        foreach (var databaseInformation in databaseInfo)
        {
            _output.WriteLine(databaseInformation.ToString());
        }
        databaseInfo.Count.ShouldEqual(0);
    }

    [Fact]
    public void TestReadShardingSettingsFile()
    {
        //SETUP
        var service = SetupSetShardingDataViaDb();

        //ATTEMPT
        var databaseInfo = service.ReadAllShardingInformation();

        //VERIFY
        foreach (var databaseInformation in databaseInfo)
        {
            _output.WriteLine(databaseInformation.ToString());
        }
        databaseInfo.Count.ShouldEqual(2);
        databaseInfo[0].ToString().ShouldEqual("Name: Other Database, DatabaseName: MyDatabase1, ConnectionName: UnitTestConnection, DatabaseType: SqlServer");
        databaseInfo[1].ToString().ShouldEqual("Name: PostgreSql1, DatabaseName: StubTest, ConnectionName: PostgreSqlConnection, DatabaseType: PostgreSQL");
    }

    [Theory]
    [InlineData("New Name", true)]
    [InlineData(null, false)]
    [InlineData("   ", false)]
    public void TestAddDatabaseInfoToJsonFile_TestName(string name, bool isValid)
    {
        //SETUP
        var service = SetupSetShardingDataViaDb();

        //ATTEMPT
        var databaseInfo = new DatabaseInformation
        {
            DatabaseType = nameof(AuthPDatabaseTypes.SqlServer),
            Name = name,
            ConnectionName = "UnitTestConnection"
        };
        var status = service.AddDatabaseInfoToShardingInformation(databaseInfo);

        //VERIFY
        _output.WriteLine(status.IsValid ? status.Message : status.GetAllErrors());
        status.IsValid.ShouldEqual(isValid);
        service.ReadAllShardingInformation().Count.ShouldEqual(status.IsValid ? 3 : 2);
    }

    [Theory]
    [InlineData("UnitTestConnection", true)]
    [InlineData("   ", false)]
    public void TestAddDatabaseInfoToJsonFile_ConnectionName(string connectionName, bool isValid)
    {
        //SETUP
        var service = SetupSetShardingDataViaDb();

        //ATTEMPT
        var databaseInfo = new DatabaseInformation
        {
            Name = "New Entry",
            DatabaseType = nameof(AuthPDatabaseTypes.SqlServer),
            ConnectionName = connectionName
        };
        var status = service.AddDatabaseInfoToShardingInformation(databaseInfo);

        //VERIFY
        _output.WriteLine(status.IsValid ? status.Message : status.GetAllErrors());
        status.IsValid.ShouldEqual(isValid);
        service.ReadAllShardingInformation().Count.ShouldEqual(status.IsValid ? 3 : 2);
    }

    [Theory]
    [InlineData("Other Database", true)]
    [InlineData("   ", false)]
    [InlineData("Missing Entry", false)]
    public void TestUpdateDatabaseInfoToShardingInformation(string name, bool isValid)
    {
        //SETUP
        var service = SetupSetShardingDataViaDb();

        //ATTEMPT
        var databaseInfo = new DatabaseInformation
        {
            DatabaseType = nameof(AuthPDatabaseTypes.SqlServer),
            Name = name,
            ConnectionName = "UnitTestConnection"
        };
        var status = service.UpdateDatabaseInfoToShardingInformation(databaseInfo);

        //VERIFY
        _output.WriteLine(status.IsValid ? status.Message : status.GetAllErrors());
        status.IsValid.ShouldEqual(isValid);
    }

    [Theory]
    [InlineData("Other Database", true)]
    [InlineData("   ", false)]
    [InlineData("Missing Entry", false)]
    public async Task TestRemoveDatabaseInfoFromShardingInformationAsync(string name, bool isValid)
    {
        //SETUP
        var service = SetupSetShardingDataViaDb();

        //ATTEMPT
        var status = await service.RemoveDatabaseInfoFromShardingInformationAsync(name);

        //VERIFY
        _output.WriteLine(status.IsValid ? status.Message : status.GetAllErrors());
        status.IsValid.ShouldEqual(isValid);
        service.ReadAllShardingInformation().Count.ShouldEqual(status.IsValid ? 1 : 2);
    }


}