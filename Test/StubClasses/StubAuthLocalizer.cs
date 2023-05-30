// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
using LocalizeMessagesAndErrors.UnitTestingCode;

namespace Test.StubClasses;

public class StubAuthLocalizer : IAuthPDefaultLocalizer
{
    /// <summary>
    /// Correct <see cref="T:LocalizeMessagesAndErrors.IDefaultLocalizer" /> service for the AuthP to use on localized code.
    /// </summary>
    public IDefaultLocalizer DefaultLocalizer => new StubDefaultLocalizer();
}