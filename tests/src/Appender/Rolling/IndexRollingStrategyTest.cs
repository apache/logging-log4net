using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using log4net.Appender.Rolling;
using System.IO;

namespace log4net.Tests.Appender.Rolling
{
    [TestFixture]
    public class IndexRollingStrategyTest
    {
        [Test]
        public void RollTest()
        {
            IndexRollingStrategy strategy = new IndexRollingStrategy();
            List<string> tmpFiles = new List<string>();
            try
            {
                // create a dummy temp file
                string filename = "logfile.log";
                // add the file
                tmpFiles.Add(filename);
                for (int i = 0; i <= 10; i++)
                {
                    Console.WriteLine("Now at rolling iteration {0}", i);
                    // roll the file once
                    strategy.Roll(filename);
                    // create filename again
                    using (File.Create(filename))
                    {
                        // we don't need the filestream anymore
                    }
                    tmpFiles.Add(filename + "." + i);
                    // test if there are rolled files for [0..i]
                    for (int j = 0; j < i; j++)
                    {
                        if (!File.Exists(filename + "." + j))
                        {
                            // fail
                            Assert.Fail("The file '{0}' was not rolled correctly at iteration i={1}; j={2}", filename, i, j);
                        }
                    }
                }

                Console.WriteLine("Now at rolling iteration {0}, this should not create another file", 11);
                // roll the file once
                strategy.Roll(filename);
                // test if there are rolled files for [0..i]
                for (int j = 0; j <= 10; j++)
                {
                    if (!File.Exists(filename + "." + j))
                    {
                        // fail
                        Assert.Fail("The file '{0}' was not rolled correctly at iteration j={2}", filename, j);
                    }
                }
                if (File.Exists(filename + ".11"))
                {
                    Assert.Fail("The file '{0}' should not have been rolled further iteration 10", filename);
                }

            }
            catch (Exception e)
            {
                Assert.Fail("Exception: {0}", e);
            }
            finally
            {
                // cleanup
                while (tmpFiles.Count > 0)
                {
                    if (File.Exists(tmpFiles[0]))
                    {
                        File.Delete(tmpFiles[0]);
                    }
                    tmpFiles.RemoveAt(0);
                }
            }
        }
    }
}
