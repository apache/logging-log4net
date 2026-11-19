#if NET462_OR_GREATER
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

using System.IO;
using System.Web;
using log4net.Core;
using log4net.Util;

namespace log4net.Layout.Pattern;

/// <summary>
/// Base of the <c>%aspnet-*</c> converters; writes <see cref="SystemInfo.NotAvailableText" /> without a current request.
/// </summary>
/// <remarks>
/// A request scoped converter also writes it for an event fixed with properties in another request.
/// </remarks>
/// <author>Ron Grabowski</author>
internal abstract class AspNetPatternLayoutConverter : PatternLayoutConverter
{
  private readonly bool _isRequestScoped;

  /// <summary>
  /// Registers the request capture for a converter that shows request data.
  /// </summary>
  /// <param name="isRequestScoped">Whether the converter shows data of the current request, not of the application.</param>
  protected AspNetPatternLayoutConverter(bool isRequestScoped)
  {
    _isRequestScoped = isRequestScoped;
    if (isRequestScoped)
    {
      AspNetRequestCapture.Register();
    }
  }

  protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
  {
    // Another request's data would be wrong, not missing.
    if (HttpContext.Current is HttpContext httpContext
      && (!_isRequestScoped || AspNetRequestCapture.WasLoggedIn(loggingEvent, httpContext)))
    {
      Convert(writer, loggingEvent, httpContext);
    }
    else
    {
      writer.Write(SystemInfo.NotAvailableText);
    }
  }

  /// <summary>
  /// Derived pattern converters must override this method in order to
  /// convert conversion specifiers in the correct way.
  /// </summary>
  /// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
  /// <param name="loggingEvent">The <see cref="LoggingEvent" /> on which the pattern converter should be executed.</param>
  /// <param name="httpContext">The <see cref="HttpContext" /> under which the ASP.Net request is running.</param>
  protected abstract void Convert(TextWriter writer, LoggingEvent loggingEvent, HttpContext httpContext);
}
#endif // NET462_OR_GREATER