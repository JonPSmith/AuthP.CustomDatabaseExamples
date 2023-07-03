// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.AccessTenantData.Services;
using AuthPermissions.AspNetCore.AccessTenantData;
using AuthPermissions.AspNetCore.GetDataKeyCode;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SetupCode;
using CustomDatabase2.ShardingDataInDb;
using CustomDatabase2.ShardingDataInDb.ShardingDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RunMethodsSequentially;

namespace CustomDatabase2.SqliteCustomParts.Sharding;

public static class SqliteSetupExtensions
{
    /// <summary>
    /// This registers your custom database provider, in this case a Sqlite database.
    /// This code will set the type of the database provider to <see cref="AuthPDatabaseTypes.CustomDatabase"/>
    /// and your code will register your database provider and also sets the RunMethodsSequentially code too.
    /// </summary>
    /// <param name="setupData"></param>
    /// <param name="connectionString">The connection string to your Sqlite database</param>
    /// <param name="combineDirAndDb">Implementation of the service to add the directory to a Sqlite connection string</param>
    /// <param name="customConfiguration">Optional: This contains custom configuration code which is run
    /// within the <see cref="DbContext.OnModelCreating"/></param> of the <see cref="AuthPermissionsDbContext"/>
    /// <returns></returns>
    /// <exception cref="AuthPermissionsException"></exception>
    /// <exception cref="AuthPermissionsBadDataException"></exception>
    public static AuthSetupData SetupMultiTenantShardingWithSqlite(this AuthSetupData setupData, 
        string connectionString,
        ISqliteCombineDirAndDbName combineDirAndDb,
        ICustomConfiguration? customConfiguration = null)
    {
        connectionString.CheckConnectString();

        if (!setupData.Options.TenantType.IsMultiTenant())
            throw new AuthPermissionsException(
                $"You must define what type of multi-tenant structure you want, i.e {TenantTypes.SingleLevel} or {TenantTypes.HierarchicalTenant}.");

        setupData.Options.TenantType |= TenantTypes.AddSharding;

        if (setupData.Options.Configuration == null)
            throw new AuthPermissionsException(
                $"You must set the {nameof(AuthPermissionsOptions.Configuration)} to the ASP.NET Core Configuration when using Sharding");

        //This gets access to the ConnectionStrings
        setupData.Services.Configure<ConnectionStringsOption>(setupData.Options.Configuration.GetSection("ConnectionStrings"));
        //This gets access to the ShardingData in the separate sharding settings file
        setupData.Services.Configure<ShardingSettingsOption>(setupData.Options.Configuration);
        //This adds the sharding settings file to the configuration
        var shardingFileName = AuthPermissionsOptions.FormShardingSettingsFileName(setupData.Options.SecondPartOfShardingFile);
        setupData.Options.Configuration.AddJsonFile(shardingFileName, optional: true, reloadOnChange: true);

        //CHANGE: I have to use a database to hold the sharding data because the IOptionsMonitor doesn't pick up a change immediately
        setupData.Services.AddTransient<IAccessDatabaseInformationVer5, SetShardingDataViaDb>();
        setupData.Services.AddTransient<IShardingConnections, GetShardingDataViaDb>();
        //This provides the information for the default 
        setupData.Services.AddSingleton(new ShardingDataDbContextOptions());

        //Register the ShardingDataDbContext used to hold the sharding information
        //NOTE: remember to add a RegisterServiceToRunInJob to migrate the database on startup 
        setupData.Services.AddSqlite<ShardingDataDbContext>(connectionString, dbOptions =>
            dbOptions.MigrationsHistoryTable("__ShardingDataMigration")
            .MigrationsAssembly("CustomDatabase2.ShardingDataInDb")
        );
        setupData.Services.AddTransient<ILinkToTenantDataService, LinkToTenantDataService>();

        switch (setupData.Options.LinkToTenantType)
        {
            case LinkToTenantTypes.OnlyAppUsers:
                setupData.Services
                    .AddScoped<IGetShardingDataFromUser, GetShardingDataUserAccessTenantData>();
                break;
            case LinkToTenantTypes.AppAndHierarchicalUsers:
                setupData.Services
                    .AddScoped<IGetShardingDataFromUser,
                        GetShardingDataAppAndHierarchicalUsersAccessTenantData>();
                break;
            default:
                setupData.Services.AddScoped<IGetShardingDataFromUser, GetShardingDataUserNormal>();
                break;
        }

        #region custom database parts

        setupData.Options.InternalData.AuthPDatabaseType = AuthPDatabaseTypes.CustomDatabase;
        //Because we are using a custom database we register the sqlite specific methods
        setupData.Services.AddScoped<IDatabaseSpecificMethods, SqliteSpecificMethods>();
        //SqliteSpecificMethods needs a way to combine the directory and the database name
        setupData.Services.AddSingleton(combineDirAndDb);

        //This sets up AuthP's "Migration on startup" feature, which 
        //This registered your custom configuration to run inside the AuthPermissionsDbContext
        if (customConfiguration != null)
            setupData.Services.AddSingleton(x => customConfiguration);

        //We define the Sqlite 
        setupData.Services.AddDbContext<AuthPermissionsDbContext>(
            options =>
            {
                //This registers the Sqlite 
                options.UseSqlite(connectionString, dbOptions =>
                    dbOptions.MigrationsHistoryTable(AuthDbConstants.MigrationsHistoryTableName)
                        .MigrationsAssembly("CustomDatabase2.SqliteCustomParts.Sharding"));
                EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(options);
            });

        //This provides the AuthP's "migrate on startup" feature
        setupData.Options.InternalData.RunSequentiallyOptions =
            setupData.Services.RegisterRunMethodsSequentially(options =>
            {
                if (setupData.Options.UseLocksToUpdateGlobalResources)
                {
                    if (string.IsNullOrEmpty(setupData.Options.PathToFolderToLock))
                        throw new AuthPermissionsBadDataException(
                            $"The {nameof(AuthPermissionsOptions.PathToFolderToLock)} property in the {nameof(AuthPermissionsOptions)} must be set to a " +
                            "directory that all the instances of your application can access. ");

                    //The https://github.com/madelson/DistributedLock doesn't support Sqlite for locking
                    //so we just use the File lock
                    //NOTE: DistributedLock does support many database types and its fairly easy to build a LockAndRun method
                    options.AddFileSystemLockAndRunMethods(setupData.Options.PathToFolderToLock);
                }
                else
                    options.AddRunMethodsWithoutLock();
            });

        #endregion


        return setupData;
    }
}