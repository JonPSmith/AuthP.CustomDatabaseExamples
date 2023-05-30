// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SetupCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunMethodsSequentially;

namespace CustomDatabase1.SqliteCustomParts;

public static class SqliteSetupExtensions
{
    /// <summary>
    /// This registers your custom database provider, in this case a Sqlite database.
    /// This code will set the type of the database provider to <see cref="AuthPDatabaseTypes.CustomDatabase"/>
    /// and your code will register your database provider and also sets the RunMethodsSequentially code too.
    /// </summary>
    /// <param name="setupData"></param>
    /// <param name="connectionString">The connection string to your Sqlite database</param>
    /// <param name="customConfiguration">Optional: This contains custom configuration code which is run
    /// within the <see cref="DbContext.OnModelCreating"/></param> of the <see cref="AuthPermissionsDbContext"/>
    /// <returns></returns>
    /// <exception cref="AuthPermissionsException"></exception>
    /// <exception cref="AuthPermissionsBadDataException"></exception>
    public static AuthSetupData UsingEfCoreSqlite(this AuthSetupData setupData, 
        string connectionString,
        ICustomConfiguration? customConfiguration = null)
    {
        connectionString.CheckConnectString();

        if (setupData.Options.InternalData.AuthPDatabaseType != AuthPDatabaseTypes.NotSet)
            throw new AuthPermissionsException("You have already set up a database type for AuthP.");

        //This tells AuthP that you are providing a non-standard database provider
        setupData.Options.InternalData.AuthPDatabaseType = AuthPDatabaseTypes.CustomDatabase;

        setupData.Services.AddDbContext<AuthPermissionsDbContext>(
            options =>
            {
                //This registers the Sqlite 
                options.UseSqlite(connectionString, dbOptions =>
                    dbOptions.MigrationsHistoryTable(AuthDbConstants.MigrationsHistoryTableName)
                    .MigrationsAssembly("CustomDatabase1.SqliteCustomParts"));
                EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(options);
            });

        //This registered your custom configuration to run inside the AuthPermissionsDbContext
        if (customConfiguration != null)
            setupData.Services.AddSingleton(x => customConfiguration);

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

        return setupData;
    }
}