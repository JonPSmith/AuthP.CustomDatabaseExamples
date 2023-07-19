// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.CommonCode;
using Microsoft.IdentityModel.Tokens;

namespace CustomDatabase2.ShardingDataInDb;

/// <summary>
/// This is used if you want to use the <see cref="IShardingTenantAddRemove"/>'s
/// <see cref="ShardingTenantAddRemoveService.CreateShardingTenantAndConnectionAsync"/>.
/// It contains the properties to create a new tenant, either tenants that there own database
/// (HasOwnDb == true) or tenants that share a database (HasOwnDb == false)
/// </summary>
public class ShardingTenantAddDto
{
    /// <summary>
    /// Required: The name of the new tenant to create
    /// </summary>
    public string TenantName { get; set; }

    /// <summary>
    /// Defines if the tenant should have its own database - defaults to true
    /// </summary>
    public bool? HasOwnDb { get; set; }

    /// <summary>
    /// If adding a child hierarchical, then this must be set to a id of the parent hierarchical tenant
    /// </summary>
    public int ParentTenantId { get; set; } = 0;

    /// <summary>
    /// If you are adding a hybrid tenant (i.e. HasOwnDb == false), then you provide the name of the
    /// <see cref="DatabaseInformation"/> entry that should already in the sharding data.
    /// </summary>
    public string? DatabaseInfoName { get; set; }

    /// <summary>
    /// Optional: List of tenant role names 
    /// </summary>
    public List<string> TenantRoleNames { get; set; } = new List<string>();

    /// <summary>
    /// The name of the connection string which defines the database server to use
    /// if adding a new <see cref="DatabaseInfoName"/> entry
    /// </summary>
    public string? ConnectionStringName { get; set; }

    /// <summary>
    /// The short name (e.g. SqlServer) of the database provider for this tenant
    /// if adding a new <see cref="DatabaseInfoName"/> entry
    /// </summary>
    public string? DbProviderShortName { get; set; }

    /// <summary>
    /// This ensures the data provided is valid
    /// </summary>
    /// <exception cref="AuthPermissionsBadDataException"></exception>
    public void ValidateProperties()
    {
       if (TenantName.IsNullOrEmpty())
           throw new AuthPermissionsBadDataException("Should not be null or empty", nameof(TenantName));

       if (HasOwnDb == null)
           throw new AuthPermissionsBadDataException(
               $"You must set the {nameof(HasOwnDb)} to true (has own db) or false (shares a database)", nameof(HasOwnDb));

       if (HasOwnDb == false && DatabaseInfoName.IsNullOrEmpty())
           throw new AuthPermissionsBadDataException(
               $"The {nameof(HasOwnDb)} is false so you need to provide {nameof(DatabaseInfoName)} ", nameof(DatabaseInfoName));

       if (ParentTenantId != 0 && !DatabaseInfoName.IsNullOrEmpty())
           throw new AuthPermissionsBadDataException("If you are adding a child hierarchical (i.e. " + 
                $"{nameof(ParentTenantId)} isn't null), then you should NOT provide a {nameof(DatabaseInfoName)}.", nameof(ParentTenantId));

       if (HasOwnDb == true && ParentTenantId == 0)
       {
           if (ConnectionStringName.IsNullOrEmpty())
               throw new AuthPermissionsBadDataException(
                   $"The {nameof(HasOwnDb)} is true so you need to provide {nameof(ConnectionStringName)} ", nameof(ConnectionStringName));

           if (DbProviderShortName.IsNullOrEmpty())
               throw new AuthPermissionsBadDataException(
                   $"The {nameof(HasOwnDb)} is true so you need to provide {nameof(DbProviderShortName)} ", nameof(DbProviderShortName));
       }
    }

    /// <summary>
    /// This will build the <see cref="DatabaseInformation"/> when you add a shard tenant.
    /// NOTE: I have used a datetime for the database name for the reasons covered in the comments.
    /// If you want to change the <see cref="DatabaseInformation"/>'s Name or the DatabaseName,
    /// then you can create a new class and override the <see cref="FormDatabaseInformation"/> method.
    /// See https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/override
    /// </summary>
    /// <returns></returns>
    /// <exception cref="AuthPermissionsException"></exception>
    public virtual DatabaseInformation? FormDatabaseInformation()
    {
        if (HasOwnDb != true)
            throw new AuthPermissionsException("There is already a DatabaseInformation for this tenant.");

        var dateTimeNow = DateTime.UtcNow;
        return new DatabaseInformation
        {            
            //NOTE: I don't include the tenant name in the database name because
            //1. The tenant name can be changed, but you can't always the change the database name 
            //2. PostgreSQL has a 64 character limit on the name of a database
            Name = $"{dateTimeNow.ToString("yyyyMMddHHmmss")}-{TenantName}",
            DatabaseName = dateTimeNow.ToString("yyyyMMddHHmmss-fff"),
            ConnectionName = ConnectionStringName,
            DatabaseType = DbProviderShortName
        };
    }
}