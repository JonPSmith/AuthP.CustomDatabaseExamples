// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
using StatusGeneric;

namespace CustomDatabase2.CustomParts.Sharding;

public class AddNewDbForNewTenantSqlServer : IGetDatabaseForNewTenant
{
    private readonly IAccessDatabaseInformationVer5 _accessShardingInfo;
    private readonly AuthPermissionsDbContext _context;
    private readonly ITenantChangeService _tenantChangeService;
    private readonly IDefaultLocalizer _localizeDefault;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="accessShardingInfo"></param>
    /// <param name="context"></param>
    /// <param name="tenantChangeService"></param>
    /// <param name="localizeProvider"></param>
    public AddNewDbForNewTenantSqlServer(
        IAccessDatabaseInformationVer5 accessShardingInfo,
        AuthPermissionsDbContext context,
        ITenantChangeService tenantChangeService, IAuthPDefaultLocalizer localizeProvider)
    {
        _accessShardingInfo = accessShardingInfo ?? throw new ArgumentNullException(nameof(accessShardingInfo));
        _context = context;
        _tenantChangeService = tenantChangeService ?? throw new ArgumentNullException(nameof(tenantChangeService));
        _localizeDefault = localizeProvider.DefaultLocalizer;
    }

    private Tenant? _tenant;
    private string? _tenantRef;

    /// <summary>
    /// This implementation creates a new SqlServer database for each tenant. The steps are:
    /// 1. Create the <see cref="DatabaseInformation"/> and store it in the shardingsettings
    /// 2. Its adds the sharding information to the tenant, and saves it.
    /// 3. Then it create the database for this tenant
    /// a sqlite database, and then add a DatabaseInformation to the shardingsettings json file.
    /// </summary>
    /// <param name="tenant">This is the tenant that you want to find/create a new database.
    /// NOTE: The tenant hasn't been written to the database at this stage, so the TenantId is zero.</param>
    /// <param name="hasOwnDb">If true the tenant needs its own database. False means it shares a database.</param>
    /// <param name="region">If not null this provides geographic information to pick the nearest database server.</param>
    /// <param name="version">Optional: provides the version name in case that effects the database selection</param>
    /// <returns>Status with the DatabaseInfoName, or error if it can't find a database to work with</returns>
    public async Task<IStatusGeneric<Tenant>> FindOrCreateDatabaseAsync(Tenant tenant, bool hasOwnDb, string region, string version = null)
    {
        _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        var status = new StatusGenericLocalizer<Tenant>(_localizeDefault);

        if (!hasOwnDb)
            status.AddErrorString("HasOwnDbBad".ClassLocalizeKey(this, true),
                "The HasOwnDb must be true as each tenant has there own database.");

        //First we create the sharding information or this new 
        _tenantRef = $"Tenant_{tenant.TenantId}";
        var databaseInfo = new DatabaseInformation
        {
            Name = _tenantRef,
            ConnectionName = "DefaultConnection",
            DatabaseName = _tenantRef,
            DatabaseType = "SqlServer"
        };

        //This adds a new DatabaseInformation to the shardingsettings
        if (status.CombineStatuses(_accessShardingInfo.AddDatabaseInfoToShardingInformation(databaseInfo))
            .HasErrors)
            return status;

        //Now set up the sharding parts of the tenant
        tenant.UpdateShardingState(databaseInfo.Name, hasOwnDb);
        if (status.CombineStatuses(await _context.SaveChangesWithChecksAsync(_localizeDefault)).HasErrors)
            return status;

        var errorString = await _tenantChangeService.CreateNewTenantAsync(tenant);
        if (errorString != null)
            return status.AddErrorString(this.AlreadyLocalized(), errorString);

        return status.SetResult(tenant);
    }

    /// <summary>
    /// If called it will undo what the FindOrCreateDatabaseAsync method did.
    /// This is called if there was a problem with the new user such that the new tenant would be deleted.
    /// </summary>
    /// <returns></returns>
    public async Task<IStatusGeneric> RemoveLastDatabaseSetupAsync()
    {
        var status = new StatusGenericLocalizer<string>(_localizeDefault);

        if (_accessShardingInfo.GetDatabaseInformationByName(_tenantRef) != null)
        {
            //There is a entry for the tenant so we remove the sharding info and the delete the database 
            await _tenantChangeService.SingleTenantDeleteAsync(_tenant);
            await _accessShardingInfo.RemoveDatabaseInfoFromShardingInformationAsync(_tenantRef);
        }

        return status;
    }
}