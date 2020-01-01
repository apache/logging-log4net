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

using log4net.Appender;
using NUnit.Framework;

namespace log4net.Tests.Appender
{
	/// <summary>
	/// Used for internal unit testing the <see cref="AppenderCollection"/> class.
	/// </summary>
	/// <remarks>
	/// Used for internal unit testing the <see cref="AppenderCollection"/> class.
	/// </remarks>
	/// <author>Carlos Muñoz</author>
	[TestFixture]
	public class AppenderCollectionTest
	{
		/// <summary>
		/// Verifies that ToArray returns the elements of the <see cref="AppenderCollection"/>
		/// </summary>
		[Test]
		public void ToArrayTest()
		{
			AppenderCollection appenderCollection = new AppenderCollection();
			IAppender appender = new MemoryAppender();
			appenderCollection.Add(appender);

			IAppender[] appenderArray = appenderCollection.ToArray();

			Assert.AreEqual(1, appenderArray.Length);
			Assert.AreEqual(appender, appenderArray[0]);
		}

		[Test]
		public void ReadOnlyToArrayTest()
		{
			AppenderCollection appenderCollection = new AppenderCollection();
			IAppender appender = new MemoryAppender();
			appenderCollection.Add(appender);
			AppenderCollection readonlyAppenderCollection = AppenderCollection.ReadOnly(appenderCollection);

			IAppender[] appenderArray = readonlyAppenderCollection.ToArray();

			Assert.AreEqual(1, appenderArray.Length);
			Assert.AreEqual(appender, appenderArray[0]);
		}
	}
}