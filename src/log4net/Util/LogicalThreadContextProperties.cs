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
using System.Security;
#if NET462_OR_GREATER
using CallContext = System.Runtime.Remoting.Messaging.CallContext;
#endif
#if NETSTANDARD2_0_OR_GREATER
using CallContext = System.Threading.AsyncLocal<log4net.Util.PropertiesDictionary>;
#endif

namespace log4net.Util
{
  /// <summary>
  /// Implementation of Properties collection for the <see cref="log4net.LogicalThreadContext"/>
  /// </summary>
  /// <remarks>
  /// <para>
  /// Class implements a collection of properties that is specific to each thread.
  /// The class is not synchronized as each thread has its own <see cref="PropertiesDictionary"/>.
  /// </para>
  /// <para>
  /// This class stores its properties in a slot on the <see cref="CallContext"/> named
  /// <c>log4net.Util.LogicalThreadContextProperties</c>.
  /// </para>
  /// <para>
  /// For .NET Standard 1.3 this class uses
  /// System.Threading.AsyncLocal rather than <see cref="CallContext"/>.
  /// </para>
  /// <para>
  /// The <see cref="CallContext"/> requires a link time 
  /// <see cref="System.Security.Permissions.SecurityPermission"/> for the
  /// <see cref="System.Security.Permissions.SecurityPermissionFlag.Infrastructure"/>.
  /// If the calling code does not have this permission then this context will be disabled.
  /// It will not store any property values set on it.
  /// </para>
  /// </remarks>
  /// <author>Nicko Cadell</author>
  public sealed class LogicalThreadContextProperties : ContextPropertiesBase
  {
#if NETSTANDARD2_0_OR_GREATER
    private static readonly CallContext AsyncLocalDictionary = new CallContext();
#else
    private const string c_SlotName = "log4net.Util.LogicalThreadContextProperties";
#endif

    /// <summary>
    /// Flag used to disable this context if we don't have permission to access the CallContext.
    /// </summary>
    private bool m_disabled;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <remarks>
    /// <para>
    /// Initializes a new instance of the <see cref="LogicalThreadContextProperties" /> class.
    /// </para>
    /// </remarks>
    internal LogicalThreadContextProperties()
    {
    }

    /// <inheritdoc/>
    public override object? this[string key]
    {
      get
      {
        // Don't create the dictionary if it does not already exist
        PropertiesDictionary? dictionary = GetProperties(false);
        return dictionary?[key];
      }
      set
      {
        // Force the dictionary to be created
        PropertiesDictionary props = GetProperties(true)!;
        // Reason for cloning the dictionary below: object instances set on the CallContext
        // need to be immutable to correctly flow through async/await
        var immutableProps = new PropertiesDictionary(props);
        immutableProps[key] = value;
        SetLogicalProperties(immutableProps);
      }
    }

    /// <summary>
    /// Remove a property
    /// </summary>
    /// <param name="key">the key for the entry to remove</param>
    /// <remarks>
    /// <para>
    /// Remove the value for the specified <paramref name="key"/> from the context.
    /// </para>
    /// </remarks>
    public void Remove(string key)
    {
      PropertiesDictionary? dictionary = GetProperties(false);
      if (dictionary is not null)
      {
        PropertiesDictionary immutableProps = new PropertiesDictionary(dictionary);
        immutableProps.Remove(key);
        SetLogicalProperties(immutableProps);
      }
    }

    /// <summary>
    /// Clear all the context properties
    /// </summary>
    /// <remarks>
    /// <para>
    /// Clear all the context properties
    /// </para>
    /// </remarks>
    public void Clear()
    {
      PropertiesDictionary? dictionary = GetProperties(false);
      if (dictionary is not null)
      {
        SetLogicalProperties(new PropertiesDictionary());
      }
    }

    /// <summary>
    /// Get the PropertiesDictionary stored in the LocalDataStoreSlot for this thread.
    /// </summary>
    /// <param name="create">create the dictionary if it does not exist, otherwise return null if it does not exist</param>
    /// <returns>the properties for this thread</returns>
    /// <remarks>
    /// <para>
    /// The collection returned is only to be used on the calling thread. If the
    /// caller needs to share the collection between different threads then the 
    /// caller must clone the collection before doings so.
    /// </para>
    /// </remarks>
    internal PropertiesDictionary? GetProperties(bool create)
    {
      if (!m_disabled)
      {
        try
        {
          PropertiesDictionary? properties = GetLogicalProperties();
          if (properties is null && create)
          {
            properties = new PropertiesDictionary();
            SetLogicalProperties(properties);
          }
          return properties;
        }
        catch (SecurityException secEx)
        {
          m_disabled = true;

          // Thrown if we don't have permission to read or write the CallContext
          LogLog.Warn(declaringType, "SecurityException while accessing CallContext. Disabling LogicalThreadContextProperties", secEx);
        }
      }

      // Only get here is we are disabled because of a security exception
      if (create)
      {
        return new PropertiesDictionary();
      }
      return null;
    }

    /// <summary>
    /// Gets the call context get data.
    /// </summary>
    /// <returns>The properties dictionary stored in the call context</returns>
    /// <remarks>
    /// The <see cref="CallContext"/> method GetData security link demand, therefore we must
    /// put the method call in a separate method that we can wrap in an exception handler.
    /// </remarks>
    [SecuritySafeCritical]
    private static PropertiesDictionary? GetLogicalProperties()
    {
#if NETSTANDARD2_0_OR_GREATER
      return AsyncLocalDictionary.Value;
#else
      return CallContext.LogicalGetData(c_SlotName) as PropertiesDictionary;
#endif
    }

    /// <summary>
    /// Sets the call context data.
    /// </summary>
    /// <param name="properties">The properties.</param>
    /// <remarks>
    /// The <see cref="CallContext"/> method SetData has a security link demand, therefore we must
    /// put the method call in a separate method that we can wrap in an exception handler.
    /// </remarks>
    [SecuritySafeCritical]
    private static void SetLogicalProperties(PropertiesDictionary properties)
    {
#if NETSTANDARD2_0_OR_GREATER
      AsyncLocalDictionary.Value = properties;
#else
      CallContext.LogicalSetData(c_SlotName, properties);
#endif
    }

    /// <summary>
    /// The fully qualified type of the LogicalThreadContextProperties class.
    /// </summary>
    /// <remarks>
    /// Used by the internal logger to record the Type of the
    /// log message.
    /// </remarks>
    private static readonly Type declaringType = typeof(LogicalThreadContextProperties);
  }
}