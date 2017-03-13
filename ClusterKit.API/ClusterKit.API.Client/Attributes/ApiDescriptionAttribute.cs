﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApiDescriptionAttribute.cs" company="ClusterKit">
//   All rights reserved
// </copyright>
// <summary>
//   Describes type (class) to published api
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterKit.API.Client.Attributes
{
    using System;
    using System.Linq;

    /// <summary>
    /// Describes type (class) to published api
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.Enum)]
    public class ApiDescriptionAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the published property / method name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description to publish
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Converts string to camel case
        /// </summary>
        /// <param name="name">The property / method name</param>
        /// <returns>The name in camel case</returns>
        public static string ToCamelCase(string name)
        {
            name = name?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var array = name.ToCharArray();
            array[0] = array[0].ToString().ToLowerInvariant().ToCharArray().First();

            return new string(array);
        }
    }
}