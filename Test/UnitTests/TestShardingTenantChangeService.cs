// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.BaseCode.DataLayer.Classes;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreClasses;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreCode;
using LocalizeMessagesAndErrors.UnitTestingCode;
using Microsoft.EntityFrameworkCore;
using Test.StubClasses;
using TestSupport.Helpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests;

public class TestShardingTenantChangeService
{
    [Fact]
    public void TestSqliteShardingInvoiceDb_RealDatabase()
    {
        //SETUP
        var testDataPath = TestData.GetTestDataDir();
        var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
            .UseSqlite($"Data Source={testDataPath}\\shardingInvoice.sqlite", dbOptions =>
            {
                dbOptions.MigrationsHistoryTable("__ShardingInvoiceMigrationsHistoryTable");
                dbOptions.MigrationsAssembly("CustomDatabase2.InvoiceCode.Sharding");
            }).Options;
        var context = new ShardingSingleDbContext(options);
        context.Database.EnsureDeleted();

        //ATTEMPT
        context.Database.Migrate();
        context.Add(new CompanyTenant { CompanyName = "Test" });
        context.SaveChanges();

        //VERIFY
        context.Companies.Single().CompanyName.ShouldEqual("Test");
    }

    [Fact]
    public async Task TestCreateNewTenantAsync()
    {
        //SETUP
        var testDataPath = TestData.GetTestDataDir();
        var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
            .UseSqlite($"Data Source={testDataPath}\\CreateTenant.sqlite")
            .Options;
        var context = new ShardingSingleDbContext(options);
        context.Database.EnsureDeleted();
        context.ChangeTracker.Clear();

        var tenant = Tenant.CreateSingleTenant(
            "TestTenant", new StubDefaultLocalizer()).Result;
        tenant.UpdateShardingState("CreateTenant", true);

        var stubCon = new StubConnectionsService();
        var service = new ShardingTenantChangeService(stubCon, null);

        //ATTEMPT
        File.Exists($"{testDataPath}\\CreateTenant.sqlite").ShouldBeFalse();
        var error = await service.CreateNewTenantAsync(tenant);

        //VERIFY
        error.ShouldBeNull();
        File.Exists($"{testDataPath}\\CreateTenant.sqlite").ShouldBeTrue();
        context.Companies.Single().CompanyName.ShouldEqual("TestTenant");
    }

    [Fact]
    public async Task TestSingleTenantDeleteAsync()
    {
        //SETUP
        var testDataPath = TestData.GetTestDataDir();
        var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
            .UseSqlite($"Data Source={testDataPath}\\DeleteTenant.sqlite")
            .Options;
        var context = new ShardingSingleDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var tenant = Tenant.CreateSingleTenant(
            "TestTenant", new StubDefaultLocalizer()).Result;
        tenant.UpdateShardingState("DeleteTenant", true);

        var stubCon = new StubConnectionsService();
        var service = new ShardingTenantChangeService(stubCon, null);

        //ATTEMPT
        File.Exists($"{testDataPath}\\DeleteTenant.sqlite").ShouldBeTrue();
        var error = await service.SingleTenantDeleteAsync(tenant);

        //VERIFY
        error.ShouldBeNull();
        File.Exists($"{testDataPath}\\DeleteTenant.sqlite").ShouldBeFalse();
    }
}