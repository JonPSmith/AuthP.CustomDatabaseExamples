// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using CustomDatabase2.SqliteCustomParts.Sharding;
using TestSupport.Helpers;

namespace Test.TestHelpers;

public static class ShardingHelpers
{
    public static List<IDatabaseSpecificMethods> GetDatabaseSpecificMethods()
    {
        return new List<IDatabaseSpecificMethods>
        {
            new SqliteSpecificMethods(
                new AuthPermissionsOptions{ PathToFolderToLock = TestData.GetTestDataDir()},
                new SqliteCombineDirAndDbName(TestData.GetTestDataDir())),
            new SqlServerDatabaseSpecificMethods(),
            new PostgresDatabaseSpecificMethods(),
        };
    }
}