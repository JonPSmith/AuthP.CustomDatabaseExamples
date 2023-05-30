// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using CustomDatabase1.SqliteCustomParts;
using Microsoft.EntityFrameworkCore;
using TestSupport.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace Test;

public class RunThisToGetExtraMigrateParts
{
    private readonly ITestOutputHelper _output;

    public RunThisToGetExtraMigrateParts(ITestOutputHelper output)
    {
        _output = output;
    }

    //https://www.bricelam.net/2020/08/07/sqlite-and-efcore-concurrency-tokens.html
    /// <summary>
    /// This test is used to create the trigger SQL for the concurrency tokens.
    /// (see https://www.bricelam.net/2020/08/07/sqlite-and-efcore-concurrency-tokens.html)
    /// This is a crude way to output the code to add to the migration, because only SQLite needs extra SQL code.
    /// If your database needs extra SQL code added to the migration then look at
    /// https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/operations
    /// </summary>
    [RunnableInDebugOnly]
    public void CreateSqliteTriggerCommands()
    {
        var options = new DbContextOptionsBuilder<AuthPermissionsDbContext>()
            //this code doesn't access the database - it just needs a valid connection string
            .UseSqlite($"Data Source=database.sqlite") 
            .Options;
        var context = new AuthPermissionsDbContext(options, null, new SqliteDbConfig());

        foreach (var tableName in context.Model.GetEntityTypes().Select(t => t.GetTableName()).Distinct())
        {
            var triggerCode = BuildTriggerSql(tableName);
            _output.WriteLine("migrationBuilder.Sql(\r\n@\""+ triggerCode + "\");");
            _output.WriteLine("");
        }
    }

    private string BuildTriggerSql(string tableName)
    {
        var sqlLines = new[]
        {
            $"CREATE TRIGGER Update{tableName}Version",
            $"    AFTER UPDATE ON {tableName}",
            "    BEGIN",
            $"UPDATE {tableName}",
            "SET Version = Version + 1",
            "WHERE rowid = NEW.rowid;",
            "END;"
        };

        return string.Join("\r\n", sqlLines);
    }
}