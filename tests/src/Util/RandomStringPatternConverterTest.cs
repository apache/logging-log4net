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

using System.IO;

using NUnit.Framework;

namespace log4net.Tests.Util
{
	/// <summary>
	/// Used for internal unit testing the <see cref="RandomStringPatternConverter"/> class.
	/// </summary>
	/// <remarks>
	/// Used for internal unit testing the <see cref="RandomStringPatternConverter"/> class.
	/// </remarks>
	[TestFixture]
	public class RandomStringPatternConverterTest
	{
		[Test]
		public void TestConvert()
		{
			RandomStringPatternConverter converter = new RandomStringPatternConverter();

			// Check default string length
			StringWriter sw = new StringWriter();
			converter.Convert(sw, null);

			Assert.AreEqual(4, sw.ToString().Length, "Default string length should be 4");

			// Set string length to 7
			converter.Option = "7";
			converter.ActivateOptions();

			sw = new StringWriter();
			converter.Convert(sw, null);

			string string1 = sw.ToString();
			Assert.AreEqual(7, string1.Length, "string length should be 7");

			// Check for duplicate result
			sw = new StringWriter();
			converter.Convert(sw, null);

			string string2 = sw.ToString();
			Assert.IsTrue(string1 != string2, "strings should be different");
		}

		private class RandomStringPatternConverter
		{
			private object target = null;

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