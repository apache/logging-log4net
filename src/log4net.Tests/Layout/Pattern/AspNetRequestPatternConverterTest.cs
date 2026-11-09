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
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Hosting;

using log4net.Config;
using log4net.Layout;
using log4net.Repository;
using log4net.Tests.Appender;
using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.Layout.Pattern;

/// <summary>
/// Tests that <c>%aspnet-request</c> survives content ASP.NET request validation rejects, and that it
/// does not wait for the client to send the request body.
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
  /// Guards the next tests: if reading the form does not ask the client for the body, they pass vacuously.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void ReadingTheFormLoadsTheEntityBody()
  {
    FormPost workerRequest = new();
    HttpContext.Current = new(workerRequest);

    try
    {
      _ = HttpContext.Current.Request.Unvalidated.Form;
    }
    catch (HttpException)
    {
      // The body never arrives.
    }

    Assert.That(workerRequest.WasAskedForTheBody, Is.True);
  }

  /// <summary>
  /// The layout runs under the appender's lock, so waiting there for a client to send its body would let
  /// that client hold up every thread that logs.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void TheConverterDoesNotLoadTheEntityBody()
  {
    FormPost workerRequest = new();
    HttpContext.Current = new(workerRequest);

    string rendered = Render("%aspnet-request{name}|%aspnet-request|%message");

    Assert.That(workerRequest.WasAskedForTheBody, Is.False);
    Assert.That(HttpContext.Current.Request.ReadEntityBodyMode, Is.EqualTo(ReadEntityBodyMode.None));
    Assert.That(rendered, Does.EndWith("|TestMessage"));
    Assert.That(rendered, Does.Not.Contain(SystemInfo.NotAvailableText));
  }

  /// <summary>
  /// Once the application has read the body, a form field is still reported.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void AFormFieldIsShownOnceTheBodyHasBeenRead()
  {
    HttpContext.Current = new(new FormPost("name=value"));
    _ = HttpContext.Current.Request.Form;

    string rendered = Render("%aspnet-request{name}|%message");

    Assert.That(rendered, Is.EqualTo("value|TestMessage"));
  }

  /// <summary>
  /// After a bufferless read the form throws when read; the rest of the request is still reported.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void ABufferlessReadStillReportsTheRestOfTheRequest()
  {
    HttpContext.Current = new(new FormPost("name=value"));
    _ = HttpContext.Current.Request.GetBufferlessInputStream();

    string rendered = Render("%aspnet-request{REQUEST_METHOD}|%message");

    Assert.That(rendered, Is.EqualTo("POST|TestMessage"));
  }

  /// <summary>
  /// A buffered read the application has not finished leaves the form out, without failing the conversion.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void AnUnfinishedBufferedReadLeavesTheFormOut()
  {
    // Nothing preloaded, so any read of the form would have to ask the client.
    FormPost workerRequest = new();
    HttpContext.Current = new(workerRequest);
    _ = HttpContext.Current.Request.GetBufferedInputStream();

    string rendered = Render("%aspnet-request{name}|%message");

    Assert.That(workerRequest.WasAskedForTheBody, Is.False);
    Assert.That(rendered, Is.EqualTo(SystemInfo.NullText + "|TestMessage"));
  }

  /// <summary>
  /// Once the application has read the buffered stream to its end, the form is shown.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void AFinishedBufferedReadShowsTheForm()
  {
    HttpContext.Current = new(new FormPost("name=value"));
    HttpContext.Current.Request.GetBufferedInputStream().CopyTo(Stream.Null);

    string rendered = Render("%aspnet-request{name}|%message");

    Assert.That(rendered, Is.EqualTo("value|TestMessage"));
  }

  /// <summary>
  /// The query string comes before the server variables, as in the indexer the lookup replaces.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void TheQueryStringComesBeforeTheServerVariables()
  {
    HttpContext.Current = new(new FormPost(query: "REQUEST_METHOD=fromquery"));

    string rendered = Render("%aspnet-request{REQUEST_METHOD}|%message");

    Assert.That(rendered, Is.EqualTo("fromquery|TestMessage"));
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
  /// A form post of which <paramref name="preloaded"/> arrived with the headers: all of the body, as IIS
  /// hands over a small one, or nothing, so that the body still has to be read from the client.
  /// </summary>
  private sealed class FormPost(string? preloaded = null, string query = "")
    : SimpleWorkerRequest("/", Path.GetTempPath(), "page.aspx", query, TextWriter.Null)
  {
    private readonly byte[]? _preloaded = preloaded is null ? null : Encoding.ASCII.GetBytes(preloaded);

    /// <summary>
    /// Whether ASP.NET tried to read the body from the client, where a real request blocks.
    /// </summary>
    public bool WasAskedForTheBody { get; private set; }

    /// <inheritdoc/>
    public override string GetHttpVerbName() => "POST";

    /// <inheritdoc/>
    public override string GetKnownRequestHeader(int index) => index switch
    {
      HeaderContentType => "application/x-www-form-urlencoded",
      HeaderContentLength => (_preloaded?.Length ?? 1000).ToString(CultureInfo.InvariantCulture),
      _ => base.GetKnownRequestHeader(index),
    };

    /// <inheritdoc/>
    public override byte[]? GetPreloadedEntityBody() => _preloaded;

    /// <inheritdoc/>
    public override bool IsEntireEntityBodyIsPreloaded() => _preloaded is not null;

    /// <inheritdoc/>
    public override int ReadEntityBody(byte[] buffer, int size)
    {
      WasAskedForTheBody = true;
      return 0;
    }
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
