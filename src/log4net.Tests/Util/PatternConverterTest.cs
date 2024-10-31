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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using log4net.Config;
using log4net.Core;
using log4net.Layout.Pattern;
using log4net.Repository;
using log4net.Tests.Appender;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Util;

[TestFixture]
public class PatternConverterTest
{
  [Test]
  public void PatternLayoutConverterProperties()
  {
    XmlDocument log4NetConfig = new();
    log4NetConfig.LoadXml("""
      <log4net>
        <appender name="StringAppender" type="log4net.Tests.Appender.StringAppender, log4net.Tests">
          <layout type="log4net.Layout.PatternLayout">
              <converter>
                  <name value="propertyKeyCount" />
                  <type value="log4net.Tests.Util.PropertyKeyCountPatternLayoutConverter, log4net.Tests" />
                  <property>
                      <key value="one-plus-one" />
                      <value value="2" />
                  </property>
                  <property>
                     <key value="two-plus-two" />
                     <value value="4" />
                  </property> 
              </converter>
              <conversionPattern value="%propertyKeyCount" />
          </layout>
        </appender>
        <root>
          <level value="ALL" />                  
          <appender-ref ref="StringAppender" />
        </root>  
      </log4net>
    """);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    XmlConfigurator.Configure(rep, log4NetConfig["log4net"]!);

    ILog log = LogManager.GetLogger(rep.Name, "PatternLayoutConverterProperties");
    log.Debug("Message");

    PropertyKeyCountPatternLayoutConverter? converter =
        PropertyKeyCountPatternLayoutConverter.MostRecentInstance;
    Assert.That(converter, Is.Not.Null);
    Assert.That(converter.Properties, Is.Not.Null);
    Assert.That(converter.Properties, Has.Count.EqualTo(2));
    Assert.That(converter.Properties["two-plus-two"], Is.EqualTo("4"));

    StringAppender appender =
        (StringAppender)LogManager.GetRepository(rep.Name).GetAppenders()[0];
    Assert.That(appender.GetString(), Is.EqualTo("2"));
  }

  [Test]
  public void PatternConverterProperties()
  {
    XmlDocument log4NetConfig = new();
    log4NetConfig.LoadXml("""
      <log4net>
        <appender name="PatternStringAppender" type="log4net.Tests.Util.PatternStringAppender, log4net.Tests">
          <layout type="log4net.Layout.SimpleLayout" />
          <setting>
              <converter>
                  <name value="propertyKeyCount" />
                  <type value="log4net.Tests.Util.PropertyKeyCountPatternConverter, log4net.Tests" />
                  <property>
                      <key value="one-plus-one" />
                      <value value="2" />
                  </property>
                  <property>
                     <key value="two-plus-two" />
                     <value value="4" />
                  </property> 
              </converter>
              <conversionPattern value="%propertyKeyCount" />
          </setting>
        </appender>
        <root>
          <level value="ALL" />                  
          <appender-ref ref="PatternStringAppender" />
        </root>  
      </log4net>
    """);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    XmlConfigurator.Configure(rep, log4NetConfig["log4net"]!);

    ILog log = LogManager.GetLogger(rep.Name, "PatternConverterProperties");
    log.Debug("Message");

    PropertyKeyCountPatternConverter? converter =
        PropertyKeyCountPatternConverter.MostRecentInstance;
    Assert.That(converter, Is.Not.Null);
    Assert.That(converter.Properties, Is.Not.Null);
    Assert.That(converter.Properties, Has.Count.EqualTo(2));
    Assert.That(converter.Properties["two-plus-two"], Is.EqualTo("4"));

    PatternStringAppender appender =
        (PatternStringAppender)LogManager.GetRepository(rep.Name).GetAppenders()[0];
    Assert.That(appender.Setting, Is.Not.Null);
    Assert.That(appender.Setting.Format(), Is.EqualTo("2"));
  }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class PropertyKeyCountPatternLayoutConverter : PatternLayoutConverter
{
  public PropertyKeyCountPatternLayoutConverter() => MostRecentInstance = this;

  [SuppressMessage("Design", "CA1062:Validate arguments of public methods")]
  protected override void Convert(TextWriter writer, LoggingEvent loggingEvent) 
    => writer.Write(Properties!.GetKeys().Length);

  public static PropertyKeyCountPatternLayoutConverter? MostRecentInstance { get; private set; }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class PropertyKeyCountPatternConverter : PatternConverter
{
  public PropertyKeyCountPatternConverter() => MostRecentInstance = this;

  [SuppressMessage("Design", "CA1062:Validate arguments of public methods")]
  public override void Convert(TextWriter writer, object? state)
    => writer.Write(Properties!.GetKeys().Length);

  public static PropertyKeyCountPatternConverter? MostRecentInstance { get; private set; }
}

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class PatternStringAppender : StringAppender
{
  public PatternStringAppender() => MostRecentInstance = this;

  // ReSharper disable once UnusedAutoPropertyAccessor.Global
  public PatternString? Setting { get; set; }

  // ReSharper disable once UnusedAutoPropertyAccessor.Global
  public static PatternStringAppender? MostRecentInstance { get; private set; }
}
