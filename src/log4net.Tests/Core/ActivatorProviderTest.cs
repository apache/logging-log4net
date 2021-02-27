using log4net.Core;
using log4net.Tests.Appender;
using NUnit.Framework;
using System;

#if NETSTANDARD1_3
using System.Reflection;
#endif
using System.Linq;

namespace log4net.Tests.Core
{
    [TestFixture]
    public class ActivatorProviderTest
    {
		/// <summary>
		/// Any initialization that happens before each test can
		/// go here
		/// </summary>
		[SetUp]
		public void SetUp()
		{
		}

		[TearDown]
		public void TearDown()
        {
			// this is set by default, but could have been changed in the tests.
			ActivatorProvider.Instance = new DefaultActivator();
		}

		/// <summary>
		/// Test whether an instance can be created using the default activator.
		/// </summary>
		[Test]
		public void TestCanCreateInstanceDefaultActivator()
		{
			var type = typeof(CountingAppender);
			var result = ActivatorProvider.CanCreateInstance(type);

			Assert.IsTrue(result);
		}


		/// <summary>
		/// Test retrieving an appender without dependencies using the default activator.
		/// </summary>
		[Test]
		public void TestCreateInstanceDefaultActivator()
		{
			var type = typeof(CountingAppender);
			var result = ActivatorProvider.CreateInstance(type);

			Assert.IsInstanceOf(type, result);
		}

		/// <summary>
		/// Test retrieving an appender with dependencies using the default activator.
		/// </summary>
		[Test]
		public void TestCreateInstanceWithDependenciesDefaultActivator_ThrowsException()
		{
			var type = typeof(CountingAppenderWithDependency);
			Assert.Throws<MissingMethodException>(() => ActivatorProvider.CreateInstance(type));
		}

		/// <summary>
		/// Test retrieving an appender with dependencies using the custom activator.
		/// </summary>
		[Test]
		public void TestCreateInstanceWithDependenciesCustomActivator()
		{
			ActivatorProvider.Instance = new CustomActivator();

			var type = typeof(CountingAppenderWithDependency);
			var result = ActivatorProvider.CreateInstance(type);

			Assert.IsInstanceOf(type, result);
		}

		private class CustomActivator : IActivator
        {
            public bool CanCreateInstance(Type type)
            {
				return true;
            }

            public object CreateInstance(Type type)
            {
				var ctor = type.GetConstructors().First();
				var parameters = ctor.GetParameters();
				var arguments = parameters.Select(p => CreateInstance(p.ParameterType)).ToArray();
				return ctor.Invoke(arguments);
            }
        }
    }
}
