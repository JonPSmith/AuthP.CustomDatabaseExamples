// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using CustomDatabase2.CustomParts.Sharding;
using LocalizeMessagesAndErrors.UnitTestingCode;
using Test.StubClasses;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests;

public class TestAddNewDbForNewTenantSqlServer
{
    [Fact]
    public async Task TestFindOrCreateDatabaseAsync()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var accessShardingInfo = new StubAccessDatabaseInformationVer5();
        var tenantChangeService = new StubTenantChangeService();
        var service = new AddNewDbForNewTenantSqlServer(accessShardingInfo,
            context, tenantChangeService, new StubAuthLocalizer());

        var tenant = Tenant.CreateSingleTenant(
            "MyTenant", new StubDefaultLocalizer()).Result;
        context.Add(tenant);
        context.SaveChanges();

        //ATTEMPT
        var status = await service.FindOrCreateDatabaseAsync(tenant, true, null);

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        accessShardingInfo.CalledMethodName.ShouldEqual("AddDatabaseInfoToShardingInformation");
        accessShardingInfo.DatabaseInfoFromCode.Name.ShouldEqual("Tenant_1");
        accessShardingInfo.DatabaseInfoFromCode.ConnectionName.ShouldEqual("DefaultConnection");
        accessShardingInfo.DatabaseInfoFromCode.DatabaseName.ShouldEqual("Tenant_1");
        accessShardingInfo.DatabaseInfoFromCode.DatabaseType.ShouldEqual("SqlServer");
        tenantChangeService.CalledMethodName.ShouldEqual("CreateNewTenantAsync");
        tenantChangeService.TenantParameter.TenantFullName.ShouldEqual("MyTenant");
    }

    [Fact]
    public async Task TestRemoveLastDatabaseSetupAsync()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var accessShardingInfo = new StubAccessDatabaseInformationVer5();
        var tenantChangeService = new StubTenantChangeService();
        var service = new AddNewDbForNewTenantSqlServer(accessShardingInfo, 
            context, tenantChangeService, new StubAuthLocalizer());

        var tenant = Tenant.CreateSingleTenant(
            "MyTenant", new StubDefaultLocalizer()).Result;
        context.Add(tenant);
        context.SaveChanges();

        (await service.FindOrCreateDatabaseAsync(tenant, true, null)).IsValid.ShouldBeTrue();

        //ATTEMPT
        var status = await service.RemoveLastDatabaseSetupAsync();

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        accessShardingInfo.CalledMethodName.ShouldEqual("RemoveDatabaseInfoFromShardingInformationAsync");
        accessShardingInfo.DatabaseInfoFromCode.Name.ShouldEqual("Tenant_1");
        tenantChangeService.CalledMethodName.ShouldEqual("SingleTenantDeleteAsync");
        tenantChangeService.TenantParameter.TenantFullName.ShouldEqual("MyTenant");
    }
}