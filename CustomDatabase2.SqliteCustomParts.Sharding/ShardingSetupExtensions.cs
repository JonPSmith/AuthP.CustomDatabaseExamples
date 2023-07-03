// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.AccessTenantData;
using AuthPermissions.AspNetCore.AccessTenantData.Services;
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
using Microsoft.Extensions.DependencyInjection;
using RunMethodsSequentially;

namespace CustomDatabase2.CustomParts.Sharding;

public static class ShardingSetupExtensions
{
    /// <summary>
    /// This registers your custom database provider, in this case a PostgreSQL provider.
    /// This code will set the type of the database provider to <see cref="AuthPDatabaseTypes.CustomDatabase"/>
    /// and your code will register your database provider and also sets the RunMethodsSequentially code too.
    /// </summary>
    /// <param name="setupData"></param>
    /// <param name="postgresConnectionString">The connection string to your PostgreSQL database
    /// within the <see cref="DbContext.OnModelCreating"/> of the <see cref="AuthPermissionsDbContext"/>
    /// and the <see cref="ShardingDataDbContext"/></param>
    /// <returns></returns>
    /// <exception cref="AuthPermissionsException"></exception>
    /// <exception cref="AuthPermissionsBadDataException"></exception>
    public static AuthSetupData SetupMultiTenantShardingWithSqlite(this AuthSetupData setupData, 
        string postgresConnectionString, string sqlServerConnectionString)
    {
        postgresConnectionString.CheckConnectString();

        if (!setupData.Options.TenantType.IsMultiTenant())
            throw new AuthPermissionsException(
                $"You must define what type of multi-tenant structure you want, i.e {TenantTypes.SingleLevel} or {TenantTypes.HierarchicalTenant}.");

        setupData.Options.TenantType |= TenantTypes.AddSharding;

        if (setupData.Options.Configuration == null)
            throw new AuthPermissionsException(
                $"You must set the {nameof(AuthPermissionsOptions.Configuration)} to the ASP.NET Core Configuration when using Sharding");

        //This gets access to the ConnectionStrings
        setupData.Services.Configure<ConnectionStringsOption>(setupData.Options.Configuration.GetSection("ConnectionStrings"));
        //CHANGE: I have to use a database to hold the sharding data because the IOptionsMonitor doesn't pick up a change immediately
        //This removes the registering the ShardingSettingsOption and sharding settings json file 
        setupData.Services.AddTransient<IAccessDatabaseInformationVer5, SetShardingDataViaDb>();
        setupData.Services.AddTransient<IShardingConnections, GetShardingDataViaDb>();
        //This provides the information for the default 
        setupData.Services.AddSingleton(new ShardingDataDbContextOptions());

        //Register the ShardingDataDbContext used to hold the sharding information
        //NOTE: remember to add a RegisterServiceToRunInJob to migrate the database on startup 
        setupData.Services.AddDbContext<ShardingDataDbContext>(
            options =>
            {
                //This registers this to the  
                options.UseNpgsql(postgresConnectionString, dbOptions =>
                    dbOptions.MigrationsHistoryTable("__ShardingDataInDbHistory")
                        .MigrationsAssembly("CustomDatabase2.ShardingDataInDb"));
            });

        
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
        //Because we are using Postgres as a custom database we register the Postgres specific method
        setupData.Services.AddScoped<IDatabaseSpecificMethods, PostgresDatabaseSpecificMethods>();
        //And we need to register SqlServer as that is what the individual tenant databases use
        setupData.Services.AddScoped<IDatabaseSpecificMethods, SqlServerDatabaseSpecificMethods>();

        //We define the Postgres AuthPermissionsDbContext 
        setupData.Services.AddDbContext<AuthPermissionsDbContext>(
            options =>
            {
                //This registers the Sqlite 
                options.UseNpgsql(postgresConnectionString, dbOptions =>
                    dbOptions.MigrationsHistoryTable(AuthDbConstants.MigrationsHistoryTableName)
                        .MigrationsAssembly("AuthPermissions.PostgreSql"));
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
                            "directory that all the instances of your application can access. " +
                            "This is a backup to the Postgres lock in cases where the database doesn't exist yet.");

                    options.AddSqlServerLockAndRunMethods(sqlServerConnectionString);
                    options.AddFileSystemLockAndRunMethods(setupData.Options.PathToFolderToLock);
                }
                else
                    options.AddRunMethodsWithoutLock();
            });

        #endregion


        return setupData;
    }
}