// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.DataLayer.Classes;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreCode;
using LocalizeMessagesAndErrors.UnitTestingCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Test.StubClasses;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests;

public class TestShardingTenantChangeService
{
    private readonly IShardingConnections _getConnectionsService = new StubConnectionsService();

    private ShardingSingleDbContext GetShardingSingleDbContextFromTenant(Tenant tenant)
    {
        var connectionString = _getConnectionsService.FormConnectionString(tenant.DatabaseInfoName);
        var options = this.CreateUniqueClassOptions<ShardingSingleDbContext>();
        return new ShardingSingleDbContext(options, new ManualAddConnectionStringToDb(connectionString));
    }

    [Fact]
    public async Task TestCreateNewTenantAsync()
    {
        //SETUP
        var tenant = Tenant.CreateSingleTenant(
            "TestTenant", new StubDefaultLocalizer()).Result;
        tenant.UpdateShardingState("Sharding1", true);

        using var context = GetShardingSingleDbContextFromTenant(tenant);
        context.Database.EnsureDeleted();

        context.ChangeTracker.Clear();

        var service = new ShardingTenantChangeService(_getConnectionsService, null);

        //ATTEMPT
        var error = await service.CreateNewTenantAsync(tenant);

        //VERIFY
        error.ShouldBeNull();
        (await context.Database.CanConnectAsync()).ShouldBeTrue();
        context.Companies.Single().CompanyName.ShouldEqual("TestTenant");
    }

    [Fact]
    public async Task TestSingleTenantDeleteAsync()
    {
        //SETUP
        var tenant = Tenant.CreateSingleTenant(
            "DeleteTenant", new StubDefaultLocalizer()).Result;
        tenant.UpdateShardingState("Sharding2", true);

        using var context = GetShardingSingleDbContextFromTenant(tenant);
        var x = context.Database.GetConnectionString();
        context.Database.EnsureCreated();
        (await context.Database.CanConnectAsync() || await context.Database.GetService<IRelationalDatabaseCreator>().HasTablesAsync())
            .ShouldBeTrue();

        var service = new ShardingTenantChangeService(_getConnectionsService, null);

        context.ChangeTracker.Clear();

        //ATTEMPT
        var error = await service.SingleTenantDeleteAsync(tenant);

        //VERIFY
        error.ShouldBeNull();
        (await context.Database.CanConnectAsync()).ShouldBeFalse();
    }
}