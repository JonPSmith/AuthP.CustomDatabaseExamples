// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.IO;
using System.Linq;

using System.Threading.Tasks;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.DataLayer.Classes;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreClasses;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreCode;
using LocalizeMessagesAndErrors.UnitTestingCode;
using Microsoft.EntityFrameworkCore;
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
        var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
            .UseSqlServer(_getConnectionsService.FormConnectionString(tenant.DatabaseInfoName))
            .Options;
        return new ShardingSingleDbContext(options);
    }

    [Fact]
    public async Task TestCreateNewTenantAsync()
    {
        //SETUP
        var tenant = Tenant.CreateSingleTenant(
            "TestTenant", new StubDefaultLocalizer()).Result;
        tenant.UpdateShardingState("CreateTenant", true);

        var context = GetShardingSingleDbContextFromTenant(tenant);
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
            "TestTenant", new StubDefaultLocalizer()).Result;
        tenant.UpdateShardingState("DeleteTenant", true);
        var context = GetShardingSingleDbContextFromTenant(tenant);
        context.Database.EnsureCreated();

        context.ChangeTracker.Clear();

        var service = new ShardingTenantChangeService(_getConnectionsService, null);

        //ATTEMPT
        var error = await service.SingleTenantDeleteAsync(tenant);

        //VERIFY
        error.ShouldBeNull();
        context.LineItems.Count().ShouldEqual(0);
        context.Invoices.Count().ShouldEqual(0);
        context.Companies.Count().ShouldEqual(0);
    }
}