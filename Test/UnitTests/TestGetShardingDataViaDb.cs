// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using CustomDatabase2.ShardingDataInDb;
using CustomDatabase2.ShardingDataInDb.ShardingDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests;

public class TestGetShardingDataViaDb
{
    private readonly ITestOutputHelper _output;
    private readonly IOptionsSnapshot<ConnectionStringsOption> _connectSnapshot;

    public TestGetShardingDataViaDb(ITestOutputHelper output)
    {
        _output = output;

        var config = AppSettings.GetConfiguration(
            "..\\Test\\TestData", "appsettings.json");
        var services = new ServiceCollection();
        services.Configure<ConnectionStringsOption>(config.GetSection("ConnectionStrings"));
        var serviceProvider = services.BuildServiceProvider();

        _connectSnapshot = serviceProvider.GetRequiredService<IOptionsSnapshot<ConnectionStringsOption>>();
    }

    private IShardingConnections SetupGetShardingDataViaDb(AuthPermissionsDbContext? authContext = null)
    {
        var options = this.CreateUniqueClassOptions<ShardingDataDbContext>();
        var shardingContext = new ShardingDataDbContext(options, new DatabaseInformationOptions(false));
        shardingContext.Database.EnsureDeleted();
        shardingContext.Database.EnsureCreated();

        shardingContext.AddRange(
            new DatabaseInformation
            {
                Name = "Another",
                DatabaseName = "AnotherDatabase",
                ConnectionName = "AnotherConnectionString",
                DatabaseType = "SqlServer"
            },
            new DatabaseInformation
            {
                Name = "Bad: No DatabaseName",
                DatabaseName = null,
                ConnectionName = "ServerOnlyConnectionString",
                DatabaseType = "SqlServer"
            },
            new DatabaseInformation
            {
                Name = "Special Postgres",
                DatabaseName = "MyDatabase",
                ConnectionName = "PostgresConnection",
                DatabaseType = "PostgreSQL"
            });
        shardingContext.SaveChanges();

        return new GetShardingDataViaDb(_connectSnapshot,
            shardingContext, authContext, new AuthPermissionsOptions(),
            ShardingHelpers.GetDatabaseSpecificMethods(),
            new StubAuthLocalizer());
    }


    [Fact]
    public void TestGetAllConnectionStrings()
    {
        //SETUP
        var service = SetupGetShardingDataViaDb();

        //ATTEMPT
        var databaseData = service.GetAllPossibleShardingData()
            .OrderBy(x => x.Name).ToList();

        //VERIFY
        foreach (var data in databaseData)
        {
            _output.WriteLine(data.ToString());
        }
        databaseData.Count.ShouldEqual(3);
        databaseData[0].Name.ShouldEqual("Another");
        databaseData[1].Name.ShouldEqual("Bad: No DatabaseName");
        databaseData[2].Name.ShouldEqual("Special Postgres");
    }

    [Fact]
    public void TestGetConnectionStringNames()
    {
        //SETUP
        var service = SetupGetShardingDataViaDb();

        //ATTEMPT
        var connectionStrings = service.GetConnectionStringNames().OrderBy(x => x).ToList();

        //VERIFY
        foreach (var data in connectionStrings)
        {
            _output.WriteLine(data.ToString());
        }
        connectionStrings.Count.ShouldEqual(3);
        connectionStrings[0].ShouldEqual("AnotherConnectionString");
        connectionStrings[1].ShouldEqual("PostgresConnection");
        connectionStrings[2].ShouldEqual("ServerOnlyConnectionString");
    }

    [Theory]
    [InlineData("DefaultConnection", true)]
    [InlineData("PostgresConnection", false)]
    public void TestFormingConnectionString(string connectionName, bool isValid)
    {
        //SETUP
        var service = SetupGetShardingDataViaDb();

        //ATTEMPT
        var databaseInfo = new DatabaseInformation
        {
            Name = "Test",
            DatabaseName = "TestDb",
            ConnectionName = connectionName,
            DatabaseType = "SqlServer"
        };
        var status = service.TestFormingConnectionString(databaseInfo);

        //VERIFY
        _output.WriteLine(status.IsValid ? "success" : status.GetAllErrors());
        status.IsValid.ShouldEqual(isValid);
    }

    [Fact]
    public void TestGetNamedConnectionStringSqlServer()
    {
        //SETUP
        var service = SetupGetShardingDataViaDb();

        //ATTEMPT
        var connectionString = service.FormConnectionString("Another");

        //VERIFY
        connectionString.ShouldEqual("Data Source=MyServer;Initial Catalog=AnotherDatabase");
    }

    [Fact]
    public void TestGetNamedConnectionStringSqlServer_NoDatabaseName()
    {
        //SETUP
        var service = SetupGetShardingDataViaDb();

        //ATTEMPT
        var ex = Assert.Throws<AuthPermissionsException>(() => service.FormConnectionString("Bad: No DatabaseName"));

        //VERIFY
        ex.Message.ShouldEqual("The DatabaseName can't be null or empty when the connection string doesn't have a database defined.");
    }

    [Fact]
    public void TestGetNamedConnectionStringPostgres()
    {
        //SETUP
        var service = SetupGetShardingDataViaDb();

        //ATTEMPT
        var connectionString = service.FormConnectionString("Special Postgres");

        //VERIFY
        connectionString.ShouldEqual("Host=127.0.0.1;Database=MyDatabase;Username=postgres;Password=LetMeIn");
    }

    [Fact]
    public async Task TestQueryTenantsSingle()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tenant1 = "Tenant1".CreateTestSingleTenantOk();
        tenant1.UpdateShardingState("Another", false);
        context.Add(tenant1);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        var service = SetupGetShardingDataViaDb(context);

        //ATTEMPT
        var keyPairs = await service.GetDatabaseInfoNamesWithTenantNamesAsync();

        //VERIFY
        keyPairs.ShouldEqual(new List<(string databaseName, bool? hasOwnDb, List<string> tenantNames)>
        {
            ("Another", false, new List<string>{ "Tenant1"}),
            ("Bad: No DatabaseName", null, new List<string>()),
            ("Special Postgres", null, new List<string>())
        });
    }


    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TestQueryTenantsSingleDefaultConnectionName(bool addTenantDefaultDatabase)
    {
        //This checks that the Default DatabaseInfoName always returns a HasOwnDb of false.
        //That's because that database contains the AuthP data as well, so sharding database would have other data with it

        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        if (addTenantDefaultDatabase)
        {
            var tenant1 = "Tenant1".CreateTestSingleTenantOk();
            tenant1.UpdateShardingState("Default Database", true);
            context.Add(tenant1);
        }
        var tenant2 = "Tenant2".CreateTestSingleTenantOk();
        tenant2.UpdateShardingState("Another", false);
        context.Add(tenant2);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        var config = AppSettings.GetConfiguration("..\\Test\\TestData");
        var services = new ServiceCollection();
        services.Configure<ConnectionStringsOption>(config.GetSection("ConnectionStrings"));

        var service = SetupGetShardingDataViaDb(context);

        //ATTEMPT
        var keyPairs = await service.GetDatabaseInfoNamesWithTenantNamesAsync();

        //VERIFY
        keyPairs.ShouldEqual(new List<(string databaseName, bool? hasOwnDb, List<string> tenantNames)>
        {
            ("Another", false, new List<string>{ "Tenant2"}),
            ("Bad: No DatabaseName", null, new List<string>()),
            ("Special Postgres", null, new List<string>())
        });
    }
}