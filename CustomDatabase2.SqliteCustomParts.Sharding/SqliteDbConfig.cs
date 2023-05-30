// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CustomDatabase2.SqliteCustomParts.Sharding;

public class SqliteDbConfig : ICustomConfiguration
{
    /// <summary>This method allows you add extra configuration code to the AuthP's
    /// <see cref="AuthPermissionsDbContext"/>  when you are using a custom database provider.
    /// Typical configuration code are to handle . 
    /// </summary>
    /// <param name="modelBuilder"></param>
    public void ApplyCustomConfiguration(ModelBuilder modelBuilder)
    {
        //This will provide the first part to the SQLite concurrency tokens
        //See https://www.bricelam.net/2020/08/07/sqlite-and-efcore-concurrency-tokens.html
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            entityType.AddProperty("Version", typeof(int));
            entityType.FindProperty("Version")
                .ValueGenerated = ValueGenerated.OnAddOrUpdate;
            entityType.FindProperty("Version")
                .SetDefaultValue(0);
            entityType.FindProperty("Version")
                .IsConcurrencyToken = true;
        }
    }
}