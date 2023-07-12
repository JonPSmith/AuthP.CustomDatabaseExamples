// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.Classes;
using System.Collections.Generic;
using AuthPermissions.BaseCode.CommonCode;
using Test.StubClasses;

namespace Test.TestHelpers;

public static class AuthPSetupHelpers
{
    /// <summary>
    /// Use this to create a SingleTenant Tenant in your tests
    /// </summary>
    /// <param name="fullTenantName"></param>
    /// <param name="tenantRoles"></param>
    /// <returns></returns>
    public static Tenant CreateTestSingleTenantOk(this string fullTenantName, List<RoleToPermissions> tenantRoles = null)
    {
        var status = Tenant.CreateSingleTenant(fullTenantName,
            new StubAuthLocalizer().DefaultLocalizer, tenantRoles);
        status.IfErrorsTurnToException();
        return status.Result;
    }

    /// <summary>
    /// Use this to create a sharding Tenant in your tests
    /// </summary>
    /// <param name="fullTenantName"></param>
    /// <param name="hasOwnDb"></param>
    /// <param name="databaseInfoName"></param>
    /// <returns></returns>
    public static Tenant CreateShardingTenantOk(this string fullTenantName, bool hasOwnDb, string databaseInfoName)
    {
        var status = Tenant.CreateSingleTenant(fullTenantName,
            new StubAuthLocalizer().DefaultLocalizer);
        status.IfErrorsTurnToException();
        status.Result.UpdateShardingState(databaseInfoName, hasOwnDb);
        return status.Result;
    }
}