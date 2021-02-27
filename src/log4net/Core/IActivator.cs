using System;

namespace log4net.Core
{
    /// <summary>
    /// Contains method to create types of objects, or obtain references to existing objects.
    /// </summary>
    /// <author>Travis Schettler</author>
    public interface IActivator
    {
        /// <summary>
		/// Test if a <see cref="Type"/> is constructible with <see cref="CreateInstance(Type)"/>.
        /// </summary>
		/// <param name="type">The type to inspect.</param>
		/// <returns><c>true</c> if the type is creatable, <c>false</c> otherwise.</returns>
        bool CanCreateInstance(Type type);

        /// <summary>
        /// Creates an instance of the specified type.
        /// </summary>
        /// <param name="type">The type of object to create.</param>
        /// <returns>A reference to the newly created object.</returns>
        object CreateInstance(Type type);
    }
}
