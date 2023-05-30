// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.SetupCode;
using CustomDatabase2.SqliteCustomParts.Sharding;
using StatusGeneric;
using TestSupport.Helpers;

namespace Test.StubClasses;

public class StubConnectionsService : IShardingConnections
{
    private readonly bool _testConnectionReturnsError;

    /// <summary>
    /// This contains the methods with are specific to a database provider
    /// </summary>
    public IReadOnlyDictionary<AuthPDatabaseTypes, IDatabaseSpecificMethods> DatabaseProviderMethods { get; }

    /// <summary>
    /// This returns the supported database provider that can be used for multi tenant sharding
    /// </summary>
    public IReadOnlyDictionary<string, IDatabaseSpecificMethods> ShardingDatabaseProviders { get; }

    public StubConnectionsService(bool testConnectionReturnsError = false)
    {
        _testConnectionReturnsError = testConnectionReturnsError;
        DatabaseProviderMethods = new Dictionary<AuthPDatabaseTypes, IDatabaseSpecificMethods>
        {
            { AuthPDatabaseTypes.CustomDatabase, new SqliteSpecificMethods(
                new AuthPermissionsOptions{ PathToFolderToLock = TestData.GetTestDataDir() }, 
                new SqliteCombineDirAndDbName(TestData.GetTestDataDir())) },
        };
        ShardingDatabaseProviders = new Dictionary<string, IDatabaseSpecificMethods>
        {
            { "Sqlite", new SqliteSpecificMethods(
                new AuthPermissionsOptions{ PathToFolderToLock = TestData.GetTestDataDir() },
                new SqliteCombineDirAndDbName(TestData.GetTestDataDir())) }
        };
    }

    public List<DatabaseInformation> GetAllPossibleShardingData()
    {
        return new List<DatabaseInformation>
        {
            new DatabaseInformation{Name = "Default Database", ConnectionName = "UnitTestConnection", DatabaseType = "Sqlite"},
            new DatabaseInformation
            {
                Name = "Another Database", ConnectionName = "DefaultConnection", DatabaseName = "StubTest", DatabaseType = "Sqlite"
            }
        };
    }

    public IEnumerable<string> GetConnectionStringNames()
    {
        return new[] { "DefaultConnection"};
    }

    public IStatusGeneric TestFormingConnectionString(DatabaseInformation databaseInfo)
    {
        var status = new StatusGenericHandler();
        if (_testConnectionReturnsError)
            status.AddError("The connection string failed");

        return status;
    }

    public string FormConnectionString(DatabaseInformation databaseInfo)
    {
        return $"Data Source={TestData.GetTestDataDir()}\\{databaseInfo.Name}.sqlite";
    }

    public string FormConnectionString(string databaseInfoName)
    {
        return $"Data Source={TestData.GetTestDataDir()}\\{databaseInfoName}.sqlite";
    }

    public Task<List<(string databaseInfoName, bool? hasOwnDb, List<string> tenantNames)>> GetDatabaseInfoNamesWithTenantNamesAsync()
    {
        return Task.FromResult(new List<(string key, bool? hasOwnDb, List<string> tenantNames)>
        {
            ("Default Database", false, new List<string>{ "Tenant1","Tenant3"})
        });
    }
}