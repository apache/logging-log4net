#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
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
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

using log4net.Tests.Appender;

using NUnit.Framework;

namespace log4net.Tests.Util
{
	/// <summary>
	/// Used for internal unit testing the <see cref="RandomStringPatternConverter"/> class.
	/// </summary>
	/// <remarks>
	/// Used for internal unit testing the <see cref="RandomStringPatternConverter"/> class.
	/// </remarks>
	[TestFixture] public class RandomStringPatternConverterTest
	{
		[Test] public void TestConvert()
		{
			RandomStringPatternConverter converter = new RandomStringPatternConverter();

			// Check default string length
			StringWriter sw = new StringWriter();
			converter.Convert(sw, null);

			Assertion.AssertEquals("Default string length should be 4", 4, sw.ToString().Length);

			// Set string length to 7
			converter.Option = "7";
			converter.ActivateOptions();

			sw = new StringWriter();
			converter.Convert(sw, null);

			string string1 = sw.ToString();
			Assertion.AssertEquals("string length should be 7", 7, string1.Length);

			// Check for duplicate result
			sw = new StringWriter();
			converter.Convert(sw, null);

			string string2 = sw.ToString();
			Assertion.Assert("strings should be different", string1 != string2);
		}

		class RandomStringPatternConverter
		{
			object target = null;

			public RandomStringPatternConverter()
			{
				target = Utils.CreateInstance("log4net.Util.PatternStringConverters.RandomStringPatternConverter,log4net");
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

			public void ActivateOptions()
			{
				Utils.InvokeMethod(target, "ActivateOptions");
			}
		}
	}
}
