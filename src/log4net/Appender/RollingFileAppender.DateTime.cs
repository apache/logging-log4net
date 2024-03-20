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

namespace log4net.Appender
{
  public partial class RollingFileAppender
  {
    /// <summary>
    /// This interface is used to supply Date/Time information to the <see cref="RollingFileAppender"/>.
    /// </summary>
    /// <remarks>
    /// This interface is used to supply Date/Time information to the <see cref="RollingFileAppender"/>.
    /// Used primarily to allow test classes to plug themselves in so they can
    /// supply test date/times.
    /// </remarks>
    public interface IDateTime
    {
      /// <summary>
      /// Gets the <i>current</i> time.
      /// </summary>
      /// <value>The <i>current</i> time.</value>
      /// <remarks>
      /// <para>
      /// Gets the <i>current</i> time.
      /// </para>
      /// </remarks>
      DateTime Now { get; }
    }

    /// <summary>
    /// Default implementation of <see cref="IDateTime"/> that returns the current time.
    /// </summary>
    private sealed class LocalDateTime : IDateTime
    {
      /// <summary>
      /// Gets the <b>current</b> time.
      /// </summary>
      /// <value>The <b>current</b> time.</value>
      /// <remarks>
      /// <para>
      /// Gets the <b>current</b> time.
      /// </para>
      /// </remarks>
      public DateTime Now => DateTime.Now;
    }

    /// <summary>
    /// Implementation of <see cref="IDateTime"/> that returns the current time as the coordinated universal time (UTC).
    /// </summary>
    private sealed class UniversalDateTime : IDateTime
    {
      /// <summary>
      /// Gets the <b>current</b> time.
      /// </summary>
      /// <value>The <b>current</b> time.</value>
      /// <remarks>
      /// <para>
      /// Gets the <b>current</b> time.
      /// </para>
      /// </remarks>
      public DateTime Now => DateTime.UtcNow;
    }
  }
}