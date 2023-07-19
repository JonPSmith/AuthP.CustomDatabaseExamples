// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using CustomDatabase2.ShardingDataInDb;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests;

public class TestShardingTenantAddRemoveService
{
    private readonly ITestOutputHelper _output;

    private readonly AuthPermissionsOptions _authPOptions;
    private readonly StubAccessDatabaseInformationVer5 _setShardings;
    private readonly IShardingTenantAddRemove _service;

    public TestShardingTenantAddRemoveService(ITestOutputHelper output)
    {
        _output = output;

        _authPOptions = new AuthPermissionsOptions
        {
            TenantType = TenantTypes.SingleLevel | TenantTypes.AddSharding
        };

        var demoTenant = "Tenant1".CreateShardingTenantOk(true, "Another Database");
        _setShardings = new StubAccessDatabaseInformationVer5();
        _service = new ShardingTenantAddRemoveService(new StubAuthTenantAdminService(demoTenant), 
            new StubConnectionsService(),
            _setShardings, _authPOptions, new StubAuthLocalizer());
    }

    [Fact]
    public void TestCodeToRejectType()
    {
        //SETUP
        IShardingConnections param = new StubConnectionsService(true);

        //ATTEMPT
        //A test like below is used in the ShardingTenantAddRemove to stop developers using the ShardingConnectionsJsonFile
        var test = param.GetType() == typeof(StubConnectionsService);

        //VERIFY
        test.ShouldBeTrue();
    }

    [Fact]
    public async Task Create_HasOwnDbTrue_Good()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "Test",
            HasOwnDb = true,
            ConnectionStringName = "DefaultConnection",
            DbProviderShortName = "SqlServer",
        };

        //ATTEMPT
        var status = await _service.CreateShardingTenantAndConnectionAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeFalse(status.GetAllErrors());
        _output.WriteLine(_setShardings.DatabaseInfoFromCode.ToString());
        _setShardings.DatabaseInfoFromCode.Name.ShouldEndWith("-Test");
        _setShardings.DatabaseInfoFromCode.ConnectionName.ShouldEqual("DefaultConnection");
        _setShardings.DatabaseInfoFromCode.DatabaseType.ShouldEqual("SqlServer");
    }

    [Fact]
    public async Task Create_HasOwnDbTrue_DuplicateTenant()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "Tenant1",
            HasOwnDb = true,
            ConnectionStringName = "DefaultConnection",
            DbProviderShortName = "SqlServer",
        };

        //ATTEMPT
        var status = await _service.CreateShardingTenantAndConnectionAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeTrue();
        status.GetAllErrors().ShouldEqual("The tenant name 'Tenant1' is already used");
    }

    [Fact]
    public async Task Create_HasOwnDbFalse_Good()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "Test",
            HasOwnDb = false,
            DatabaseInfoName = "Another Database"
        };

        //ATTEMPT
        var status = await _service.CreateShardingTenantAndConnectionAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeFalse(status.GetAllErrors());
        _output.WriteLine(status.Message);
    }

    [Fact]
    public async Task hierarchicaH()
    {
        true.ShouldBeFalse("needs a test for child added to hierarchical tenant.");
    }

    [Fact]
    public async Task Delete_HasOwnDbTrue_Good()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();
        var setupStatus = Tenant.CreateSingleTenant("Tenant1", new StubAuthLocalizer().DefaultLocalizer);
        setupStatus.IfErrorsTurnToException();
        setupStatus.Result.UpdateShardingState("Another Database", true);
        context.Add(setupStatus.Result);
        context.SaveChanges();

        var stubTenantAdmin = new StubAuthTenantAdminService(setupStatus.Result);
        var service = new ShardingTenantAddRemoveService(stubTenantAdmin, new StubConnectionsService(),
            _setShardings, _authPOptions, new StubAuthLocalizer());

        ////ATTEMPT
        var status = await service.DeleteTenantAndConnectionAsync(setupStatus.Result.TenantId);

        ////VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        stubTenantAdmin.CalledMethodName.ShouldEqual("DeleteTenantAsync");
        _setShardings.CalledMethodName.ShouldEqual("RemoveDatabaseInfoFromShardingInformationAsync");
        _setShardings.DatabaseInfoFromCode.Name.ShouldEqual("Another Database");
    }

    [Fact]
    public async Task Delete_HasOwnDbFalse_Good()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();
        var setupStatus = Tenant.CreateSingleTenant("Tenant1", new StubAuthLocalizer().DefaultLocalizer);
        setupStatus.IfErrorsTurnToException();
        setupStatus.Result.UpdateShardingState("Another Database", false);
        context.Add(setupStatus.Result);
        context.SaveChanges();

        var stubTenantAdmin = new StubAuthTenantAdminService(setupStatus.Result);
        var service = new ShardingTenantAddRemoveService(stubTenantAdmin, new StubConnectionsService(),
            _setShardings, _authPOptions, new StubAuthLocalizer());

        ////ATTEMPT
        var status = await service.DeleteTenantAndConnectionAsync(setupStatus.Result.TenantId);

        ////VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        stubTenantAdmin.CalledMethodName.ShouldEqual("DeleteTenantAsync");
        _setShardings.CalledMethodName.ShouldBeNull();
    }


}