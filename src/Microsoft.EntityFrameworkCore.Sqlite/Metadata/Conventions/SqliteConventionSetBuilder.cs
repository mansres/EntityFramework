// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions
{
    public class SqliteConventionSetBuilder : RelationalConventionSetBuilder
    {
        public SqliteConventionSetBuilder(
            [NotNull] IRelationalTypeMapper typeMapper,
            [NotNull] IRelationalAnnotationProvider annotationProvider,
            [CanBeNull] ICurrentDbContext currentContext,
            [CanBeNull] IDbSetFinder setFinder)
            : base(typeMapper, annotationProvider, currentContext, setFinder)
        {
        }

        public static ConventionSet Build()
            => new SqliteConventionSetBuilder(new SqliteTypeMapper(), new SqliteAnnotationProvider(), null, null)
                .AddConventions(new CoreConventionSetBuilder().CreateConventionSet());
    }
}
