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

#if NET462_OR_GREATER

using System.Runtime.CompilerServices;
using System.Web;
using log4net.Core;

namespace log4net.Layout.Pattern;

/// <summary>
/// A global property that a fix with <see cref="FixFlags.Properties"/> turns into a token for the logging request.
/// </summary>
/// <remarks>
/// The token does not reference the request and the table holds it weakly, so no ended request is kept alive.
/// </remarks>
internal sealed class AspNetRequestCapture : IFixingRequired
{
  /// <summary>
  /// The global property holding the capture, and in a fixed event the token.
  /// </summary>
  internal const string Key = "log4net:AspNetRequest";

  private static readonly AspNetRequestCapture _instance = new();

  private static readonly ConditionalWeakTable<HttpContext, RequestToken> _tokens = new();

  private AspNetRequestCapture()
  { }

  /// <summary>
  /// Registers the capture unless it is registered already, so that a registration the application
  /// cleared comes back with the next request converter.
  /// </summary>
  internal static void Register()
  {
    // Each set copies the global dictionary, so set only when needed.
    if (!ReferenceEquals(GlobalContext.Properties[Key], _instance))
    {
      GlobalContext.Properties[Key] = _instance;
    }
  }

  /// <summary>
  /// Whether <paramref name="loggingEvent"/> was logged in the request of <paramref name="httpContext"/>; an event not
  /// fixed with properties counts as logged there, being rendered in the call that logged it.
  /// </summary>
  internal static bool WasLoggedIn(LoggingEvent loggingEvent, HttpContext httpContext)
    => (loggingEvent.Fix & FixFlags.Properties) == 0
      || (loggingEvent.LookupProperty(Key) is RequestToken token
        && _tokens.TryGetValue(httpContext, out RequestToken? current) && ReferenceEquals(token, current));

  /// <inheritdoc/>
  object? IFixingRequired.GetFixedObject()
    => HttpContext.Current is HttpContext httpContext ? _tokens.GetValue(httpContext, _ => new RequestToken()) : null;

  /// <summary>
  /// Prints nothing, so property dumps show the key with an empty value.
  /// </summary>
  public override string ToString() => string.Empty;

  /// <summary>
  /// Stands for one request; not serializable, so a serialized event drops it.
  /// </summary>
  private sealed class RequestToken
  {
    /// <summary>
    /// Prints nothing, so property dumps show the key with an empty value.
    /// </summary>
    public override string ToString() => string.Empty;
  }
}

#endif // NET462_OR_GREATER
