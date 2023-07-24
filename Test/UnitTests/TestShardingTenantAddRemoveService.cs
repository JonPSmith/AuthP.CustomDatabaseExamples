// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.SetupCode;
using CustomDatabase2.ShardingDataInDb;
using Test.StubClasses;
using Test.TestHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests;

public class TestShardingTenantAddRemoveService
{
    private readonly ITestOutputHelper _output;

    private AuthPermissionsOptions _authPOptions;
    private StubAccessDatabaseInformationVer5 _setShardings = new StubAccessDatabaseInformationVer5();
    private StubAuthTenantAdminService _stubTenantAdmin;

    public TestShardingTenantAddRemoveService(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// This returns an instance of the <see cref="ShardingTenantAddRemoveService"/> with the TenantType set.
    /// It also creates a extra <see cref="Tenant"/> to check duplication errors and also for the Delete
    /// </summary>
    /// <param name="hasOwnDb"></param>
    /// <param name="tenantType"></param>
    /// <param name="childTenant"></param>
    /// <returns></returns>
    private ShardingTenantAddRemoveService SetupService(bool hasOwnDb, TenantTypes tenantType = TenantTypes.SingleLevel,
         bool childTenant = false)
    {
        _authPOptions = new AuthPermissionsOptions
        {
            TenantType = tenantType | TenantTypes.AddSharding
        };

        var demoTenant = tenantType == TenantTypes.SingleLevel
            ? "TenantSingle".CreateSingleShardingTenant("Another Database", hasOwnDb)
            : "TenantHierarchical".CreateHierarchicalShardingTenant("Another Database", hasOwnDb);

        _stubTenantAdmin = new StubAuthTenantAdminService(demoTenant);
        return new ShardingTenantAddRemoveService(_stubTenantAdmin, new StubConnectionsService(),
            _setShardings, _authPOptions, new StubAuthLocalizer());
    }

    //-------------------------------------
    // Error tests

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

    //---------------------------------------------------
    // Create Single

    [Fact]
    public async Task Create_Single_HasOwnDbTrue_Good()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "Test",
            HasOwnDb = true,
            ConnectionStringName = "DefaultConnection",
            DbProviderShortName = "SqlServer",
        };
        var service = SetupService(true);

