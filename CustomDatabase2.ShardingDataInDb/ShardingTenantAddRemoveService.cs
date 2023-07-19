// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
using StatusGeneric;

namespace CustomDatabase2.ShardingDataInDb;

/// <summary>
/// This service makes managing shard tenants (i.e. tenants that has its own database) where the
/// tenant's HasOwnDb properly is true.
/// Each shard tenant needs a <see cref="DatabaseInformation"/> entry to define the database
/// before you can create the tenant. And for delete of a shard tenant its good to remove the
/// <see cref="DatabaseInformation"/> entry.
/// NOTE: This can also handle a hybrid approach where some tenants share a database
/// - where the  tenant's HasOwnDb properly is false. For this type of tenant it assumes a
/// <see cref="DatabaseInformation"/> is already been set up. It also doesn't delete
/// <see cref="DatabaseInformation"/> entries on sharing tenants share a database.
/// </summary>
public class ShardingTenantAddRemoveService : IShardingTenantAddRemove
{
    private readonly IAuthTenantAdminService _tenantAdmin;
    private readonly IShardingConnections _getShardings;
    private readonly IAccessDatabaseInformationVer5 _setShardings;
    private readonly AuthPermissionsOptions _options;
    private readonly IDefaultLocalizer _localizeDefault;

    /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
    public ShardingTenantAddRemoveService(IAuthTenantAdminService tenantAdmin,
        IShardingConnections getShardings, IAccessDatabaseInformationVer5 setShardings,
        AuthPermissionsOptions options, IAuthPDefaultLocalizer localizeProvider)
    {
        _tenantAdmin = tenantAdmin ?? throw new ArgumentNullException(nameof(tenantAdmin));
        _getShardings = getShardings ?? throw new ArgumentNullException(nameof(getShardings));
        _setShardings = setShardings ?? throw new ArgumentNullException(nameof(setShardings));
        _options = options;
        _localizeDefault = localizeProvider.DefaultLocalizer;

        if (!_options.TenantType.IsSharding())
            throw new AuthPermissionsException("This service is specifically designed for sharding multi-tenants " +
                                               "and you are not using a sharding.");

        if (_setShardings.GetType() == typeof(ShardingConnectionsJsonFile))
            throw new AuthPermissionsBadDataException(
                $"This service doesn't work with the {nameof(ShardingConnectionsJsonFile)}. " +
                $"You must use a shading service that can create and read within the same request.");
    }

    /// <summary>
    /// This creates a tenant which has its own database, i.e. a sharding tenant (tenant's HasOwnDb is true),
    /// and provides a new sharding entry to contain the new database name. If a tenant that shares a database
    /// (tenant's HasOwnDb properly is false), then it use the <see cref="DatabaseInformation"/> defined by the
    /// <see cref="ShardingTenantAddDto.DatabaseInfoName"/> in the <see cref="ShardingTenantAddDto"/>.
    /// </summary>
    /// <param name="dto">A class called <see cref="ShardingTenantAddDto"/> holds all the data needed,
    /// including a method to validate that the information is correct.</param>
    /// <returns>status</returns>
    public async Task<IStatusGeneric> CreateShardingTenantAndConnectionAsync(ShardingTenantAddDto dto)
    {
        dto.ValidateProperties();
        if (!_options.TenantType.IsHierarchical() && dto.ParentTenantId != 0)
            throw new AuthPermissionsException("The parentTenantId parameter must be zero if for SingleLevel.");

        var status = new StatusGenericLocalizer(_localizeDefault);
        if (_tenantAdmin.QueryTenants().Any(x => x.TenantFullName == dto.TenantName))
            return status.AddErrorFormattedWithParams("DuplicateTenantName".ClassLocalizeKey(this, true),
                $"The tenant name '{dto.TenantName}' is already used", nameof(dto.TenantName));

        //1. We obtain an information data via the ShardingTenantAddDto class
        DatabaseInformation? databaseInfo = null;
        if (dto.HasOwnDb == true && dto.ParentTenantId == 0)
        {
            databaseInfo = dto.FormDatabaseInformation();
            if (status.CombineStatuses(
                    _setShardings.AddDatabaseInfoToShardingInformation(databaseInfo)).HasErrors)
                return status;
        }
        else if(!(_options.TenantType.IsHierarchical() && dto.ParentTenantId != 0))
        {
            //if a child hierarchical tenant we don't need to get the DatabaseInformation as the parent's DatabaseInformation is used

            databaseInfo = _getShardings.GetAllPossibleShardingData()
                .SingleOrDefault(x => x.Name == dto.DatabaseInfoName);
            if (databaseInfo == null)
                return status.AddErrorFormatted("MissingDatabaseInformation".ClassLocalizeKey(this, true),
                    $"The DatabaseInformation with the name '{dto.DatabaseInfoName}' wasn't found.");
        }

        //2. Now we can create the tenant, which in turn will setup the database via your ITenantChangeService implementation
        if (_options.TenantType.IsSingleLevel())
            status.CombineStatuses(await _tenantAdmin.AddSingleTenantAsync(dto.TenantName, dto.TenantRoleNames,
                dto.HasOwnDb, databaseInfo.Name));
        else
        {
            status.CombineStatuses(await _tenantAdmin.AddHierarchicalTenantAsync(dto.TenantName,
                dto.ParentTenantId, dto.TenantRoleNames,
                dto.HasOwnDb, databaseInfo?.Name));
        }

        if (status.HasErrors && dto.HasOwnDb == true)
        {
            //we created a DatabaseInformation, so we want to delete it
            status.CombineStatuses(
                await _setShardings.RemoveDatabaseInfoFromShardingInformationAsync(databaseInfo.Name));
        }

        return status;
    }

    /// <summary>
    /// This will delete a tenant (shared or shard), and if that tenant is a shard (i.e. has its own database)
    /// it will also delete the <see cref="DatabaseInformation"/> entry for this shard tenant.
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    /// <returns>status</returns>
    public async Task<IStatusGeneric> DeleteTenantAndConnectionAsync(int tenantId)
    {
        var status = new StatusGenericLocalizer(_localizeDefault);

        //1. We find the tenant to get HasOwnDb. If true, then we hold the DatabaseInfoName to delete the sharding entry
        var tenantStatus = await _tenantAdmin.GetTenantViaIdAsync(tenantId);
        if (status.CombineStatuses(tenantStatus).HasErrors)
            return status;

        string? databaseInfoName = tenantStatus.Result.HasOwnDb && tenantStatus.Result.ParentTenantId == null
            ? tenantStatus.Result.DatabaseInfoName
            : null;

        //2. We delete the tenant
        if (status.CombineStatuses(await _tenantAdmin.DeleteTenantAsync(tenantId)).HasErrors)
            return status;

        //3. If the tenant was successfully deleted, and the tenant's HasOwnDb is true, then we delete the DatabaseInformation
        if (databaseInfoName != null)
        {
            //We ignore any errors
            await _setShardings.RemoveDatabaseInfoFromShardingInformationAsync(databaseInfoName);
        }

        return status;
    }
}