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
using System.Data;
using System.Xml;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Tests.Appender.AdoNet;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Appender;

[TestFixture]
public class AdoNetAppenderTest
{
  [Test]
  public void NoBufferingTest()
  {
    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

    AdoNetAppender adoNetAppender = new()
    {
      BufferSize = -1,
      ConnectionType = typeof(Log4NetConnection).AssemblyQualifiedName!
    };
    adoNetAppender.ActivateOptions();

    BasicConfigurator.Configure(rep, adoNetAppender);

    ILog log = LogManager.GetLogger(rep.Name, "NoBufferingTest");
    log.Debug("Message");
    Assert.That(Log4NetCommand.MostRecentInstance, Is.Not.Null);
    Assert.That(Log4NetCommand.MostRecentInstance.ExecuteNonQueryCount, Is.EqualTo(1));
  }

  [Test]
  public void BufferingTest()
  {
    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

    const int bufferSize = 5;

    AdoNetAppender adoNetAppender = new()
    {
      BufferSize = bufferSize,
      ConnectionType = typeof(Log4NetConnection).AssemblyQualifiedName!
    };
    adoNetAppender.ActivateOptions();

    BasicConfigurator.Configure(rep, adoNetAppender);

    ILog log = LogManager.GetLogger(rep.Name, "BufferingTest");
    for (int i = 0; i < bufferSize; i++)
    {
      log.Debug("Message");
      Assert.That(Log4NetCommand.MostRecentInstance, Is.Null);
    }
    log.Debug("Message");
    Assert.That(Log4NetCommand.MostRecentInstance, Is.Not.Null);
    Assert.That(Log4NetCommand.MostRecentInstance.ExecuteNonQueryCount, Is.EqualTo(bufferSize + 1));
  }

  [Test]
  public void WebsiteExample()
  {
    XmlDocument log4NetConfig = new();
    log4NetConfig.LoadXml(
      """
      <log4net>
      <appender name="AdoNetAppender" type="log4net.Appender.AdoNetAppender">
          <bufferSize value="-1" />
          <connectionType value="log4net.Tests.Appender.AdoNet.Log4NetConnection" />
          <connectionString value="data source=[database server];initial catalog=[database name];integrated security=false;persist security info=True;User ID=[user];Password=[password]" />
          <commandText value="INSERT INTO Log ([Date],[Thread],[Level],[Logger],[Message],[Exception]) VALUES (@log_date, @thread, @log_level, @logger, @message, @exception)" />
          <parameter>
              <parameterName value="@log_date" />
              <dbType value="DateTime" />
              <layout type="log4net.Layout.RawTimeStampLayout" />
          </parameter>
          <parameter>
              <parameterName value="@thread" />
              <dbType value="String" />
              <size value="255" />
              <layout type="log4net.Layout.PatternLayout">
                  <conversionPattern value="%thread" />
              </layout>
          </parameter>
          <parameter>
              <parameterName value="@log_level" />
              <dbType value="String" />
              <size value="50" />
              <layout type="log4net.Layout.PatternLayout">
                  <conversionPattern value="%level" />
              </layout>
          </parameter>
          <parameter>
              <parameterName value="@logger" />
              <dbType value="String" />
              <size value="255" />
              <layout type="log4net.Layout.PatternLayout">
                  <conversionPattern value="%logger" />
              </layout>
          </parameter>
          <parameter>
              <parameterName value="@message" />
              <dbType value="String" />
              <size value="4000" />
              <layout type="log4net.Layout.PatternLayout">
                  <conversionPattern value="%message" />
              </layout>
          </parameter>
          <parameter>
              <parameterName value="@exception" />
              <dbType value="String" />
              <size value="2000" />
              <layout type="log4net.Layout.ExceptionLayout" />
          </parameter>
      </appender>
      <root>
          <level value="ALL" />
          <appender-ref ref="AdoNetAppender" />
        </root>  
      </log4net>
      """);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    XmlConfigurator.Configure(rep, log4NetConfig["log4net"]!);
    ILog log = LogManager.GetLogger(rep.Name, "WebsiteExample");
    log.Debug("Message");

    IDbCommand? command = Log4NetCommand.MostRecentInstance;
    Assert.That(command, Is.Not.Null);

    Assert.That(command.CommandText,
      Is.EqualTo("INSERT INTO Log ([Date],[Thread],[Level],[Logger],[Message],[Exception]) VALUES (@log_date, @thread, @log_level, @logger, @message, @exception)"));
    Assert.That(command.Parameters, Has.Count.EqualTo(6));

    IDbDataParameter param = (IDbDataParameter)command.Parameters["@message"];
    Assert.That(param.Value, Is.EqualTo("Message"));

    param = (IDbDataParameter)command.Parameters["@log_level"];
    Assert.That(param.Value, Is.EqualTo(Level.Debug.ToString()));

    param = (IDbDataParameter)command.Parameters["@logger"];
    Assert.That(param.Value, Is.EqualTo("WebsiteExample"));

    param = (IDbDataParameter)command.Parameters["@exception"];
    Assert.That((string?)param.Value, Is.Empty);
  }

