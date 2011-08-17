using System;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Util
{
    [TestFixture]
    public class PatternStringTest
    {
        [Test]
        public void TestEnvironmentFolderPathPatternConverter()
        {
            string[] specialFolderNames = Enum.GetNames(typeof(Environment.SpecialFolder));

            foreach (string specialFolderName in specialFolderNames)
            {
                string pattern = "%envFolderPath{" + specialFolderName + "}";

                PatternString patternString = new PatternString(pattern);

                string evaluatedPattern = patternString.Format();

                Environment.SpecialFolder specialFolder = 
                    (Environment.SpecialFolder)Enum.Parse(typeof(Environment.SpecialFolder), specialFolderName);

                Assert.AreEqual(Environment.GetFolderPath(specialFolder), evaluatedPattern);
            }
        }
    }
}
