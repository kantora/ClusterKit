﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NewNodeTemplateRequest.cs" company="KlusterKite">
//   All rights reserved
// </copyright>
// <summary>
//   Request from
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace KlusterKite.NodeManager.Launcher.Messages
{
    using System;

    /// <summary>
    /// Request from newly started node
    /// </summary>
    public class NewNodeTemplateRequest
    {
        /// <summary>
        /// Gets or sets container type to start node
        /// </summary>
        public string ContainerType { get; set; }

        /// <summary>
        /// Gets or sets the framework runtime type name
        /// </summary>
        public string FrameworkRuntimeType { get; set; }

        /// <summary>
        /// Gets or sets the runtime name
        /// </summary>
        public string Runtime { get; set; }

        /// <summary>
        /// Gets or sets the unique node identification number.
        /// </summary>
        public Guid NodeUid { get; set; }
    }
}