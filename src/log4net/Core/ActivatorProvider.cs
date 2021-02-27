using System;

namespace log4net.Core
{
    /// <summary>
    /// Provider class for <see cref="IActivator"/> instance and methods.
    /// </summary>
    /// <author>Travis Schettler</author>
    public sealed class ActivatorProvider
    {
        /// <summary>
        /// Sets the <see cref="IActivator"/> instance. Default is <see cref="DefaultActivator"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is only accessible privately as <see cref="ActivatorProvider"/> contains wrapper methods for the instance, 
        /// but is allowed to be set to a custom type activator.
        /// </para>
        /// </remarks>
        public static IActivator Instance { private get; set; } = new DefaultActivator();

        /// <summary>
        /// Wrapper method that calls the <see cref="IActivator.CanCreateInstance(Type)"/> method on the <see cref="Instance"/>.
        /// </summary>
		/// <param name="type">The type to inspect.</param>
		/// <returns><c>true</c> if the type is creatable, <c>false</c> otherwise.</returns>
        public static bool CanCreateInstance(Type type)
        {
           return Instance.CanCreateInstance(type);
        }

        /// <summary>
        /// Wrapper method that calls the <see cref="IActivator.CreateInstance(Type)"/> method on the <see cref="Instance"/>.
        /// </summary>
        /// <param name="type">The type of service object to get.</param>
        /// <returns>A service object of the specified type or null if there is no service object for the type.</returns>
        public static object CreateInstance(Type type)
        {
            return Instance.CreateInstance(type);
        }
    }
}
