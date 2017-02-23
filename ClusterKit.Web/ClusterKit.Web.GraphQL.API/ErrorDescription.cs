﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErrorDescription.cs" company="ClusterKit">
//   All rights reserved
// </copyright>
// <summary>
//   The error description
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterKit.Web.GraphQL.API
{
    using ClusterKit.Web.GraphQL.Client.Attributes;

    using JetBrains.Annotations;

    /// <summary>
    /// The error description
    /// </summary>
    [ApiDescription(Description = "The mutation error description")]
    public class ErrorDescription
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorDescription"/> class.
        /// </summary>
        [UsedImplicitly]
        public ErrorDescription()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorDescription"/> class.
        /// </summary>
        /// <param name="field">
        /// The field.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        public ErrorDescription(string field, string message)
        {
            this.Field = field;
            this.Message = message;
        }

        /// <summary>
        /// Gets or sets the related field name
        /// </summary>
        [UsedImplicitly]
        [DeclareField(Description = "The related field name")]
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        [UsedImplicitly]
        [DeclareField(Description = "The error message")]
        public string Message { get; set; }
    }
}