namespace log4net.Util
{
    using System;

    /// <summary>
    /// Static factory to create objects by specified type.
    /// </summary>
    public static class ObjectFactory
    {
#if NET20 && !NET_3_5 // Only for net2.0
        /// <summary>Encapsulates a method that has one parameter and returns a value of the type specified by the <paramref>TResult</paramref> parameter.</summary>
        /// <param name="arg">The parameter of the method that this delegate encapsulates.</param>
        /// <typeparam name="T">The type of the parameter of the method that this delegate encapsulates.</typeparam>
        /// <typeparam name="TResult">The type of the return value of the method that this delegate encapsulates.</typeparam>
        /// <returns>Return value of the method that this delegate encapsulates.</returns>
        public delegate TResult Func<T, TResult>(T arg);
#endif

        private static Func<Type, object> _creator = Activator.CreateInstance;

        /// <summary>
        /// Gets or sets a static delegate to create objects.
        /// </summary>
        public static Func<Type, object> Creator
        {
            get => _creator;
            set => _creator = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Creates an instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to cast created object.</typeparam>
        /// <param name="type">The type of object to create.</param>
        /// <param name="throwException"><see langword="true" /> if a <see cref="InvalidCastException" /> throws for invalid casting conversion to specified type.</param>
        /// <returns>A reference to the newly created object.</returns>
        public static T Create<T>(Type type, bool throwException = true)
            where T : class
        {
            var instance = Create(type) as T;
            if (throwException && instance == null)
            {
                throw new InvalidCastException($"Unable to cast type [{type.FullName}] to [{typeof(T).FullName}]");
            }

            return instance;
        }

        /// <summary>
        /// Creates an instance of the specified type.
        /// <remarks>The instance is created by that type's default constructor.</remarks>
        /// </summary>
        /// <param name="type">The type of object to create.</param>
        /// <returns>A reference to the newly created object.</returns>
        public static object Create(Type type)
        {
            return Creator(type);
        }
    }
}