        //ATTEMPT
        var status = await service.CreateShardingTenantAndConnectionAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeFalse(status.GetAllErrors());
        _output.WriteLine(_setShardings.DatabaseInfoFromCode.ToString());
        _setShardings.DatabaseInfoFromCode.Name.ShouldEndWith("-Test");
        _setShardings.DatabaseInfoFromCode.ConnectionName.ShouldEqual("DefaultConnection");
        _setShardings.DatabaseInfoFromCode.DatabaseType.ShouldEqual("SqlServer");
        _stubTenantAdmin.CalledMethodName.ShouldEqual("AddSingleTenantAsync");
    }

    [Fact]
    public async Task Create_Single_HasOwnDbTrue_Good_OverriddenByDatabaseInfoName()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "Test",
            HasOwnDb = true,
            DatabaseInfoName = "Default Database"
        };
        var service = SetupService(true);

        //ATTEMPT
        var status = await service.CreateShardingTenantAndConnectionAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeFalse(status.GetAllErrors());
        _stubTenantAdmin.CalledMethodName.ShouldEqual("AddSingleTenantAsync");
    }

    [Fact]
    public async Task Create_Single_HasOwnDbTrue_DuplicateTenant()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "TenantSingle",
            HasOwnDb = true,
            ConnectionStringName = "DefaultConnection",
            DbProviderShortName = "SqlServer",
        };
        var service = SetupService(true);

        //ATTEMPT
        var status = await service.CreateShardingTenantAndConnectionAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeTrue();
        status.GetAllErrors().ShouldEqual("The tenant name 'TenantSingle' is already used");
    }

    [Fact]
    public async Task Create_Single_HasOwnDbFalse_Good()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "Test",
            HasOwnDb = false,
            DatabaseInfoName = "Another Database"
        };
        var service = SetupService(false);

        //ATTEMPT
        var status = await service.CreateShardingTenantAndConnectionAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeFalse(status.GetAllErrors());
        _output.WriteLine(status.Message);
        _stubTenantAdmin.CalledMethodName.ShouldEqual("AddSingleTenantAsync");
    }

    //---------------------------------------------------
    // Create Hierarchical

    [Fact]
    public async Task Create_Hierarchical_TopLevel_HasOwnDbTrue_Good()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "Test",
            HasOwnDb = true,
            ConnectionStringName = "DefaultConnection",
            DbProviderShortName = "SqlServer",
        };
        var service = SetupService(true, TenantTypes.HierarchicalTenant);

        //ATTEMPT
        var status = await service.CreateShardingTenantAndConnectionAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeFalse(status.GetAllErrors());
        _output.WriteLine(_setShardings.DatabaseInfoFromCode.ToString());
        _setShardings.DatabaseInfoFromCode.Name.ShouldEndWith("-Test");
        _setShardings.DatabaseInfoFromCode.ConnectionName.ShouldEqual("DefaultConnection");
        _setShardings.DatabaseInfoFromCode.DatabaseType.ShouldEqual("SqlServer");
        _stubTenantAdmin.CalledMethodName.ShouldEqual("AddHierarchicalTenantAsync");
    }

    [Fact]
    public async Task Create_Hierarchical_TopLevel_HasOwnDbFalse_Good()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "Test",
            HasOwnDb = false,
            DatabaseInfoName = "Another Database"
        };
        var service = SetupService(false, TenantTypes.HierarchicalTenant);

        //ATTEMPT
        var status = await service.CreateShardingTenantAndConnectionAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeFalse(status.GetAllErrors());
        _output.WriteLine(status.Message);
        _stubTenantAdmin.CalledMethodName.ShouldEqual("AddHierarchicalTenantAsync");
    }

    [Fact]
    public async Task Create_Hierarchical_Child_Good()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "Test",
            HasOwnDb = true,
            ParentTenantId = 1
        };
        var service = SetupService(true, TenantTypes.HierarchicalTenant);

        //ATTEMPT
        var status = await service.CreateShardingTenantAndConnectionAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeFalse(status.GetAllErrors());
        _setShardings.DatabaseInfoFromCode.ShouldBeNull();
        _stubTenantAdmin.CalledMethodName.ShouldEqual("AddHierarchicalTenantAsync");
    }

    //---------------------------------------------------
    // Create Single

    [Fact]
    public async Task Delete_Single_HasOwnDbTrue_Good()
    {
        //SETUP
        var service = SetupService(true);

        ////ATTEMPT
        var status = await service.DeleteTenantAndConnectionAsync(0);

        ////VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        _stubTenantAdmin.CalledMethodName.ShouldEqual("DeleteTenantAsync");
        _setShardings.CalledMethodName.ShouldEqual("RemoveDatabaseInfoFromShardingInformationAsync");
        _setShardings.DatabaseInfoFromCode.Name.ShouldEqual("Another Database");
    }

    [Fact]
    public async Task Delete_Single_HasOwnDbFalse_Good()
    {
        //SETUP
        var service = SetupService(false);

        ////ATTEMPT
        var status = await service.DeleteTenantAndConnectionAsync(0);

        ////VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        _stubTenantAdmin.CalledMethodName.ShouldEqual("DeleteTenantAsync");
        _setShardings.CalledMethodName.ShouldBeNull();
    }

    //---------------------------------------------------
    // Delete Hierarchical

    [Fact]
    public async Task Delete_Hierarchical_HasOwnDbTrue_Good()
    {
        //SETUP
        var service = SetupService(true, TenantTypes.HierarchicalTenant);

        ////ATTEMPT
        var status = await service.DeleteTenantAndConnectionAsync(0);

        ////VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        _stubTenantAdmin.CalledMethodName.ShouldEqual("DeleteTenantAsync");
        _setShardings.CalledMethodName.ShouldEqual("RemoveDatabaseInfoFromShardingInformationAsync");
        _setShardings.DatabaseInfoFromCode.Name.ShouldEqual("Another Database");
    }

    [Fact]
    public async Task Delete_Hierarchical_HasOwnDbFalse_Good()
    {
        //SETUP
        var service = SetupService(false, TenantTypes.HierarchicalTenant);

        ////ATTEMPT
        var status = await service.DeleteTenantAndConnectionAsync(0);

        ////VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        _stubTenantAdmin.CalledMethodName.ShouldEqual("DeleteTenantAsync");
        _setShardings.CalledMethodName.ShouldBeNull();
    }
}