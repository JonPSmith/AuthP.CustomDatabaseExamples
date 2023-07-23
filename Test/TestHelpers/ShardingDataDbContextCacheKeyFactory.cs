// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using CustomDatabase2.ShardingDataInDb.ShardingDb;

namespace Test.TestHelpers;

public class ShardingDataDbContextCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
        => context is ShardingDataDbContext dynamicContext
            ? (context.GetType(), dynamicContext.ShardingData, designTime)
            : (object)context.GetType();

    public object Create(DbContext context)
        => Create(context, false);
}