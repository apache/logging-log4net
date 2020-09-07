namespace log4net.Tests
{
    using System.IO;

    using NUnit.Framework;

    [SetUpFixture]
    public class NUnitTestRunnerInitializer
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
        }
    }
}
