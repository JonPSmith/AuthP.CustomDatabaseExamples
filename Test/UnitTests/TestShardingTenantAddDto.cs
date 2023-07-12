// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using CustomDatabase2.ShardingDataInDb;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests;

public class TestShardingTenantAddDto
{
    private readonly ITestOutputHelper _output;

    public TestShardingTenantAddDto(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestValidateProperties_HasOwnDbTrue_Ok()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "Test",
            HasOwnDb = true,
            ConnectionStringName = "DefaultConnection",
            DbProviderShortName = "SqlServer",
        };

        //ATTEMPT
        dto.ValidateProperties();
        var data = dto.FormDatabaseInformation();

        //VERIFY
        _output.WriteLine(data.ToString());
        data.Name.ShouldEndWith("-Test");
        data.ConnectionName.ShouldEqual("DefaultConnection");
        data.DatabaseType.ShouldEqual("SqlServer");
    }

    [Fact]
    public void TestValidateProperties_HasOwnDbFalse_Ok()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "Test",
            HasOwnDb = false,
            DatabaseInfoName = "Entry Name"
        };

        //ATTEMPT
        dto.ValidateProperties();

        //VERIFY
    }

    [Theory]
    [InlineData("TenantName")]
    [InlineData("ConnectionStringName")]
    [InlineData("DbProviderShortName")]
    public void TestValidateProperties_HasOwnDbTrue_Bad(string stopAdd)
    {
        //SETUP
        var dto = new ShardingTenantAddDto { HasOwnDb = true };
        if (stopAdd != "TenantName")
            dto.TenantName = "Test";
        if (stopAdd != "ConnectionStringName")
            dto.ConnectionStringName = "DefaultConnection";
        if (stopAdd != "DbProviderShortName")
            dto.DbProviderShortName = "SqlServer";

        //ATTEMPT
        try
        {
            dto.ValidateProperties();
        }
        catch (Exception e)
        {
            _output.WriteLine(e.Message);
            return;
        }

        //VERIFY
        true.ShouldBeFalse("Should have had an exception ");
    }

    [Theory]
    [InlineData("TenantName")]
    [InlineData("DatabaseInfoName")]
    public void TestValidateProperties_HasOwnDbFalse_Bad(string stopAdd)
    {
        //SETUP
        var dto = new ShardingTenantAddDto { HasOwnDb = false };
        if (stopAdd != "TenantName")
            dto.TenantName = "Test";
        if (stopAdd != "DatabaseInfoName")
            dto.DatabaseInfoName = "Entry Name";

        //ATTEMPT
        try
        {
            dto.ValidateProperties();
        }
        catch (Exception e)
        {
            _output.WriteLine(e.Message);
            return;
        }

        //VERIFY
        true.ShouldBeFalse("Should have had an exception ");
    }
}