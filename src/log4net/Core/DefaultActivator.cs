using System;
using System.Reflection;

namespace log4net.Core
{
	/// <summary>
	/// The default log4net object activator.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Uses <see cref="Activator.CreateInstance(Type)"/> to create instances from types.
	/// </para>
	/// </remarks>
	/// <author>Travis Schettler</author>
	internal sealed class DefaultActivator : IActivator
    {
		/// <summary>
		/// Test if a <see cref="Type"/> is constructible with <see cref="CreateInstance(Type)"/>.
		/// </summary>
		/// <param name="type">the type to inspect</param>
		/// <returns><c>true</c> if the type is creatable using a default constructor, <c>false</c> otherwise</returns>
		/// <remarks>
		/// <para>
		/// Original code refactored out of the IsTypeConstructible method in the <see cref="Repository.Hierarchy.XmlHierarchyConfigurator"/> class.
		/// </para>
		/// </remarks>
		public bool CanCreateInstance(Type type)
        {
#if NETSTANDARD1_3
			TypeInfo typeInfo = type.GetTypeInfo();
			if (typeInfo.IsClass && !typeInfo.IsAbstract)
#else
			if (type.IsClass && !type.IsAbstract)
#endif
			{
				ConstructorInfo defaultConstructor = type.GetConstructor(new Type[0]);
				if (defaultConstructor != null && !defaultConstructor.IsAbstract && !defaultConstructor.IsPrivate)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Creates an instance of the specified type using that type's default constructor.
		/// </summary>
		/// <param name="serviceType">The type of object to create.</param>
		/// <returns>A reference to the newly created object.</returns>
		public object CreateInstance(Type serviceType)
        {
			return Activator.CreateInstance(serviceType);
		}
	}
}
