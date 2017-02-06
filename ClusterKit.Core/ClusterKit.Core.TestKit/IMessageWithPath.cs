// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMessageWithPath.cs" company="ClusterKit">
//   All rights reserved
// </copyright>
// <summary>
//   Message with addressee path description
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterKit.Core.TestKit
{
    /// <summary>
    /// Message with receiver address description
    /// </summary>
    public interface IMessageWithPath
    {
        /// <summary>
        /// Gets or sets the receiver address path.
        /// </summary>
        string ReceiverPath { get; set; }

        /// <summary>
        /// Gets the receiver address path from actor system root.
        /// </summary>
        string ReceiverPathRooted { get; }
    }
}