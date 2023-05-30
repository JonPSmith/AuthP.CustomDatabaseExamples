// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using StatusGeneric;

namespace Test.StubClasses;

/// <summary>
/// This stub only has the following methods that do anything
/// - AddDatabaseInfoToShardingInformation
/// - GetDatabaseInformationByName
/// - RemoveDatabaseInfoFromShardingInformationAsync
/// </summary>
public class StubAccessDatabaseInformationVer5 : IAccessDatabaseInformationVer5
{
    public DatabaseInformation DatabaseInfoFromCode { get; private set; }
    public string CalledMethodName { get; private set; }


    /// <summary>
    /// This will return a list of <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> in the sharding settings file in the application
    /// </summary>
    /// <returns>If data, then returns the default list. This handles the situation where the <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> isn't set up.</returns>
    public List<DatabaseInformation> ReadAllShardingInformation()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// This returns the <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> where its <see cref="P:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation.Name" /> matches the databaseInfoName property.
    /// </summary>
    /// <param name="databaseInfoName">The Name of the <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> you are looking for</param>
    /// <returns>If no matching database information found, then it returns null</returns>
    public DatabaseInformation GetDatabaseInformationByName(string databaseInfoName)
    {
        CalledMethodName = nameof(AddDatabaseInfoToShardingInformation);
        return new DatabaseInformation
        {
            Name = databaseInfoName,
            ConnectionName = "DefaultConnection",
            DatabaseName = databaseInfoName,
            DatabaseType = "Sqlite"
        };
    }

    /// <summary>
    /// This adds a new <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> to the list in the current sharding settings file.
    /// If there are no errors it will update the sharding settings file in the application.
    /// </summary>
    /// <param name="databaseInfo">Adds a new <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> with the <see cref="P:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation.Name" /> to the sharding data.</param>
    /// <returns>status containing a success message, or errors</returns>
    public IStatusGeneric AddDatabaseInfoToShardingInformation(DatabaseInformation databaseInfo)
    {
       DatabaseInfoFromCode = databaseInfo;
       CalledMethodName = nameof(AddDatabaseInfoToShardingInformation);

       return new StatusGenericHandler();
    }

    /// <summary>
    /// This updates a <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> already in the sharding settings file.
    /// It uses the <see cref="P:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation.Name" /> in the provided in the <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> parameter.
    /// If there are no errors it will update the sharding settings file in the application.
    /// </summary>
    /// <param name="databaseInfo">Looks for a <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> with the <see cref="P:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation.Name" /> and updates it.</param>
    /// <returns>status containing a success message, or errors</returns>
    public IStatusGeneric UpdateDatabaseInfoToShardingInformation(DatabaseInformation databaseInfo)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// This removes a <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> with the same <see cref="P:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation.Name" /> as the databaseInfoName.
    /// If there are no errors it will update the sharding settings file in the application
    /// </summary>
    /// <param name="databaseInfoName">Looks for a <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> with the <see cref="P:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation.Name" /> and removes it.</param>
    /// <returns>status containing a success message, or errors</returns>
    public Task<IStatusGeneric> RemoveDatabaseInfoFromShardingInformationAsync(string databaseInfoName)
    {
        DatabaseInfoFromCode = new DatabaseInformation { Name = databaseInfoName };
        CalledMethodName = nameof(RemoveDatabaseInfoFromShardingInformationAsync);

        return Task.FromResult< IStatusGeneric>(new StatusGenericHandler());
    }
}