#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.IO;

using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Layout.Pattern;
using log4net.Repository;
using log4net.Tests.Appender;
using log4net.Util;

using NUnit.Framework;
using System.Globalization;

namespace log4net.Tests.Layout
{
	/// <summary>
	/// Used for internal unit testing the <see cref="PatternLayout"/> class.
	/// </summary>
	/// <remarks>
	/// Used for internal unit testing the <see cref="PatternLayout"/> class.
	/// </remarks>
	[TestFixture]
	public class PatternLayoutTest
	{
		private CultureInfo _currentCulture;
		private CultureInfo _currentUICulture;

		[SetUp]
		public void SetUp()
		{
			// set correct thread culture
			_currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
			_currentUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
		}


        [TearDown]
        public void TearDown() {
			Utils.RemovePropertyFromAllContexts();
			// restore previous culture
			System.Threading.Thread.CurrentThread.CurrentCulture = _currentCulture;
			System.Threading.Thread.CurrentThread.CurrentUICulture = _currentUICulture;
        }

        protected virtual PatternLayout NewPatternLayout() {
            return new PatternLayout();
        }

        protected virtual PatternLayout NewPatternLayout(string pattern) {
            return new PatternLayout(pattern);
        }

        [Test]
		public void TestThreadPropertiesPattern()
		{
			StringAppender stringAppender = new StringAppender();
            stringAppender.Layout = NewPatternLayout("%property{" + Utils.PROPERTY_KEY + "}");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no thread properties value set");
			stringAppender.Reset();

			ThreadContext.Properties[Utils.PROPERTY_KEY] = "val1";

			log1.Info("TestMessage");
			Assert.AreEqual("val1", stringAppender.GetString(), "Test thread properties value set");
			stringAppender.Reset();

			ThreadContext.Properties.Remove(Utils.PROPERTY_KEY);

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread properties value removed");
			stringAppender.Reset();
		}

        [Test]
        public void TestStackTracePattern()
        {
            StringAppender stringAppender = new StringAppender();
            stringAppender.Layout = NewPatternLayout("%stacktrace{2}");

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            BasicConfigurator.Configure(rep, stringAppender);

            ILog log1 = LogManager.GetLogger(rep.Name, "TestStackTracePattern");

            log1.Info("TestMessage");
            StringAssert.EndsWith("PatternLayoutTest.TestStackTracePattern", stringAppender.GetString(), "stack trace value set");
            stringAppender.Reset();
        }

		[Test]
		public void TestGlobalPropertiesPattern()
		{
			StringAppender stringAppender = new StringAppender();
            stringAppender.Layout = NewPatternLayout("%property{" + Utils.PROPERTY_KEY + "}");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestGlobalProperiesPattern");

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no global properties value set");
			stringAppender.Reset();

			GlobalContext.Properties[Utils.PROPERTY_KEY] = "val1";

			log1.Info("TestMessage");
			Assert.AreEqual("val1", stringAppender.GetString(), "Test global properties value set");
			stringAppender.Reset();

			GlobalContext.Properties.Remove(Utils.PROPERTY_KEY);

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test global properties value removed");
			stringAppender.Reset();
		}

		[Test]
		public void TestAddingCustomPattern()
		{
			StringAppender stringAppender = new StringAppender();
			PatternLayout layout = NewPatternLayout();

			layout.AddConverter("TestAddingCustomPattern", typeof(TestMessagePatternConverter));
			layout.ConversionPattern = "%TestAddingCustomPattern";
			layout.ActivateOptions();

			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestAddingCustomPattern");

			log1.Info("TestMessage");
			Assert.AreEqual("TestMessage", stringAppender.GetString(), "%TestAddingCustomPattern not registered");
			stringAppender.Reset();
		}

