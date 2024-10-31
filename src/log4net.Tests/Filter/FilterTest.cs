/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.Xml;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Filter;
using log4net.Repository;
using NUnit.Framework;

namespace log4net.Tests.Filter;

[TestFixture]
public class FilterTest
{
  [Test]
  public void FilterConfigurationTest()
  {
    var log4NetConfig = new XmlDocument();
    log4NetConfig.LoadXml("""
      <log4net>
      <appender name="MemoryAppender" type="log4net.Appender.MemoryAppender, log4net">
          <filter type="log4net.Tests.Filter.MultiplePropertyFilter, log4net.Tests">
              <condition>
                  <key value="ABC" />
                  <stringToMatch value="123" />
              </condition>
              <condition>
                  <key value="DEF" />
                  <stringToMatch value="456" />
              </condition>
          </filter>
      </appender>
      <root>
          <level value="ALL" />
          <appender-ref ref="MemoryAppender" />
      </root>
      </log4net>
    """);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    XmlConfigurator.Configure(rep, log4NetConfig["log4net"]!);

    IAppender[] appenders = LogManager.GetRepository(rep.Name).GetAppenders();
    Assert.That(appenders, Has.Length.EqualTo(1));

    IAppender? appender = Array.Find(appenders, (IAppender a) => a.Name == "MemoryAppender");
    Assert.That(appender, Is.Not.Null);

    MultiplePropertyFilter? multiplePropertyFilter =
        ((AppenderSkeleton)appender!).FilterHead as MultiplePropertyFilter;
    Assert.That(multiplePropertyFilter, Is.Not.Null);
    MultiplePropertyFilter.Condition[] conditions = multiplePropertyFilter.GetConditions();
    Assert.That(conditions, Has.Length.EqualTo(2));
    Assert.That(conditions[0].Key, Is.EqualTo("ABC"));
    Assert.That(conditions[0].StringToMatch, Is.EqualTo("123"));
    Assert.That(conditions[1].Key, Is.EqualTo("DEF"));
    Assert.That(conditions[1].StringToMatch, Is.EqualTo("456"));
  }
}

internal sealed class MultiplePropertyFilter : FilterSkeleton
{
  private readonly List<Condition> _conditions = [];

  public override FilterDecision Decide(LoggingEvent loggingEvent) => FilterDecision.Accept;

  public Condition[] GetConditions() => _conditions.ToArray();

  public void AddCondition(Condition condition) => _conditions.Add(condition);

  internal sealed class Condition
  {
    public string? Key { get; set; }
    public string? StringToMatch { get; set; }
  }
}