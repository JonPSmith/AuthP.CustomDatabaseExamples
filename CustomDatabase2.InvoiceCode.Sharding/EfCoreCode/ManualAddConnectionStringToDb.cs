// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.GetDataKeyCode;

namespace CustomDatabase2.InvoiceCode.Sharding.EfCoreCode;

/// <summary>
/// This class allows you inject a connection string into a DbContext.
/// Used in <see cref="ITenantChangeService"/> service and in unit tests
/// </summary>
public class ManualAddConnectionStringToDb : IGetShardingDataFromUser
{
    /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
    public ManualAddConnectionStringToDb(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string DataKey { get; }
    public string ConnectionString { get; }
}