        [Test]
        public void NamedPatternConverterWithoutPrecisionShouldReturnFullName()
        {
            StringAppender stringAppender = new StringAppender();
            PatternLayout layout = NewPatternLayout();
            layout.AddConverter("message-as-name", typeof(MessageAsNamePatternConverter));
            layout.ConversionPattern = "%message-as-name";
            layout.ActivateOptions();
            stringAppender.Layout = layout;
            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            BasicConfigurator.Configure(rep, stringAppender);
            ILog log1 = LogManager.GetLogger(rep.Name, "TestAddingCustomPattern");

            log1.Info("NoDots");
            Assert.AreEqual("NoDots", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("One.Dot");
            Assert.AreEqual("One.Dot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("Tw.o.Dots");
            Assert.AreEqual("Tw.o.Dots", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("TrailingDot.");
            Assert.AreEqual("TrailingDot.", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info(".LeadingDot");
            Assert.AreEqual(".LeadingDot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            // empty string and other evil combinations as tests for of-by-one mistakes in index calculations
            log1.Info(string.Empty);
            Assert.AreEqual(string.Empty, stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info(".");
            Assert.AreEqual(".", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("x");
            Assert.AreEqual("x", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();
        }

        [Test]
        public void NamedPatternConverterWithPrecision1ShouldStripLeadingStuffIfPresent()
        {
            StringAppender stringAppender = new StringAppender();
            PatternLayout layout = NewPatternLayout();
            layout.AddConverter("message-as-name", typeof(MessageAsNamePatternConverter));
            layout.ConversionPattern = "%message-as-name{1}";
            layout.ActivateOptions();
            stringAppender.Layout = layout;
            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            BasicConfigurator.Configure(rep, stringAppender);
            ILog log1 = LogManager.GetLogger(rep.Name, "TestAddingCustomPattern");

            log1.Info("NoDots");
            Assert.AreEqual("NoDots", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("One.Dot");
            Assert.AreEqual("Dot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("Tw.o.Dots");
            Assert.AreEqual("Dots", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("TrailingDot.");
            Assert.AreEqual("TrailingDot.", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info(".LeadingDot");
            Assert.AreEqual("LeadingDot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            // empty string and other evil combinations as tests for of-by-one mistakes in index calculations
            log1.Info(string.Empty);
            Assert.AreEqual(string.Empty, stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("x");
            Assert.AreEqual("x", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info(".");
            Assert.AreEqual(".", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();
        }

        [Test]
        public void NamedPatternConverterWithPrecision2ShouldStripLessLeadingStuffIfPresent() {
            StringAppender stringAppender = new StringAppender();
            PatternLayout layout = NewPatternLayout();
            layout.AddConverter("message-as-name", typeof(MessageAsNamePatternConverter));
            layout.ConversionPattern = "%message-as-name{2}";
            layout.ActivateOptions();
            stringAppender.Layout = layout;
            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            BasicConfigurator.Configure(rep, stringAppender);
            ILog log1 = LogManager.GetLogger(rep.Name, "TestAddingCustomPattern");

            log1.Info("NoDots");
            Assert.AreEqual("NoDots", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("One.Dot");
            Assert.AreEqual("One.Dot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("Tw.o.Dots");
            Assert.AreEqual("o.Dots", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("TrailingDot.");
            Assert.AreEqual("TrailingDot.", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info(".LeadingDot");
            Assert.AreEqual("LeadingDot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            // empty string and other evil combinations as tests for of-by-one mistakes in index calculations
            log1.Info(string.Empty);
            Assert.AreEqual(string.Empty, stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("x");
            Assert.AreEqual("x", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info(".");
            Assert.AreEqual(".", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();
        }

        /// <summary>
		/// Converter to include event message
		/// </summary>
		private class TestMessagePatternConverter : PatternLayoutConverter
		{
			/// <summary>
			/// Convert the pattern to the rendered message
			/// </summary>
			/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
			/// <param name="loggingEvent">the event being logged</param>
			/// <returns>the relevant location information</returns>
			protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
			{
				loggingEvent.WriteRenderedMessage(writer);
			}
		}

		[Test]
		public void TestExceptionPattern()
		{
			StringAppender stringAppender = new StringAppender();
			PatternLayout layout = NewPatternLayout("%exception{stacktrace}");
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestExceptionPattern");

			Exception exception = new Exception("Oh no!");
			log1.Info("TestMessage", exception);

			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString());

			stringAppender.Reset();
		}

        private class MessageAsNamePatternConverter : NamedPatternConverter
        {
            protected override string GetFullyQualifiedName(LoggingEvent loggingEvent)
            {
                return loggingEvent.MessageObject.ToString();
            }
        }
    }
}