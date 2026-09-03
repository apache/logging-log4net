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

// netstandard has no System.Web
#if NET462_OR_GREATER

using System;
using System.IO;
using System.Web;

using log4net.Config;
using log4net.Layout;
using log4net.Repository;
using log4net.Tests.Appender;
using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.Layout.Pattern;

/// <summary>
/// Tests that <c>%aspnet-request</c> survives content ASP.NET request validation rejects.
/// </summary>
[TestFixture]
public sealed class AspNetRequestPatternConverterTest
{
  private const string Payload = "<script>";

  /// <summary>
  /// Detaches the hand-built request from the thread again.
  /// </summary>
  [TearDown]
  public void TearDown() => HttpContext.Current = null;

  /// <summary>
  /// Guards the other tests: if request validation does not fire here, they pass vacuously.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void TheRequestUsedByTheseTestsDoesFailValidation()
  {
    HttpRequest request = CurrentRequest("q=%3Cscript%3E");

    Assert.Throws<HttpRequestValidationException>(() => _ = request.Params);
  }

  /// <summary>
  /// Reading a named field of a request that fails validation once threw inside the layout, and
  /// the appender discarded the whole event, letting a sender suppress the record of their own
  /// request.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void ARejectedNamedFieldKeepsItsContent()
  {
    CurrentRequest("q=%3Cscript%3E");

    string rendered = Render("%aspnet-request{q}|%message");

    Assert.That(rendered, Is.EqualTo(Payload + "|TestMessage"));
  }

  /// <summary>
  /// The same for the whole collection, which has no unvalidated counterpart and is rebuilt.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void ARejectedRequestStillWritesTheEvent()
  {
    CurrentRequest("q=%3Cscript%3E");

    string rendered = Render("%aspnet-request|%message");

    Assert.That(rendered, Does.EndWith("|TestMessage"));
  }

  /// <summary>
  /// A benign request is unchanged, so the paths above are not hiding a broken happy path.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void ABenignNamedFieldIsUnchanged()
  {
    CurrentRequest("name=value");

    string rendered = Render("%aspnet-request{name}|%message");

    Assert.That(rendered, Is.EqualTo("value|TestMessage"));
  }

  /// <summary>
  /// Without a request the converter writes the not-available marker rather than failing.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void NoHttpContextWritesTheNotAvailableMarker()
  {
    HttpContext.Current = null;

    string rendered = Render("%aspnet-request{q}|%message");

    Assert.That(rendered, Is.EqualTo(SystemInfo.NotAvailableText + "|TestMessage"));
  }

  /// <summary>
  /// Publishes a request carrying <paramref name="queryString"/> and opts it into validation.
  /// </summary>
  private static HttpRequest CurrentRequest(string queryString)
  {
    HttpRequest request = new("page.aspx", "http://localhost/page.aspx", queryString);
    HttpContext.Current = new(request, new HttpResponse(TextWriter.Null));
    // The runtime does this for a real request; the first Params read then validates.
    request.ValidateInput();
    return request;
  }

  /// <summary>
  /// Logs one event through <paramref name="pattern"/> and returns what the appender received.
  /// </summary>
  private static string Render(string pattern)
  {
    StringAppender appender = new() { Layout = new PatternLayout(pattern) };
    ILoggerRepository repository = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(repository, appender);
    LogManager.GetLogger(repository.Name, nameof(AspNetRequestPatternConverterTest))
      .Info("TestMessage");
    return appender.GetString();
  }
}

#endif // NET462_OR_GREATER
