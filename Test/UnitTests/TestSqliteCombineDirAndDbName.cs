// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using CustomDatabase2.SqliteCustomParts.Sharding;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests;

public class TestSqliteCombineDirAndDbName
{
    [Theory]
    [InlineData("MyDir\\")]
    [InlineData("MyDir")]
    public void TestAddDirectoryToConnection(string dirName)
    {
        //SETUP
        var service = new SqliteCombineDirAndDbName("MyDir\\");

        //ATTEMPT
        var database = service.AddDirectoryToConnection("Data source={AppDir}\\OriginalName.sqlite");

        //VERIFY
        database.ShouldEqual("Data Source=MyDir\\OriginalName.sqlite");
    }

    [Fact]
    public void TestAddDirectoryToConnection_DefineDatabase()
    {
        //SETUP
        var service = new SqliteCombineDirAndDbName("MyDir\\");

        //ATTEMPT
        var database = service.AddDirectoryToConnection("Data source={AppDir}\\OriginalName", "MyDatabase");

        //VERIFY
        database.ShouldEqual("Data Source=MyDir\\MyDatabase.sqlite");
    }
}