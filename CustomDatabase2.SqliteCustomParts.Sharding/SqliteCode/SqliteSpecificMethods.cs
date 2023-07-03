// // Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// // Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.SetupCode;
using Medallion.Threading.FileSystem;
using Microsoft.Data.Sqlite;
using StatusGeneric;

namespace CustomDatabase2.CustomParts.Sharding.SqliteCode;

/// <summary>
/// This class implements the sharding methods for a Sqlite database.
/// i.e. it provides it provides Sqlite methods for creating connection strings.
/// Your would register this class to the DI in your custom database extension methods
/// </summary>
public class SqliteSpecificMethods : IDatabaseSpecificMethods
{
    private readonly AuthPermissionsOptions _options;
    private readonly ISqliteCombineDirAndDbName _buildSqliteDataSource;

    public SqliteSpecificMethods(AuthPermissionsOptions options,
        ISqliteCombineDirAndDbName buildSqliteDataSource)
    {
        _options = options;
        _buildSqliteDataSource = buildSqliteDataSource;
    }

    /// <summary>
    /// This is used select the <see cref="IDatabaseSpecificMethods"/> from the AuthP's <see cref="SetupInternalData.AuthPDatabaseType"/>
    /// </summary>
    public AuthPDatabaseTypes AuthPDatabaseType => AuthPDatabaseTypes.CustomDatabase;

    /// <summary>
    /// This contains the short name of Database Provider the service supports
    /// </summary>
    public string DatabaseProviderShortName => "Sqlite";

    /// <summary>
    /// This changes the database to the <see cref="DatabaseInformation.DatabaseName"/> in the given connectionString.
    /// For Sqlite the DatabaseName must in
    /// NOTE: If the <see cref="DatabaseInformation.DatabaseName"/> is null / empty, then it returns the connectionString with no change
    /// </summary>
    /// <param name="databaseInformation">Information about the database type/name to be used in the connection string</param>
    /// <param name="connectionString"></param>
    /// <returns>A connection string containing the correct database to be used, or errors</returns>
    /// <exception cref="InvalidEnumArgumentException"></exception>
    public string SetDatabaseInConnectionString(DatabaseInformation databaseInformation, string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrEmpty(builder.DataSource) && string.IsNullOrEmpty(databaseInformation.DatabaseName))
            throw new AuthPermissionsException(
                $"The {nameof(DatabaseInformation.DatabaseName)} can't be null or empty " +
                "when the connection string doesn't have a database defined.");

        if (string.IsNullOrEmpty(databaseInformation.DatabaseName))
            //This uses the database that is already in the connection string
            return _buildSqliteDataSource.AddDirectoryToConnection(connectionString);

        //This returns a connection string containing:
        // a) the database name from the DatabaseInformation
        // b) The {AppDir} part is replaced by the directory defined in the SqliteCombineDirAndDbName
        return _buildSqliteDataSource
            .AddDirectoryToConnection(connectionString, databaseInformation.DatabaseName);
    }

    /// <summary>
    /// This will execute the function (which updates the shardingsettings json file) within a Distributed Lock. 
    /// Typically this will use a lock on the database provider.  
    /// </summary>
    /// <param name="connectionString">connection string to the database to place a Distributed Lock on</param>
    /// <param name="runInLock"></param>
    /// <returns></returns>
    public IStatusGeneric ChangeDatabaseInformationWithinDistributedLock(string connectionString, Func<IStatusGeneric> runInLock)
    {
        //The https://github.com/madelson/DistributedLock doesn't support Sqlite for locking
        //so we just use the File lock
        //NOTE: DistributedLock does support many database types and its fairly easy to build a LockAndRun method
        var myDistributedLock = new FileDistributedLock(GetDirectoryInfoToLockWithCheck(), "MyLockName");
        using (myDistributedLock.Acquire())
        {
            return runInLock();
        }
    }

    /// <summary>
    /// This will execute the function (which updates the shardingsettings json file) within a Distributed Lock. 
    /// Typically this will use a lock on the database provider.  
    /// </summary>
    /// <param name="connectionString">connection string to the database to place a Distributed Lock on</param>
    /// <param name="runInLockAsync"></param>
    /// <returns></returns>
    public async Task<IStatusGeneric> ChangeDatabaseInformationWithinDistributedLockAsync(string connectionString,
        Func<Task<IStatusGeneric>> runInLockAsync)
    {
        //The https://github.com/madelson/DistributedLock doesn't support Sqlite for locking
        //so we just use the File lock
        //NOTE: DistributedLock does support many database types and its fairly easy to build a LockAndRun method
        var myDistributedLock = new FileDistributedLock(GetDirectoryInfoToLockWithCheck(), "MyLockName");
        using (await myDistributedLock.AcquireAsync())
        {
            return await runInLockAsync.Invoke();
        }
    }

    private DirectoryInfo GetDirectoryInfoToLockWithCheck()
    {
        if (string.IsNullOrEmpty(_options.PathToFolderToLock))
            throw new AuthPermissionsBadDataException(
                $"The {nameof(AuthPermissionsOptions.PathToFolderToLock)} property in the {nameof(AuthPermissionsOptions)}" +
                " must be set to a directory that all the instances of your application can access.");

        return new DirectoryInfo(_options.PathToFolderToLock);
    }
}