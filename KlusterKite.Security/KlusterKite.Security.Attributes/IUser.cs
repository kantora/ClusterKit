﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IUser.cs" company="KlusterKite">
//   All rights reserved
// </copyright>
// <summary>
//   The general interface to represent user in the system
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace KlusterKite.Security.Attributes
{
    /// <summary>
    /// The general interface to represent user in the system
    /// </summary>
    public interface IUser
    {
        /// <summary>
        /// Gets the string representation of the user id (login or other representation)
        /// </summary>
        string UserId { get; }
    }
}
