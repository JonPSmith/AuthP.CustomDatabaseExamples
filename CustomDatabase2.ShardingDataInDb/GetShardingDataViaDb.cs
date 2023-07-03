// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using CustomDatabase2.ShardingDataInDb.ShardingDb;
using LocalizeMessagesAndErrors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StatusGeneric;

namespace CustomDatabase2.ShardingDataInDb;

public class GetShardingDataViaDb : IShardingConnections
{
    private readonly ConnectionStringsOption _connectionDict;
    private readonly ShardingDataDbContext _shardingContext;
    private readonly AuthPermissionsDbContext _authDbContext;
    private readonly AuthPermissionsOptions _options;
    private readonly IDefaultLocalizer _localizeDefault;

    /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
    public GetShardingDataViaDb(IOptionsSnapshot<ConnectionStringsOption> connectionsAccessor, 
        ShardingDataDbContext shardingContext,
        AuthPermissionsDbContext authDbContext,
        AuthPermissionsOptions options,
        IEnumerable<IDatabaseSpecificMethods> databaseProviderMethods,
        IAuthPDefaultLocalizer localizeProvider)
    {
        //thanks to https://stackoverflow.com/questions/37287427/get-multiple-connection-strings-in-appsettings-json-without-ef
        _connectionDict = connectionsAccessor.Value;
        _shardingContext = shardingContext;
        _authDbContext = authDbContext;
        _options = options;
        DatabaseProviderMethods = databaseProviderMethods.ToDictionary(x => x.AuthPDatabaseType);
        ShardingDatabaseProviders = DatabaseProviderMethods.Values.ToDictionary(x => x.DatabaseProviderShortName);
        _localizeDefault = localizeProvider.DefaultLocalizer;
    }

    /// <summary>
    /// This contains the methods with are specific to a database provider
    /// </summary>
    public IReadOnlyDictionary<AuthPDatabaseTypes, IDatabaseSpecificMethods> DatabaseProviderMethods { get; }

    /// <summary>
    /// This returns the names of supported database provider that can be used for multi tenant sharding
    /// </summary>
    public IReadOnlyDictionary<string, IDatabaseSpecificMethods> ShardingDatabaseProviders { get; }

    /// <summary>
    /// This returns all the database names in the DatabaseInformation data
    /// See <see cref="T:AuthPermissions.AspNetCore.ShardingServices.ShardingSettingsOption" /> for the format of that file
    /// NOTE: If there is no DatabaseInformation data, then it will return one
    /// <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> that uses the "DefaultConnection" connection string
    /// </summary>
    /// <returns>A list of <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> from the DatabaseInformation data</returns>
    public List<DatabaseInformation> GetAllPossibleShardingData()
    {
        return _shardingContext.ShardingData.ToList();
    }

    /// <summary>This provides the names of the connection string</summary>
    /// <returns></returns>
    public IEnumerable<string> GetConnectionStringNames()
    {
        return _shardingContext.ShardingData.Select(x => x.ConnectionName);
    }

    /// <summary>
    /// This returns all the database info names in the DatabaseInformation data, with a list of tenant name linked to each connection name
    /// </summary>
    /// <returns>List of all the database info names with the tenants (and whether its sharding) within that database data name
    /// NOTE: The hasOwnDb is true for a database containing a single database, false for multiple tenant database and null if empty</returns>
    public async Task<List<(string databaseInfoName, bool? hasOwnDb, List<string> tenantNames)>> GetDatabaseInfoNamesWithTenantNamesAsync()
    {
        var nameAndConnectionName = await _authDbContext.Tenants
            .Select(x => new { ConnectionName = x.DatabaseInfoName, x })
            .ToListAsync();

        var grouped = nameAndConnectionName.GroupBy(x => x.ConnectionName)
            .ToDictionary(x => x.Key,
                y => y.Select(z => new { z.x.HasOwnDb, z.x.TenantFullName }));

        var result = new List<(string databaseInfoName, bool? hasOwnDb, List<string>)>();
        //Add sharding database names that have no tenants in them so that you can see all the connection string  names
        foreach (var databaseInfoName in _shardingContext.ShardingData.Select(x => x.Name))
        {
            result.Add(grouped.ContainsKey(databaseInfoName)
                ? (databaseInfoName,
                    databaseInfoName == _options.ShardingDefaultDatabaseInfoName
                        ? false //The default DatabaseInfoName contains the AuthP information, so its a shared database
                        : grouped[databaseInfoName].FirstOrDefault()?.HasOwnDb,
                    grouped[databaseInfoName].Select(x => x.TenantFullName).ToList())
                : (databaseInfoName,
                    databaseInfoName == _options.ShardingDefaultDatabaseInfoName ? false : null,
                    new List<string>()));
        }

        return result;
    }

    /// <summary>
    /// This will provide the connection string for the entry with the given database info name
    /// </summary>
    /// <param name="databaseInfoName">The name of sharding database info we want to access</param>
    /// <returns>The connection string, or throw exception</returns>
    public string FormConnectionString(string databaseInfoName)
    {
        if (databaseInfoName == null)
            throw new AuthPermissionsException("The name of the database date can't be null");

        var databaseData = _shardingContext.ShardingData.SingleOrDefault(x => x.Name == databaseInfoName);
        if (databaseData == null)
            throw new AuthPermissionsException(
                $"The database information with the name of '{databaseInfoName}' wasn't found.");

        if (!_connectionDict.TryGetValue(databaseData.ConnectionName, out var connectionString))
            throw new AuthPermissionsException(
                $"Could not find the connection name '{databaseData.ConnectionName}' that the sharding database data '{databaseInfoName}' requires.");

        if (!ShardingDatabaseProviders.TryGetValue(databaseData.DatabaseType,
                out IDatabaseSpecificMethods databaseSpecificMethods))
            throw new AuthPermissionsException($"The {databaseData.DatabaseType} database provider isn't supported");

        return databaseSpecificMethods.SetDatabaseInConnectionString(databaseData, connectionString);
    }

    /// <summary>
    /// This method allows you to check that the <see cref="DatabaseInformation"/> will create a
    /// a valid connection string. Useful when adding or editing the data in the sharding settings file.
    /// </summary>
    /// <param name="databaseInfo">The full definition of the <see cref="DatabaseInformation"/> for this database info</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public IStatusGeneric TestFormingConnectionString(DatabaseInformation databaseInfo)
    {
        var status = new StatusGenericLocalizer(_localizeDefault);

        if (databaseInfo == null)
            throw new ArgumentNullException(nameof(databaseInfo));

        if (!_connectionDict.TryGetValue(databaseInfo.ConnectionName, out var connectionString))
            return status.AddErrorFormatted("NoConnectionString".ClassLocalizeKey(this, true),
                $"The {nameof(DatabaseInformation.ConnectionName)} '{databaseInfo.ConnectionName}' ",
                $"wasn't found in the connection strings.");

        if (!ShardingDatabaseProviders.TryGetValue(databaseInfo.DatabaseType,
                out IDatabaseSpecificMethods databaseSpecificMethods))
            throw new AuthPermissionsException($"The {databaseInfo.DatabaseType} database provider isn't supported");
        try
        {
            databaseSpecificMethods.SetDatabaseInConnectionString(databaseInfo, connectionString);
        }
        catch
        {
            status.AddErrorFormatted("BadConnectionString".ClassLocalizeKey(this, true),
                $"There was an  error when trying to create a connection string. Typically this is because ",
                $"the connection string doesn't match the {nameof(DatabaseInformation.DatabaseType)}.");
        }

        return status;
    }
}