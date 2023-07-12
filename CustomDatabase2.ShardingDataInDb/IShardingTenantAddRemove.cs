// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.ShardingServices;
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
public interface IShardingTenantAddRemove
{
    /// <summary>
    /// This creates a tenant which has its own database, i.e. a sharding tenant (tenant's HasOwnDb is true),
    /// and provides a new sharding entry to contain the new database name. If a tenant that shares a database
    /// (tenant's HasOwnDb properly is false), then it use the <see cref="DatabaseInformation"/> defined by the
    /// <see cref="ShardingTenantAddDto.DatabaseInfoName"/> in the <see cref="ShardingTenantAddDto"/>.
    /// NOTE: This will ONLY work for adding a top-level hierarchical tenant, i.e the parentTenantId is zero.
    /// To add a child hierarchical tenant use the normal <see cref="IAuthTenantAdminService"/>.
    /// </summary>
    /// <param name="dto">A class called <see cref="ShardingTenantAddDto"/> holds all the data needed,
    /// including a method to validate that the information is correct.</param>
    /// <returns>status</returns>
    Task<IStatusGeneric> CreateShardingTenantAndConnectionAsync(ShardingTenantAddDto dto);

    /// <summary>
    /// This will delete a tenant (shared or shard), and if that tenant is a shard (i.e. has its own database)
    /// it will also delete the <see cref="DatabaseInformation"/> entry for this shard tenant.
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    /// <returns>status</returns>
    Task<IStatusGeneric> DeleteTenantAndConnectionAsync(int tenantId);
}