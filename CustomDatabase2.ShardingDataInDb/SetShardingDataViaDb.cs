// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.SetupCode;
using CustomDatabase2.ShardingDataInDb.ShardingDb;
using LocalizeMessagesAndErrors;
using StatusGeneric;

namespace CustomDatabase2.ShardingDataInDb;

public class SetShardingDataViaDb : IAccessDatabaseInformationVer5
{
    private readonly ShardingDataDbContext _shardingContext;
    private readonly IShardingConnections _connectionsService;
    private readonly IDefaultLocalizer _localizeDefault;

    public SetShardingDataViaDb(ShardingDataDbContext shardingContext, 
        IShardingConnections connectionsService,
        IAuthPDefaultLocalizer localizeProvider)
    {
        _shardingContext = shardingContext;
        _connectionsService = connectionsService;
        _localizeDefault = localizeProvider.DefaultLocalizer;
    }

    /// <summary>
    /// This will return a list of <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" />
    /// in the sharding settings database
    /// </summary>
    /// <returns>Returns all the sharding information</returns>
    public List<DatabaseInformation> ReadAllShardingInformation()
    {
        return _shardingContext.ShardingData.ToList();
    }

    /// <summary>
    /// This returns the <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> where its
    /// <see cref="P:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation.Name" /> matches the databaseInfoName property.
    /// </summary>
    /// <param name="databaseInfoName">The Name of the <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> you are looking for</param>
    /// <returns>If no matching database information found, then it returns null</returns>
    public DatabaseInformation? GetDatabaseInformationByName(string databaseInfoName)
    {
        return _shardingContext.ShardingData.SingleOrDefault(x => x.Name == databaseInfoName);
    }

    /// <summary>
    /// This adds a new <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" />
    /// to the list in the current sharding settings file.
    /// If there are no errors it will update the sharding settings file in the application.
    /// </summary>
    /// <param name="databaseInfo">Adds a new <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> with the <see cref="P:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation.Name" /> to the sharding data.</param>
    /// <returns>status containing a success message, or errors</returns>
    public IStatusGeneric AddDatabaseInfoToShardingInformation(DatabaseInformation databaseInfo)
    {
        var status = new StatusGenericLocalizer(_localizeDefault);
        status.SetMessageFormatted("SuccessAdd".ClassLocalizeKey(this, true),
            $"Successfully added the sharding data with the name of '{databaseInfo.Name}'.");

        if (status.CombineStatuses(CheckDatabasesInfoBeforeUpdate(databaseInfo, ChangeTypes.Add)).HasErrors)
            return status;

        _shardingContext.Add(databaseInfo);
        _shardingContext.SaveChanges();

        return status;
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
        var status = new StatusGenericLocalizer(_localizeDefault);
        status.SetMessageFormatted("SuccessUpdate".ClassLocalizeKey(this, true),
            $"Successfully updated the sharding data with the name of '{databaseInfo.Name}'.");

        if (status.CombineStatuses(CheckDatabasesInfoBeforeUpdate(databaseInfo, ChangeTypes.Update)).HasErrors)
            return status;

        _shardingContext.Update(databaseInfo);
        _shardingContext.SaveChanges();

        return status;
    }

    /// <summary>
    /// This removes a <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> with the same <see cref="P:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation.Name" /> as the databaseInfoName.
    /// If there are no errors it will update the sharding settings file in the application
    /// </summary>
    /// <param name="databaseInfoName">Looks for a <see cref="T:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation" /> with the <see cref="P:AuthPermissions.AspNetCore.ShardingServices.DatabaseInformation.Name" /> and removes it.</param>
    /// <returns>status containing a success message, or errors</returns>
    public async Task<IStatusGeneric> RemoveDatabaseInfoFromShardingInformationAsync(string databaseInfoName)
    {
        var status = new StatusGenericLocalizer(_localizeDefault);
        status.SetMessageFormatted("SuccessDelete".ClassLocalizeKey(this, true),
            $"Successfully deleted the sharding data with the name of '{databaseInfoName}'.");

        if (status.CombineStatuses(CheckDatabasesInfoBeforeUpdate(
                new DatabaseInformation{ Name = databaseInfoName },ChangeTypes.Delete)).HasErrors)
            return status;

        var entityToDelete = _shardingContext.ShardingData.SingleOrDefault(x => x.Name == databaseInfoName);

        _shardingContext.Remove(entityToDelete);
        await _shardingContext.SaveChangesAsync();

        return status;
    }

    //-------------------------------------------------
    //private methods

    private enum ChangeTypes {Add, Update, Delete}

    /// <summary>
    /// This checks:
    /// - All:            That the Name contains characters
    /// - Add, Update:    That the ConnectionName and DatabaseType contains characters
    /// - Update, Delete: Entry is there
    /// - Add:            Entry isn't there
    /// - Add, Update:    Test TestFormingConnectionString
    /// </summary>
    /// <param name="changedInfo"></param>
    /// <returns></returns>
    private IStatusGeneric CheckDatabasesInfoBeforeUpdate(DatabaseInformation changedInfo, ChangeTypes change)
    {
        var status = new StatusGenericLocalizer(_localizeDefault);

        //All: Check that the Name contains characters
        if (string.IsNullOrWhiteSpace(changedInfo.Name))
            return status.AddErrorString("NameMissing".ClassLocalizeKey(this, true),
                $"The {nameof(DatabaseInformation.Name)} is null or empty, which isn't allowed.");

        //Add, Update: Check that the ConnectionName contains characters
        if (change != ChangeTypes.Delete && string.IsNullOrWhiteSpace(changedInfo.ConnectionName))
            return status.AddErrorString("ConnectionNameMissing".ClassLocalizeKey(this, true),
                $"The {nameof(DatabaseInformation.ConnectionName)} is null or empty, which isn't allowed.");

        //Add, Update: Check that the DatabaseType contains characters
        if (change != ChangeTypes.Delete && string.IsNullOrWhiteSpace(changedInfo.DatabaseType))
            return status.AddErrorString("DatabaseTypeMissing".ClassLocalizeKey(this, true),
                $"The {nameof(DatabaseInformation.DatabaseType)} is null or empty, which isn't allowed.");

        var entryIsThere = _shardingContext.ShardingData.Any(x => x.Name == changedInfo.Name);
        //Update, Delete: Check that the entry is there
        if (!entryIsThere && (change == ChangeTypes.Update || change == ChangeTypes.Delete))
            return status.AddErrorFormatted("MissingEntry".ClassLocalizeKey(this, true),
                $"Could not find a entry with the Name {changedInfo.Name}.");
        //Add: Check if there is a entry with the same Name
        if (entryIsThere && change == ChangeTypes.Add)
            return status.AddErrorFormatted("DatabaseInfoDuplicate".ClassLocalizeKey(this, true),
                $"The {nameof(DatabaseInformation.Name)} of {changedInfo.Name} is already used.");

        //Add, Update: Check that the connection string is valid for the database provider
        if (change == ChangeTypes.Add || change == ChangeTypes.Update)
            status.CombineStatuses(_connectionsService.TestFormingConnectionString(changedInfo));

        return status;
    }
}