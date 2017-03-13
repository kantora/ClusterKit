﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestUserFactory.cs" company="ClusterKit">
//   All rights reserved
// </copyright>
// <summary>
//   Defines the TestUserFactory type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterKit.Data.Tests.Mock
{
    using System;
    using System.Data.Entity;
    using System.Linq.Expressions;

    using ClusterKit.Data.EF;

    /// <summary>
    /// The users data factory
    /// </summary>
    public class TestUserFactory : EntityDataFactorySync<TestDataContext, User, Guid>
    {
        /// <inheritdoc />
        public TestUserFactory(TestDataContext context)
            : base(context)
        {
        }

        /// <inheritdoc />
        public override Guid GetId(User obj) => obj.Uid;

        /// <inheritdoc />
        public override Expression<Func<User, bool>> GetIdValidationExpression(Guid id) => obj => obj.Uid == id;

        /// <inheritdoc />
        protected override DbSet<User> GetDbSet()
        {
            return this.Context.Users;
        }
    }
}
