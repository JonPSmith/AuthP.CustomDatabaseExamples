﻿// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.BaseCode;
using CustomDatabase2.ShardingDataInDb.ShardingDb;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;
using Test.TestHelpers;

namespace Test.UnitTests;

public class TestShardingDataDbContext
{
    [Fact]
    public void FormDefaultDatabaseInfo_EnsureCreated_Empty()
    {
        //SETUP
        var setup = new DatabaseInformationOptions(false);
        var options = SqliteInMemory.CreateOptions<ShardingDataDbContext>(
            builder => builder.ReplaceService<IModelCacheKeyFactory, ShardingDataDbContextCacheKeyFactory>());
        using var context = new ShardingDataDbContext(options, setup);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        //ATTEMPT
        var entries = context.ShardingData.ToList();

        //VERIFY
        entries.Count.ShouldEqual(0);

        context.Database.EnsureDeleted();
    }

    [Fact]
    public void FormDefaultDatabaseInfo_SqlServer()
    {
        //SETUP
        var authPOptions = new AuthPermissionsOptions
        {
            SecondPartOfShardingFile = "Test",
            InternalData =
            {
                AuthPDatabaseType = AuthPDatabaseTypes.SqlServer
            }
        };
        var setup = new DatabaseInformationOptions();
        setup.FormDefaultDatabaseInfo(authPOptions);
        var options = SqliteInMemory.CreateOptions<ShardingDataDbContext>(
            builder => builder.ReplaceService<IModelCacheKeyFactory, ShardingDataDbContextCacheKeyFactory>());
        using var context = new ShardingDataDbContext(options, setup);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        //ATTEMPT
        var databaseDefault = context.ShardingData.Single();

        //VERIFY
        databaseDefault.Name.ShouldEqual("Default Database");
        databaseDefault.DatabaseName.ShouldBeNull();
        databaseDefault.ConnectionName.ShouldEqual("DefaultConnection");
        databaseDefault.DatabaseType.ShouldEqual("SqlServer");
    }

    [Fact]
    public void FormDefaultDatabaseInfo_Custom_Ok()
    {
        //SETUP
        var authPOptions = new AuthPermissionsOptions
        {
            SecondPartOfShardingFile = "Test",
            InternalData =
            {
                AuthPDatabaseType = AuthPDatabaseTypes.CustomDatabase
            }
        };
        var setup = new DatabaseInformationOptions();
        setup.FormDefaultDatabaseInfo(authPOptions);
        setup.DatabaseType = "Sqlite";
        var options = SqliteInMemory.CreateOptions<ShardingDataDbContext>(
            builder => builder.ReplaceService<IModelCacheKeyFactory, ShardingDataDbContextCacheKeyFactory>());
        using var context = new ShardingDataDbContext(options, setup);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        //ATTEMPT
        var databaseDefault = context.ShardingData.Single();

        //VERIFY
        databaseDefault.Name.ShouldEqual("Default Database");
        databaseDefault.DatabaseName.ShouldBeNull();
        databaseDefault.ConnectionName.ShouldEqual("DefaultConnection");
        databaseDefault.DatabaseType.ShouldEqual("Sqlite");
    }

    [Fact]
    public void FormDefaultDatabaseInfo_Custom_Bad()
    {
        //SETUP
        var authPOptions = new AuthPermissionsOptions
        {
            SecondPartOfShardingFile = "Test",
            InternalData =
            {
                AuthPDatabaseType = AuthPDatabaseTypes.CustomDatabase
            }
        };
        var setup = new DatabaseInformationOptions();
        setup.FormDefaultDatabaseInfo(authPOptions);
        var options = SqliteInMemory.CreateOptions<ShardingDataDbContext>(
            builder => builder.ReplaceService<IModelCacheKeyFactory, ShardingDataDbContextCacheKeyFactory>());
        using var context = new ShardingDataDbContext(options, setup);

        //ATTEMPT
        try
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }
        catch (Exception e)
        {
            e.Message.ShouldEqual("You are using custom database, so you set the DatabaseType to the short form of the database provider name, e.g. SqlServer.");
            return;
        }

        //VERIFY
        true.ShouldBeFalse();
    }

}