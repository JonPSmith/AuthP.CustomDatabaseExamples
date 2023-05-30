﻿// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.BaseCode.CommonCode;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace ExamplesCommonCode.IdentityCookieCode;

/// <summary>
/// This contains a method that will periodically refresh the claims of each logged-in user 
/// </summary>
public static class PeriodicCookieEvent
{
    /// <summary>
    /// Used in the "periodically update user's claims" feature
    /// </summary>
    public const string TimeToRefreshUserClaimType = "TimeToRefreshUserClaim";

    /// <summary>
    /// This method will be called on every HTTP request where a user is logged in (therefore you should keep the No change code quick)
    /// This method implements a way to update user's claims defined by a claim with the Type 
    /// <see cref="TimeToRefreshUserClaimType"/>, which contains the time by which the refresh should occur.
    /// </summary>
    /// <param name="context"></param>
    public static async Task PeriodicRefreshUsersClaims(CookieValidatePrincipalContext context)
    {
        var originalClaims = context.Principal.Claims.ToList();

        if (originalClaims.GetClaimDateTimeTicksValue(TimeToRefreshUserClaimType) < DateTime.UtcNow)
        {
            //Need to refresh the user's claims 
            var userId = originalClaims.GetUserIdFromClaims();
            if (userId == null)
                //this shouldn't happen, but best to return
                return;

            var claimsCalculator = context.HttpContext.RequestServices.GetRequiredService<IClaimsCalculator>();
            var newClaims = await claimsCalculator.GetClaimsForAuthUserAsync(userId);
            newClaims.AddRange(originalClaims.RemoveUpdatedClaimsFromOriginalClaims(newClaims)); //Copy over unchanged claims

            var identity = new ClaimsIdentity(newClaims, "Cookie");
            var newPrincipal = new ClaimsPrincipal(identity);
            context.ReplacePrincipal(newPrincipal);
            context.ShouldRenew = true;
        }
    }

    private static IEnumerable<Claim> RemoveUpdatedClaimsFromOriginalClaims(this List<Claim> originalClaims, List<Claim> newClaims)
    {
        var newClaimTypes = newClaims.Select(x => x.Type);
        return originalClaims.Where(x => !newClaimTypes.Contains(x.Type));
    }
}