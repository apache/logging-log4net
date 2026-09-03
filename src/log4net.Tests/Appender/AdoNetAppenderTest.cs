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

  /// <summary>
  /// A secret need not sit under a password-like keyword: Extended Properties nests a whole
  /// connection string, and tokens have keywords of their own.
  /// </summary>
  [Test]
  [NonParallelizable]
  [TestCase("Extended Properties=\"Driver={SQL Server};Server=someserver;UID=someuser;PWD=H0rseBatteryStaple\"")]
  [TestCase("AccessToken=H0rseBatteryStaple")]
  [TestCase("SharedAccessSignature=H0rseBatteryStaple")]
  [TestCase("Password=H0rseBatteryStaple")]
  [TestCase("PWD=H0rseBatteryStaple")]
  public void FailedConnectionDoesNotReportASecret(string secretBearingKeyword)
  {
    const string secret = "H0rseBatteryStaple";
    List<LogLog> messages = [];
    try
    {
      Log4NetConnection.FailOnOpen = true;
      LogLog.ExecuteWithoutEmittingInternalMessages(() =>
      {
        using LogLog.LogReceivedAdapter _ = new(messages);
        AdoNetAppender adoNetAppender = new()
        {
          BufferSize = -1,
          ConnectionType = typeof(Log4NetConnection).AssemblyQualifiedName!,
          ConnectionString = $"data source=someserver;initial catalog=somedb;{secretBearingKeyword}",
          CommandText = "INSERT INTO Log ([Message]) VALUES (@message)"
        };
        adoNetAppender.ActivateOptions();
      });

      string reported = string.Join(Environment.NewLine, messages.ConvertAll(m => m.Message));

      Assert.That(reported, Does.Not.Contain(secret));
      Assert.That(reported, Does.Contain("Could not open database connection"));
      // The server is still named, which is what makes the message worth printing.
      Assert.That(reported, Does.Contain("someserver"));
    }
    finally
    {
      Log4NetConnection.FailOnOpen = false;
    }
  }

  /// <summary>
  /// Without CommandText the rendered Layout is executed as the SQL statement, which is open
  /// to SQL injection from logged content. Activation has to say so.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void ActivateOptionsWithoutCommandTextWarnsAboutSqlInjection()
  {
    List<LogLog> messages = [];
    LogLog.ExecuteWithoutEmittingInternalMessages(() =>
    {
      using LogLog.LogReceivedAdapter _ = new(messages);
      AdoNetAppender adoNetAppender = new()
      {
        BufferSize = -1,
        ConnectionType = typeof(Log4NetConnection).AssemblyQualifiedName!
      };
      adoNetAppender.ActivateOptions();
    });

    Assert.That(messages.ConvertAll(m => m.Message),
      Has.Some.Contains("open to SQL injection"));
  }

  /// <summary>
  /// Configuring CommandText is the supported way to use the appender and must not warn.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void ActivateOptionsWithCommandTextDoesNotWarn()
  {
    List<LogLog> messages = [];
    LogLog.ExecuteWithoutEmittingInternalMessages(() =>
    {
      using LogLog.LogReceivedAdapter _ = new(messages);
      AdoNetAppender adoNetAppender = new()
      {
        BufferSize = -1,
        ConnectionType = typeof(Log4NetConnection).AssemblyQualifiedName!,
        CommandText = "INSERT INTO Log ([Message]) VALUES (@message)"
      };
      adoNetAppender.ActivateOptions();
    });

    Assert.That(messages.ConvertAll(m => m.Message),
      Has.None.Contains("open to SQL injection"));
  }

  /// <summary>
  /// An event the database rejects must only lose itself. The other events of the flushed
  /// buffer have already been removed from it and cannot be retried later, so they have to
  /// be written even though they shared a transaction with the rejected event.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void RejectedEventDoesNotDiscardTheRestOfTheBuffer()
  {
    try
    {
      Log4NetCommand.ExceptionTrigger = "POISON";
      Log4NetCommand.ExecutedPayloads.Clear();

      XmlDocument log4NetConfig = new();
      log4NetConfig.LoadXml(
        """
        <log4net>
        <appender name="AdoNetAppender" type="log4net.Appender.AdoNetAppender">
          <bufferSize value="3" />
          <useTransactions value="true" />
          <connectionType value="log4net.Tests.Appender.AdoNet.Log4NetConnection" />
          <connectionString value="data source=[database server]" />
          <commandText value="INSERT INTO Log ([Message]) VALUES (@message)" />
          <parameter>
            <parameterName value="@message" />
            <dbType value="String" />
            <size value="4000" />
            <layout type="log4net.Layout.PatternLayout">
              <conversionPattern value="%message" />
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
      ILog log = LogManager.GetLogger(rep.Name, "RejectedEventDoesNotDiscardTheRestOfTheBuffer");

      // The appender reports the rejected event through its ErrorHandler; that is expected
      // here and should not clutter the test output.
      LogLog.ExecuteWithoutEmittingInternalMessages(() =>
      {
        log.Debug("before");
        log.Debug("a POISON message");
        log.Debug("after one");
        // The fourth event overflows the buffer of 3 and flushes all four events.
        log.Debug("after two");
      });

      Assert.That(Log4NetCommand.ExecutedPayloads, Has.Member("before"));
      Assert.That(Log4NetCommand.ExecutedPayloads, Has.Member("after one"));
      Assert.That(Log4NetCommand.ExecutedPayloads, Has.Member("after two"));
      Assert.That(Log4NetCommand.ExecutedPayloads, Has.No.Member("a POISON message"));
    }
    finally
    {
      Log4NetCommand.ExceptionTrigger = null;
      Log4NetCommand.ExecutedPayloads.Clear();
    }
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

  /// <summary>
  /// Retrying one by one exists to save the events around the one the database rejected. When
  /// every one of them fails, the batch is not the problem and each further round trip only
  /// learns that again.
  /// </summary>
  [Test]
  public void RetryingPerEventGivesUpAfterRepeatedFailures()
  {
    const int bufferSize = 19;
    // One inside the transaction, then five one by one before it gives up.
    const int expectedAttempts = 6;

    Log4NetCommand.AttemptCount = 0;
    Log4NetCommand.ExceptionTrigger = "Message";
    try
    {
      XmlDocument log4NetConfig = new();
      log4NetConfig.LoadXml(
        """
        <log4net>
        <appender name="AdoNetAppender" type="log4net.Appender.AdoNetAppender">
          <bufferSize value="19" />
          <useTransactions value="true" />
          <connectionType value="log4net.Tests.Appender.AdoNet.Log4NetConnection" />
          <connectionString value="data source=[database server]" />
          <commandText value="INSERT INTO Log ([Message]) VALUES (@message)" />
          <parameter>
            <parameterName value="@message" />
            <dbType value="String" />
            <size value="4000" />
            <layout type="log4net.Layout.PatternLayout">
              <conversionPattern value="%message" />
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
      ILog log = LogManager.GetLogger(rep.Name, nameof(RetryingPerEventGivesUpAfterRepeatedFailures));

      LogLog.ExecuteWithoutEmittingInternalMessages(() =>
      {
        // The event after the buffer is full flushes all twenty.
        for (int i = 0; i <= bufferSize; i++)
        {
          log.Debug("Message");
        }
      });

      Assert.That(Log4NetCommand.AttemptCount, Is.EqualTo(expectedAttempts));
    }
    finally
    {
      Log4NetCommand.ExceptionTrigger = null;
      Log4NetCommand.ExecutedPayloads.Clear();
    }
  }

  /// <summary>
  /// Giving up must not cost the events around a rejected one, which is the whole point of
  /// retrying: five failures spread out are not five in a row.
  /// </summary>
  [Test]
  public void RetryingPerEventKeepsGoingWhileFailuresAreSpreadOut()
  {
    const int bufferSize = 9;
    const int expectedWrites = 5;

    Log4NetCommand.AttemptCount = 0;
    Log4NetCommand.ExceptionTrigger = "POISON";
    try
    {
      XmlDocument log4NetConfig = new();
      log4NetConfig.LoadXml(
        """
        <log4net>
        <appender name="AdoNetAppender" type="log4net.Appender.AdoNetAppender">
          <bufferSize value="9" />
          <useTransactions value="true" />
          <connectionType value="log4net.Tests.Appender.AdoNet.Log4NetConnection" />
          <connectionString value="data source=[database server]" />
          <commandText value="INSERT INTO Log ([Message]) VALUES (@message)" />
          <parameter>
            <parameterName value="@message" />
            <dbType value="String" />
            <size value="4000" />
            <layout type="log4net.Layout.PatternLayout">
              <conversionPattern value="%message" />
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
      ILog log = LogManager.GetLogger(rep.Name, nameof(RetryingPerEventKeepsGoingWhileFailuresAreSpreadOut));

      LogLog.ExecuteWithoutEmittingInternalMessages(() =>
      {
        for (int i = 0; i <= bufferSize; i++)
        {
          log.Debug(i % 2 == 0 ? "POISON" : $"good {i}");
        }
      });

      Assert.That(Log4NetCommand.ExecutedPayloads, Has.Count.EqualTo(expectedWrites));
      Assert.That(Log4NetCommand.ExecutedPayloads, Has.Member("good 9"));
    }
    finally
    {
      Log4NetCommand.ExceptionTrigger = null;
      Log4NetCommand.ExecutedPayloads.Clear();
    }
  }
}
