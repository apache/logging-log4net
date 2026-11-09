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
using System.Collections.Specialized;
using System.Web;

namespace log4net.Util;

/// <summary>
/// Reads an ASP.NET request past request validation, without ever waiting for the client to send its body.
/// </summary>
internal static class HttpRequestExtensions
{
  extension(HttpRequest request)
  {
    /// <summary>
    /// Looks <paramref name="name"/> up in the order of the <see cref="UnvalidatedRequestValues"/> indexer,
    /// but only in a form the application has read already.
    /// </summary>
    /// <param name="name">The query, form, cookie or server variable name.</param>
    /// <returns>The first value found, or <see langword="null"/>.</returns>
    internal string? GetUnvalidatedValue(string name)
    {
      UnvalidatedRequestValues unvalidated = request.Unvalidated;
      return unvalidated.QueryString[name]
        ?? request.TryGetLoadedForm()?[name]
        ?? unvalidated.Cookies[name]?.Value
        ?? request.ServerVariables[name];
    }

    /// <summary>
    /// Rebuilds <see cref="HttpRequest.Params"/> from the unvalidated values.
    /// </summary>
    /// <returns>Query string, a loaded form, cookies and server variables.</returns>
    internal NameValueCollection GetUnvalidatedValues()
    {
      UnvalidatedRequestValues unvalidated = request.Unvalidated;
      NameValueCollection values = new();
      values.Add(unvalidated.QueryString);
      if (request.TryGetLoadedForm() is NameValueCollection form)
      {
        values.Add(form);
      }
      HttpCookieCollection cookies = unvalidated.Cookies;
      foreach (string name in cookies.AllKeys)
      {
        values.Add(name, cookies[name]?.Value);
      }
      values.Add(request.ServerVariables);
      return values;
    }

    /// <summary>
    /// The unvalidated form, or <see langword="null"/> while reading it would wait for the client.
    /// </summary>
    /// <returns>The form, or <see langword="null"/>.</returns>
    /// <remarks>
    /// A layout runs under the appender's lock, and the first read of the form loads the body from the
    /// client: one that withheld its body would stall every thread that logs.
    /// </remarks>
    internal NameValueCollection? TryGetLoadedForm()
      => request.ReadEntityBodyMode switch
      {
        ReadEntityBodyMode.Classic => request.Unvalidated.Form,
        ReadEntityBodyMode.Buffered => TryGetForm(request),
        // None: not read yet. Bufferless: the form is never filled, and reading it throws.
        _ => null,
      };
  }

  /// <summary>
  /// The form of a buffered read, or <see langword="null"/> while the application has not read the stream to its end.
  /// </summary>
  private static NameValueCollection? TryGetForm(HttpRequest request)
  {
    try
    {
      return request.Unvalidated.Form;
    }
    catch (Exception e) when (e is HttpException or InvalidOperationException)
    {
      // System.Web throws InvalidOperationException while the buffered stream is not read to its end.
      return null;
    }
  }
}

#endif // NET462_OR_GREATER
