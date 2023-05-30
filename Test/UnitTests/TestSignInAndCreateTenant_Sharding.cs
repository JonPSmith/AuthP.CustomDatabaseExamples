// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode.Services;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.SupportCode.AddUsersServices;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using System.Linq;
using CustomDatabase2.WebApp.Sharding.PermissionsCode;
using Test.StubClasses;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests;

public class TestSignInAndCreateTenant_Sharding
{
    private static (SignInAndCreateTenant service, AuthUsersAdminService userAdmin)
        CreateISignInAndCreateTenant(AuthPermissionsDbContext context,
            TenantTypes tenantType = TenantTypes.NotUsingTenants,
            IGetDatabaseForNewTenant? overrideNormal = null,
            bool loginReturnsError = false)
    {
        var authOptions = new AuthPermissionsOptions
        {
            TenantType = tenantType
        };
        var userAdmin = new AuthUsersAdminService(context, new StubSyncAuthenticationUsersFactory(),
            authOptions, new StubAuthLocalizer());
        var tenantAdmin = new AuthTenantAdminService(context, authOptions,
            new StubAuthLocalizer(), new StubITenantChangeServiceFactory(), null);
        var service = new SignInAndCreateTenant(authOptions, tenantAdmin,
            new StubAddNewUserManager(userAdmin, tenantAdmin, loginReturnsError), context,
            new StubAuthLocalizer(),
            overrideNormal ?? new StubIGetDatabaseForNewTenant(context,false));

        return (service, userAdmin);
    }

    [Theory]
    [InlineData("Free", null, "Invoice Creator,Invoice Reader")]
    [InlineData("Pro", "Tenant Admin", "Invoice Creator,Invoice Reader,Tenant Admin")]
    [InlineData("Enterprise", "Enterprise,Tenant Admin", "Invoice Creator,Invoice Reader,Tenant Admin")]
    public async Task TestAddUserAndNewTenantAsync_CustomDatabase2(string version, string tenantRoles, string adminRoles)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tuple = CreateISignInAndCreateTenant(context, TenantTypes.SingleLevel);
        var authSettings = new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(CustomDatabase2Permissions) } };
        var rolesSetup = new BulkLoadRolesService(context, authSettings);
        await rolesSetup.AddRolesToDatabaseAsync(CustomDatabase2AuthSetupData.RolesDefinition);

        context.ChangeTracker.Clear();

        //ATTEMPT
        var userData = new AddNewUserDto { Email = "me!@g1.com" };
        var tenantData = new AddNewTenantDto { TenantName = "New Tenant", Version = version };
        var status = await tuple.service.SignUpNewTenantWithVersionAsync(userData, tenantData, CreateTenantVersions.TenantSetupData);

        //VERIFY
        context.ChangeTracker.Clear();
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        var tenant = context.Tenants.Include(x => x.TenantRoles).Single();
        tenant.TenantFullName.ShouldEqual(tenantData.TenantName);
        tenant.TenantRoles.Select(x => x.RoleName).ToArray()
            .ShouldEqual(tenantRoles?.Split(',') ?? Array.Empty<string>());
        var user = context.AuthUsers.Include(x => x.UserRoles).Single();
        user.UserRoles.Select(x => x.RoleName).ToArray().ShouldEqual(adminRoles.Split(','));
    }
}