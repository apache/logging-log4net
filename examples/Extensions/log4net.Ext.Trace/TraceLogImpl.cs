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
using System.Globalization;
using log4net.Core;
using log4net.Util;

namespace log4net.Ext.Trace
{
  /// <inheritdoc/>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
  public sealed class TraceLogImpl(ILogger logger) : LogImpl(logger), ITraceLog
  {
    /// <summary>
    /// The fully qualified name of this declaring type not the type of any subclass.
    /// </summary>
    private readonly static Type ThisDeclaringType = typeof(TraceLogImpl);

    /// <summary>
    /// The default value for the TRACE level
    /// </summary>
    private readonly static Level s_defaultLevelTrace = new(20000, "TRACE");

    /// <summary>
    /// The current value for the TRACE level
    /// </summary>
    private Level? levelTrace;

    /// <summary>
    /// Lookup the current value of the TRACE level
    /// </summary>
    protected override void ReloadLevels(Repository.ILoggerRepository repository)
    {
      base.ReloadLevels(repository);
      ArgumentNullException.ThrowIfNull(repository);
      levelTrace = repository.LevelMap.LookupWithDefault(s_defaultLevelTrace);
    }

    #region Implementation of ITraceLog

    /// <inheritdoc/>
    public void Trace(object message)
      => Logger.Log(ThisDeclaringType, levelTrace, message, null);

    /// <inheritdoc/>
    public void Trace(object message, Exception t)
      => Logger.Log(ThisDeclaringType, levelTrace, message, t);

    /// <inheritdoc/>
    public void TraceFormat(string format, params object[] args)
    {
      if (IsTraceEnabled)
        Logger.Log(ThisDeclaringType, levelTrace, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
    }

    /// <inheritdoc/>
    public void TraceFormat(IFormatProvider provider, string format, params object[] args)
    {
      if (IsTraceEnabled)
        Logger.Log(ThisDeclaringType, levelTrace, new SystemStringFormat(provider, format, args), null);
    }

    /// <inheritdoc/>
    public bool IsTraceEnabled => Logger.IsEnabledFor(levelTrace);

    #endregion Implementation of ITraceLog
  }
}