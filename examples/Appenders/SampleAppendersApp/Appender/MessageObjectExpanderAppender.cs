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
using System.Reflection;

using log4net.Core;

namespace SampleAppendersApp.Appender;

/// <summary>
/// Forwarding Appender that introspects the <see cref="LoggingEvent.MessageObject"/>
/// and extracts all public properties and fields and stores them in the
/// <see cref="LoggingEvent.Properties"/>
/// </summary>
public sealed class MessageObjectExpanderAppender : log4net.Appender.ForwardingAppender
{
  /// <inheritdoc/>
  protected override void Append(LoggingEvent loggingEvent)
  {
    ArgumentNullException.ThrowIfNull(loggingEvent);
    object? messageObject = loggingEvent.MessageObject;

    if (messageObject is not null and not string)
    {
      Type messageType = messageObject.GetType();

      // Get all public instance properties
      if (ExpandProperties)
      {
        foreach (PropertyInfo propertyInfo in messageType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
          if (propertyInfo.CanRead)
          {
            loggingEvent.Properties[propertyInfo.Name] = propertyInfo.GetValue(messageObject, null);
          }
        }
      }

      // Get all public instance fields
      if (ExpandFields)
      {
        foreach (FieldInfo fieldInfo in messageType.GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
          loggingEvent.Properties[fieldInfo.Name] = fieldInfo.GetValue(messageObject);
        }
      }
    }

    // Delegate to base class which will forward
    base.Append(loggingEvent);
  }

  /// <inheritdoc/>
  public bool ExpandProperties { get; set; } = true;

  /// <inheritdoc/>
  public bool ExpandFields { get; set; } = true;
}