  [Test]
  public void BufferingWebsiteExample()
  {
    XmlDocument log4NetConfig = new();
    log4NetConfig.LoadXml(
      """
      <log4net>
      <appender name="AdoNetAppender" type="log4net.Appender.AdoNetAppender">
          <bufferSize value="2" />
          <connectionType value="log4net.Tests.Appender.AdoNet.Log4NetConnection" />
          <connectionString value="data source=[database server];initial catalog=[database name];integrated security=false;persist security info=True;User ID=[user];Password=[password]" />
          <commandText value="INSERT INTO Log ([Date],[Thread],[Level],[Logger],[Message],[Exception]) VALUES (@log_date, @thread, @log_level, @logger, @message, @exception)" />
          <parameter>
              <parameterName value="@log_date" />
              <dbType value="DateTime" />
              <layout type="log4net.Layout.RawTimeStampLayout" />
          </parameter>
          <parameter>
              <parameterName value="@thread" />
              <dbType value="String" />
              <size value="255" />
              <layout type="log4net.Layout.PatternLayout">
                  <conversionPattern value="%thread" />
              </layout>
          </parameter>
          <parameter>
              <parameterName value="@log_level" />
              <dbType value="String" />
              <size value="50" />
              <layout type="log4net.Layout.PatternLayout">
                  <conversionPattern value="%level" />
              </layout>
          </parameter>
          <parameter>
              <parameterName value="@logger" />
              <dbType value="String" />
              <size value="255" />
              <layout type="log4net.Layout.PatternLayout">
                  <conversionPattern value="%logger" />
              </layout>
          </parameter>
          <parameter>
              <parameterName value="@message" />
              <dbType value="String" />
              <size value="4000" />
              <layout type="log4net.Layout.PatternLayout">
                  <conversionPattern value="%message" />
              </layout>
          </parameter>
          <parameter>
              <parameterName value="@exception" />
              <dbType value="String" />
              <size value="2000" />
              <layout type="log4net.Layout.ExceptionLayout" />
          </parameter>
      </appender>
      <root>
          <level value="ALL" />
          <appender-ref ref="AdoNetAppender" />
        </root>  
      </log4net>
      """);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    XmlConfigurator.Configure(rep, log4NetConfig["log4net"]!);
    ILog log = LogManager.GetLogger(rep.Name, "WebsiteExample");

    for (int i = 0; i < 3; i++)
    {
      log.Debug("Message");
    }

    IDbCommand? command = Log4NetCommand.MostRecentInstance;
    Assert.That(command, Is.Not.Null);

    Assert.That(command.CommandText,
     Is.EqualTo("INSERT INTO Log ([Date],[Thread],[Level],[Logger],[Message],[Exception]) VALUES (@log_date, @thread, @log_level, @logger, @message, @exception)"));

    Assert.That(command.Parameters, Has.Count.EqualTo(6));

    IDbDataParameter param = (IDbDataParameter)command.Parameters["@message"];
    Assert.That(param.Value, Is.EqualTo("Message"));

    param = (IDbDataParameter)command.Parameters["@log_level"];
    Assert.That(param.Value, Is.EqualTo(Level.Debug.ToString()));

    param = (IDbDataParameter)command.Parameters["@logger"];
    Assert.That(param.Value, Is.EqualTo("WebsiteExample"));

    param = (IDbDataParameter)command.Parameters["@exception"];
    Assert.That(param.Value, Is.Empty);
  }

  [Test]
  public void NullPropertyXmlConfig()
  {
    XmlDocument log4NetConfig = new();
    log4NetConfig.LoadXml(
      """
      <log4net>
      <appender name="AdoNetAppender" type="log4net.Appender.AdoNetAppender">
          <bufferSize value="-1" />
          <connectionType value="log4net.Tests.Appender.AdoNet.Log4NetConnection" />
          <connectionString value="data source=[database server];initial catalog=[database name];integrated security=false;persist security info=True;User ID=[user];Password=[password]" />
          <commandText value="INSERT INTO Log ([ProductId]) VALUES (@productId)" />
          <parameter>
              <parameterName value="@productId" />
              <dbType value="String" />
              <size value="50" />
              <layout type=" log4net.Layout.RawPropertyLayout">
                 <key value="ProductId" />
              </layout>
          </parameter>                    
      </appender>
      <root>
          <level value="ALL" />
          <appender-ref ref="AdoNetAppender" />
        </root>  
      </log4net>
      """);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    XmlConfigurator.Configure(rep, log4NetConfig["log4net"]!);
    ILog log = LogManager.GetLogger(rep.Name, "NullPropertyXmlConfig");

    log.Debug("Message");
    IDbCommand? command = Log4NetCommand.MostRecentInstance;
    Assert.That(command, Is.Not.Null);

    IDbDataParameter param = (IDbDataParameter)command.Parameters["@productId"];
    Assert.That(param.Value, Is.Not.EqualTo(SystemInfo.NullText));
    Assert.That(param.Value, Is.EqualTo(DBNull.Value));
  }

  [Test]
  public void NullPropertyProgrammaticConfig()
  {
    AdoNetAppenderParameter productIdParam = new()
    {
      ParameterName = "@productId",
      DbType = DbType.String,
      Size = 50
    };
    RawPropertyLayout rawPropertyLayout = new()
    {
      Key = "ProductId"
    };
    productIdParam.Layout = rawPropertyLayout;

    AdoNetAppender appender = new()
    {
      ConnectionType = typeof(Log4NetConnection).AssemblyQualifiedName!,
      BufferSize = -1,
      CommandText = "INSERT INTO Log ([productId]) VALUES (@productId)"
    };
    appender.AddParameter(productIdParam);
    appender.ActivateOptions();

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, appender);
    ILog log = LogManager.GetLogger(rep.Name, "NullPropertyProgmaticConfig");

    log.Debug("Message");
    IDbCommand? command = Log4NetCommand.MostRecentInstance;
    Assert.That(command, Is.Not.Null);

    IDbDataParameter param = (IDbDataParameter)command.Parameters["@productId"];
    Assert.That(param.Value, Is.Not.EqualTo(SystemInfo.NullText));
    Assert.That(param.Value, Is.EqualTo(DBNull.Value));
  }
}
