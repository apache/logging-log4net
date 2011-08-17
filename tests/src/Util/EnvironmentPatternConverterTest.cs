// .NET Compact Framework 1.0 has no support for Environment.GetEnvironmentVariable()
// .NET Framework version 1.0 / 1.1 do not have support for SetEnvironmentVariable which is used in these tests.
#if !NETCF && NET_2_0

using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace log4net.Tests.Util
{
    [TestFixture]
    public class EnvironmentPatternConverterTest
    {
        private const string ENVIRONMENT_VARIABLE_NAME = "LOG4NET_TEST_TEMP";
        const string SYSTEM_LEVEL_VALUE = "SystemLevelEnvironmentValue";
        const string USER_LEVEL_VALUE = "UserLevelEnvironmentValue";
        const string PROCESS_LEVEL_VALUE = "ProcessLevelEnvironmentValue";

        [Test]
        public void SystemLevelEnvironmentVariable()
        {
            EnvironmentPatternConverter converter = new EnvironmentPatternConverter();
            Environment.SetEnvironmentVariable(ENVIRONMENT_VARIABLE_NAME, SYSTEM_LEVEL_VALUE, EnvironmentVariableTarget.Machine);

            converter.Option = ENVIRONMENT_VARIABLE_NAME;

			StringWriter sw = new StringWriter();
			converter.Convert(sw, null);

            Assert.AreEqual(SYSTEM_LEVEL_VALUE, sw.ToString(), "System level environment variable not expended correctly.");

            Environment.SetEnvironmentVariable(ENVIRONMENT_VARIABLE_NAME, null, EnvironmentVariableTarget.Machine);
        }

        [Test]
        public void UserLevelEnvironmentVariable()
        {
            EnvironmentPatternConverter converter = new EnvironmentPatternConverter();
            Environment.SetEnvironmentVariable(ENVIRONMENT_VARIABLE_NAME, USER_LEVEL_VALUE, EnvironmentVariableTarget.User);

            converter.Option = ENVIRONMENT_VARIABLE_NAME;

            StringWriter sw = new StringWriter();
            converter.Convert(sw, null);

            Assert.AreEqual(USER_LEVEL_VALUE, sw.ToString(), "User level environment variable not expended correctly.");

            Environment.SetEnvironmentVariable(ENVIRONMENT_VARIABLE_NAME, null, EnvironmentVariableTarget.User);
        }

        [Test]
        public void ProcessLevelEnvironmentVariable()
        {
            EnvironmentPatternConverter converter = new EnvironmentPatternConverter();
            Environment.SetEnvironmentVariable(ENVIRONMENT_VARIABLE_NAME, PROCESS_LEVEL_VALUE);

            converter.Option = ENVIRONMENT_VARIABLE_NAME;

            StringWriter sw = new StringWriter();
            converter.Convert(sw, null);

            Assert.AreEqual(PROCESS_LEVEL_VALUE, sw.ToString(), "Process level environment variable not expended correctly.");

            Environment.SetEnvironmentVariable(ENVIRONMENT_VARIABLE_NAME, null);
        }

        private class EnvironmentPatternConverter
        {
            private object target = null;

            public EnvironmentPatternConverter()
            {
                target = Utils.CreateInstance("log4net.Util.PatternStringConverters.EnvironmentPatternConverter,log4net");
            }

            public string Option
            {
                get { return Utils.GetProperty(target, "Option") as string; }
                set { Utils.SetProperty(target, "Option", value); }
            }

            public void Convert(TextWriter writer, object state)
            {
                Utils.InvokeMethod(target, "Convert", writer, state);
            }
        }
    }
}

#endif
