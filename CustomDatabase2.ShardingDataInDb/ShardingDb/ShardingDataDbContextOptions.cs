// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;

namespace CustomDatabase2.ShardingDataInDb.ShardingDb;

/// <summary>
/// This is used to set the default <see cref="DatabaseInformation"/>.
/// The default <see cref="DatabaseInformation"/> settings are:
/// - The Name of the default entry will be "Default Database". This should match the
/// <see cref="AuthPermissionsOptions.ShardingDefaultDatabaseInfoName"/>.
/// - The ConnectionName of the default entry is "DefaultConnection"
///
/// This allows you to change these if you want to. In particular, you can use a different connection string  
/// </summary>
public class ShardingDataDbContextOptions
{
    /// <summary>
    /// This defines the name of the default <see cref="DatabaseInformation"/> entry
    /// </summary>
    public string DefaultDatabaseInfoName { get; } = "Default Database";

    /// <summary>
    /// This defines the name of the default <see cref="DatabaseInformation"/> entry
    /// </summary>
    public string DefaultDatabaseInfoConnectionName { get; } = "DefaultConnection";
}