﻿// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;

namespace CustomDatabase2.WebApp.Sharding.Models;

public class DatabaseInformationEdit 
{
    public DatabaseInformation DatabaseInfo { get; set; }

    public IEnumerable<string> AllPossibleConnectionNames { get; set; }

    public string[] PossibleDatabaseTypes { get; set; }
}