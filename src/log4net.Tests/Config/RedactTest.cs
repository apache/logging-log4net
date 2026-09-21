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
using System.Collections.Generic;
using System.Xml;
using log4net.Config;
using log4net.Repository;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Config;

/// <summary>
/// Tests that the configurator keeps secrets out of its own messages.
/// </summary>
[TestFixture]
[NonParallelizable]
public sealed class RedactTest
{
  private const string Password = "Sup3rS3cretPw";
  private const string ConnectionString = "Server=db.example.invalid;User Id=sa;Password=" + Password;

  /// <summary>
  /// Internal debugging is the documented first troubleshooting step, and it used to echo every
  /// configured value, secrets included.
  /// </summary>
  [Test]
  public void SecretValuesAreNotEchoedByInternalDebugging()
  {
    string reported = Configure($"""
      <log4net>
        <appender name="SmtpAppender" type="log4net.Appender.SmtpAppender">
          <to value="dest@example.invalid" />
          <password value="{Password}" />
          <layout type="log4net.Layout.PatternLayout" />
        </appender>
        <root>
          <appender-ref ref="SmtpAppender" />
        </root>
      </log4net>
      """);

    Assert.That(reported, Does.Contain("Setting Property [Password]"), "internal debugging was off");
    Assert.That(reported, !Contains.Substring(Password).Using(StringComparison.Ordinal));
    // A value that is not a secret is still echoed, so the output stays useful.
    Assert.That(reported, Does.Contain("dest@example.invalid"));
  }

  /// <summary>A connection string keeps the server it names, so it can still be checked.</summary>
  [Test]
  public void AConnectionStringKeepsOnlyTheKeywordsThatNameTheServer()
  {
    string reported = Configure($"""
      <log4net>
        <appender name="AdoNetAppender" type="log4net.Appender.AdoNetAppender">
          <connectionString value="{ConnectionString}" />
          <layout type="log4net.Layout.PatternLayout" />
        </appender>
        <root>
          <appender-ref ref="AdoNetAppender" />
        </root>
      </log4net>
      """);

    Assert.That(reported, !Contains.Substring(Password).Using(StringComparison.Ordinal));
    Assert.That(reported, Does.Contain("db.example.invalid"));
  }

  /// <summary>
  /// The provider type and the lookup key are the first things read when configuration fails, so
  /// neither may be hidden by the connection string rule.
  /// </summary>
  [Test]
  public void AParameterOfTheConnectionFamilyThatIsNoSecretSurvives()
  {
    string reported = Configure("""
      <log4net>
        <appender name="AdoNetAppender" type="log4net.Appender.AdoNetAppender">
          <connectionType value="System.Data.SqlClient.SqlConnection, System.Data" />
          <connectionStringName value="myDatabase" />
          <layout type="log4net.Layout.PatternLayout" />
        </appender>
        <root>
          <appender-ref ref="AdoNetAppender" />
        </root>
      </log4net>
      """);

    Assert.That(reported, Does.Contain("System.Data.SqlClient.SqlConnection, System.Data"));
    Assert.That(reported, Does.Contain("myDatabase"));
  }

  /// <summary>Configures a repository from <paramref name="xml"/> and returns what log4net reported.</summary>
  private static string Configure(string xml)
  {
    List<LogLog> messages = [];
    bool internalDebugging = LogLog.InternalDebugging;
    try
    {
      LogLog.InternalDebugging = true;
      LogLog.ExecuteWithoutEmittingInternalMessages(() =>
      {
        using LogLog.LogReceivedAdapter _ = new(messages);
        XmlDocument document = new();
        document.LoadXml(xml);
        ILoggerRepository repository = LogManager.CreateRepository(Guid.NewGuid().ToString());
        XmlConfigurator.Configure(repository, (XmlElement)document.DocumentElement!);
      });
    }
    finally
    {
      LogLog.InternalDebugging = internalDebugging;
    }

    return string.Join(Environment.NewLine, messages.ConvertAll(message => message.Message));
  }
}
