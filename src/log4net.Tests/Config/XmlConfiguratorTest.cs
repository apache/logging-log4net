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

#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using log4net.Config;
using log4net.Repository;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Config;

/// <summary>
/// Tests for <see cref="XmlConfigurator"/> class.
/// </summary>
[TestFixture]
public class XmlConfiguratorTest
{
  /// <summary>
  /// Show config file path in error message
  /// </summary>
  [Test]
  public void ConfigureWithUnkownConfigFile()
  {
    Func<XmlElement?> getConfigSection = () => null;
    ILoggerRepository repository = LogManager.CreateRepository(Guid.NewGuid().ToString());
    SystemInfo.EntryAssemblyLocation = Guid.NewGuid().ToString();
    try
    {
      List<LogLog> configurationMessages = [];

      using LogLog.LogReceivedAdapter _ = new(configurationMessages);
      typeof(XmlConfigurator)
        .GetMethod("InternalConfigure", BindingFlags.NonPublic | BindingFlags.Static, [typeof(ILoggerRepository), getConfigSection.GetType()])!
        .Invoke(null, [repository, getConfigSection]);

      Assert.That(configurationMessages, Has.Count.EqualTo(1));
      Assert.That(configurationMessages[0].Message, Contains.Substring(SystemInfo.EntryAssemblyLocation + ".config"));
    }
    finally
    {
      SystemInfo.EntryAssemblyLocation = null!;
    }
  }
}
#endif