﻿// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace CustomDatabase1.InvoiceCode.Dtos;

public enum TenantVersionTypes
{
    //Error
    NotSet,
    //Only allows one user per tenant
    Free,
    //Allows many users in one tenant
    Pro,
    //Have your own admin user
    Enterprise
}