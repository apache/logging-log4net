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
using System.Configuration;
using System.Reflection;
using System.IO;
using System.Collections;

namespace log4net.Util;

/// <summary>
/// Utility class for system specific information.
/// </summary>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
/// <author>Alexey Solofnenko</author>
public static class SystemInfo
{
  private const string DefaultNullText = "(null)";
  private const string DefaultNotAvailableText = "NOT AVAILABLE";

  /// <summary>
  /// Initialize default values for private static fields.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Only static methods are exposed from this type.
  /// </para>
  /// </remarks>
  static SystemInfo()
  {
    string nullText = DefaultNullText;
    string notAvailableText = DefaultNotAvailableText;

    // Look for log4net.NullText in AppSettings
    string? nullTextAppSettingsKey = GetAppSetting("log4net.NullText");
    if (nullTextAppSettingsKey is not null && nullTextAppSettingsKey.Length > 0)
    {
      LogLog.Debug(_declaringType, $"Initializing NullText value to [{nullTextAppSettingsKey}].");
      nullText = nullTextAppSettingsKey;
    }

    // Look for log4net.NotAvailableText in AppSettings
    string? notAvailableTextAppSettingsKey = GetAppSetting("log4net.NotAvailableText");
    if (notAvailableTextAppSettingsKey is not null && notAvailableTextAppSettingsKey.Length > 0)
    {
      LogLog.Debug(_declaringType, $"Initializing NotAvailableText value to [{notAvailableTextAppSettingsKey}].");
      notAvailableText = notAvailableTextAppSettingsKey;
    }
    NotAvailableText = notAvailableText;
    NullText = nullText;
  }

  /// <summary>
  /// Gets the system dependent line terminator.
  /// </summary>
  public static string NewLine => Environment.NewLine;

  /// <summary>
  /// Gets the base directory for this <see cref="AppDomain"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The value returned may be either a local file path or a URI.
  /// </para>
  /// </remarks>
  public static string ApplicationBaseDirectory => AppDomain.CurrentDomain.BaseDirectory;

  /// <summary>
  /// Gets the path to the configuration file for the current <see cref="AppDomain"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The value returned may be either a local file path or a URI.
  /// </para>
  /// </remarks>
  public static string ConfigurationFileLocation
  {
    get
    {
#if NET462_OR_GREATER
      return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
#else
      return EntryAssemblyLocation + ".config";
#endif
    }
  }

  private static string? _entryAssemblyLocation;

  /// <summary>
  /// Gets the path to the file that first executed in the current <see cref="AppDomain"/>.
  /// </summary>
  public static string EntryAssemblyLocation
  {
    get
    {
      if (_entryAssemblyLocation is not null)
      {
        return _entryAssemblyLocation;
      }
      return _entryAssemblyLocation = Assembly.GetEntryAssembly()?.Location
        ?? throw new InvalidOperationException($"Unable to determine EntryAssembly location: EntryAssembly is null. Try explicitly setting {nameof(SystemInfo)}.{nameof(EntryAssemblyLocation)}");
    }
    set => _entryAssemblyLocation = value;
  }

  /// <summary>
  /// Gets the ID of the current thread.
  /// </summary>
  public static int CurrentThreadId => Environment.CurrentManagedThreadId;

  /// <summary>
  /// Gets the host name or machine name for the current machine.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The host name (<see cref="System.Net.Dns.GetHostName"/>) or
  /// the machine name (<see cref="Environment.MachineName"/>) for
  /// the current machine, or if neither of these are available
  /// then <c>NOT AVAILABLE</c> is returned.
  /// </para>
  /// </remarks>
  public static string HostName
  {
    get
    {
      if (_sHostName is null)
      {
        // Get the DNS host name of the current machine
        try
        {
          // Lookup the host name
          _sHostName = System.Net.Dns.GetHostName();
        }
        catch (System.Net.Sockets.SocketException)
        {
          LogLog.Debug(_declaringType, "Socket exception occurred while getting the dns hostname. Error Ignored.");
        }
        catch (System.Security.SecurityException)
        {
          // We may get a security exception looking up the hostname
          // You must have Unrestricted DnsPermission to access resource
          LogLog.Debug(_declaringType, "Security exception occurred while getting the dns hostname. Error Ignored.");
        }
        catch (Exception e) when (!e.IsFatal())
        {
          LogLog.Debug(_declaringType, "Some other exception occurred while getting the dns hostname. Error Ignored.", e);
        }

        // Get the NETBIOS machine name of the current machine
        if (string.IsNullOrEmpty(_sHostName))
        {
          try
          {
            _sHostName = Environment.MachineName;
          }
          catch (InvalidOperationException)
          {
          }
          catch (System.Security.SecurityException)
          {
            // We may get a security exception looking up the machine name
            // You must have Unrestricted EnvironmentPermission to access resource
          }
        }

        // Couldn't find a value
        if (string.IsNullOrEmpty(_sHostName))
        {
          _sHostName = NotAvailableText;
          LogLog.Debug(_declaringType, "Could not determine the hostname. Error Ignored. Empty host name will be used");
        }
      }
      return _sHostName!;
    }
  }

  /// <summary>
  /// Gets this application's friendly name.
  /// </summary>
  /// <remarks>
  /// <para>
  /// If available the name of the application is retrieved from
  /// the <c>AppDomain</c> using <c>AppDomain.CurrentDomain.FriendlyName</c>.
  /// </para>
  /// <para>
  /// Otherwise the file name of the entry assembly is used.
  /// </para>
  /// </remarks>
  public static string ApplicationFriendlyName
  {
    get
    {
      if (_sAppFriendlyName is null)
      {
        try
        {
          _sAppFriendlyName = AppDomain.CurrentDomain.FriendlyName;
        }
        catch (System.Security.SecurityException)
        {
          // This security exception will occur if the caller does not have 
          // some undefined set of SecurityPermission flags.
          LogLog.Debug(_declaringType, "Security exception while trying to get current domain friendly name. Error Ignored.");
        }

        if (string.IsNullOrEmpty(_sAppFriendlyName))
        {
          try
          {
            string assemblyLocation = EntryAssemblyLocation;
            _sAppFriendlyName = Path.GetFileName(assemblyLocation);
          }
          catch (System.Security.SecurityException)
          {
            // Caller needs path discovery permission
          }
        }

        if (string.IsNullOrEmpty(_sAppFriendlyName))
        {
          _sAppFriendlyName = NotAvailableText;
        }
      }
      return _sAppFriendlyName!;
    }
  }

  /// <summary>
  /// Get the UTC start time for the current process.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the UTC time at which the log4net library was loaded into the
  /// AppDomain. Due to reports of a hang in the call to <c>System.Diagnostics.Process.StartTime</c>
  /// this is not the start time for the current process.
  /// </para>
  /// <para>
  /// The log4net library should be loaded by an application early during its
  /// startup, therefore this start time should be a good approximation for
  /// the actual start time.
  /// </para>
  /// <para>
  /// Note that AppDomains may be loaded and unloaded within the
  /// same process without the process terminating, however this start time
  /// will be set per AppDomain.
  /// </para>
  /// </remarks>
  public static DateTime ProcessStartTimeUtc { get; } = DateTime.UtcNow;

  /// <summary>
  /// Text to output when a <c>null</c> is encountered.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Use this value to indicate a <c>null</c> has been encountered while
  /// outputting a string representation of an item.
  /// </para>
  /// <para>
  /// The default value is <c>(null)</c>. This value can be overridden by specifying
  /// a value for the <c>log4net.NullText</c> appSetting in the application's
  /// .config file.
  /// </para>
  /// </remarks>
  public static string NullText { get; set; }

  /// <summary>
  /// Text to output when an unsupported feature is requested.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Use this value when an unsupported feature is requested.
  /// </para>
  /// <para>
  /// The default value is <c>NOT AVAILABLE</c>. This value can be overridden by specifying
  /// a value for the <c>log4net.NotAvailableText</c> appSetting in the application's
  /// .config file.
  /// </para>
  /// </remarks>
  public static string NotAvailableText { get; set; }

  /// <summary>
  /// Gets the assembly location path for the specified assembly.
  /// </summary>
  /// <param name="myAssembly">The assembly to get the location for.</param>
  /// <returns>The location of the assembly.</returns>
  /// <remarks>
  /// <para>
  /// This method does not guarantee to return the correct path
  /// to the assembly. If only tries to give an indication as to
  /// where the assembly was loaded from.
  /// </para>
  /// </remarks>
  public static string AssemblyLocationInfo(Assembly myAssembly)
  {
    if (myAssembly.EnsureNotNull().GlobalAssemblyCache)
    {
      return "Global Assembly Cache";
    }
    try
    {
      if (myAssembly.IsDynamic)
      {
        return "Dynamic Assembly";
      }

      if (myAssembly.GetType().FullName == "System.Reflection.Emit.InternalAssemblyBuilder")
      {
        return "Dynamic Assembly";
      }

      // This call requires FileIOPermission for access to the path
      // if we don't have permission then we just ignore it and
      // carry on.
      return myAssembly.Location;
    }
    catch (NotSupportedException)
    {
      // The location information may be unavailable for dynamic assemblies and a NotSupportedException
      // is thrown in those cases. See: http://msdn.microsoft.com/de-de/library/system.reflection.assembly.location.aspx
      return "Dynamic Assembly";
    }
    catch (TargetInvocationException ex)
    {
      return $"Location Detect Failed ({ex.Message})";
    }
    catch (ArgumentException ex)
    {
      return $"Location Detect Failed ({ex.Message})";
    }
    catch (System.Security.SecurityException)
    {
      return "Location Permission Denied";
    }
  }

  /// <summary>
  /// Gets the short name of the <see cref="Assembly" />.
  /// </summary>
  /// <param name="myAssembly">The <see cref="Assembly" /> to get the name for.</param>
  /// <returns>The short name of the <see cref="Assembly" />.</returns>
  /// <remarks>
  /// <para>
  /// The short name of the assembly is the <see cref="Assembly.FullName" /> 
  /// without the version, culture, or public key. i.e. it is just the 
  /// assembly's file name without the extension.
  /// </para>
  /// <para>
  /// Because of a FileIOPermission security demand we cannot do
  /// the obvious Assembly.GetName().Name. We are allowed to get
  /// the <see cref="Assembly.FullName" /> of the assembly so we 
  /// start from there and strip out just the assembly name.
  /// </para>
  /// </remarks>
  public static string AssemblyShortName(Assembly myAssembly)
  {
    string name = myAssembly.EnsureNotNull().FullName ?? string.Empty;
    int offset = name.IndexOf(',');
    if (offset > 0)
    {
      name = name.Substring(0, offset);
    }
    return name.Trim();
  }

  /// <summary>
  /// Gets the file name portion of the <see cref="Assembly" />, including the extension.
  /// </summary>
  /// <param name="myAssembly">The <see cref="Assembly" /> to get the file name for.</param>
  /// <returns>The file name of the assembly.</returns>
  /// <remarks>
  /// <para>
  /// Gets the file name portion of the <see cref="Assembly" />, including the extension.
  /// </para>
  /// </remarks>
  public static string AssemblyFileName(Assembly myAssembly) 
    => Path.GetFileName(myAssembly.EnsureNotNull().Location);

  /// <summary>
  /// Loads the type specified in the type string.
  /// </summary>
  /// <param name="relativeType">A sibling type to use to load the type.</param>
  /// <param name="typeName">The name of the type to load.</param>
  /// <param name="throwOnError">Flag set to <c>true</c> to throw an exception if the type cannot be loaded.</param>
  /// <param name="ignoreCase"><c>true</c> to ignore the case of the type name; otherwise, <c>false</c></param>
  /// <returns>The type loaded or <c>null</c> if it could not be loaded.</returns>
  /// <remarks>
  /// <para>
  /// If the type name is fully qualified, i.e. if contains an assembly name in 
  /// the type name, the type will be loaded from the system using 
  /// <see cref="Type.GetType(string,bool)"/>.
  /// </para>
  /// <para>
  /// If the type name is not fully qualified, it will be loaded from the assembly
  /// containing the specified relative type. If the type is not found in the assembly 
  /// then all the loaded assemblies will be searched for the type.
  /// </para>
  /// </remarks>
  public static Type? GetTypeFromString(Type relativeType, string typeName, bool throwOnError, bool ignoreCase)
  {
    return GetTypeFromString(relativeType.EnsureNotNull().Assembly, typeName, throwOnError, ignoreCase);
  }

  /// <summary>
  /// Loads the type specified in the type string.
  /// </summary>
  /// <param name="typeName">The name of the type to load.</param>
  /// <param name="throwOnError">Flag set to <c>true</c> to throw an exception if the type cannot be loaded.</param>
  /// <param name="ignoreCase"><c>true</c> to ignore the case of the type name; otherwise, <c>false</c></param>
  /// <returns>The type loaded or <c>null</c> if it could not be loaded.</returns>    
  /// <remarks>
  /// <para>
  /// If the type name is fully qualified, i.e. if contains an assembly name in 
  /// the type name, the type will be loaded from the system using 
  /// <see cref="Type.GetType(string,bool)"/>.
  /// </para>
  /// <para>
  /// If the type name is not fully qualified it will be loaded from the
  /// assembly that is directly calling this method. If the type is not found 
  /// in the assembly then all the loaded assemblies will be searched for the type.
  /// </para>
  /// </remarks>
  public static Type? GetTypeFromString(string typeName, bool throwOnError, bool ignoreCase)
  {
    return GetTypeFromString(Assembly.GetCallingAssembly(), typeName, throwOnError, ignoreCase);
  }

  /// <summary>
  /// Loads the type specified in the type string.
  /// </summary>
  /// <param name="relativeAssembly">An assembly to load the type from.</param>
  /// <param name="typeName">The name of the type to load.</param>
  /// <param name="throwOnError">Flag set to <c>true</c> to throw an exception if the type cannot be loaded.</param>
  /// <param name="ignoreCase"><c>true</c> to ignore the case of the type name; otherwise, <c>false</c></param>
  /// <returns>The type loaded or <c>null</c> if it could not be loaded.</returns>
  /// <remarks>
  /// <para>
  /// If the type name is fully qualified, i.e. if contains an assembly name in 
  /// the type name, the type will be loaded from the system using 
  /// <see cref="Type.GetType(string,bool)"/>.
  /// </para>
  /// <para>
  /// If the type name is not fully qualified it will be loaded from the specified
  /// assembly. If the type is not found in the assembly then all the loaded assemblies 
  /// will be searched for the type.
  /// </para>
  /// </remarks>
  public static Type? GetTypeFromString(Assembly relativeAssembly, string typeName, bool throwOnError, bool ignoreCase)
  {
    // Check if the type name specifies the assembly name
    if (typeName.EnsureNotNull().IndexOf(',') == -1)
    {
      // Attempt to look up the type from the relativeAssembly
      if (relativeAssembly.EnsureNotNull().GetType(typeName, false, ignoreCase) is Type type)
      {
        return type;
      }

      Assembly[]? loadedAssemblies = null;
      try
      {
        loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
      }
      catch (System.Security.SecurityException)
      {
        // Insufficient permissions to get the list of loaded assemblies
      }

      if (loadedAssemblies is not null)
      {
        Type? fallback = null;
        // Search the loaded assemblies for the type
        foreach (Assembly assembly in loadedAssemblies)
        {
          if (assembly.GetType(typeName, false, ignoreCase) is Type t)
          {
            // Found type in loaded assembly
            LogLog.Debug(_declaringType, $"Loaded type [{typeName}] from assembly [{assembly.FullName}] by searching loaded assemblies.");
            if (assembly.GlobalAssemblyCache)
            {
              fallback = t;
            }
            else
            {
              return t;
            }
          }
        }
        if (fallback is not null)
        {
          return fallback;
        }
      }

      // Didn't find the type
      if (throwOnError)
      {
        throw new TypeLoadException($"Could not load type [{typeName}]. Tried assembly [{relativeAssembly.FullName}] and all loaded assemblies");
      }
      return null;
    }

    // Includes explicit assembly name
    return Type.GetType(typeName, throwOnError, ignoreCase);
  }

  /// <summary>
  /// Creates an <see cref="ArgumentOutOfRangeException"/>
  /// </summary>
  /// <param name="parameterName">The name of the parameter that caused the exception</param>
  /// <param name="actualValue">The value of the argument that causes this exception</param>
  /// <param name="message">The message that describes the error</param>
  /// <returns>
  /// A new instance of the <see cref="ArgumentOutOfRangeException"/> class 
  /// with the specified error message, parameter name, and value
  /// of the argument.
  /// </returns>
  public static ArgumentOutOfRangeException CreateArgumentOutOfRangeException(string parameterName, object actualValue, string message) 
    => new(parameterName, actualValue, message);

  /// <summary>
  /// Creates a <see cref="NotSupportedException"/> for read-only collection modification calls.
  /// </summary>
  /// <returns>The NotSupportedException object</returns>
  public static NotSupportedException CreateReadOnlyCollectionNotModifiableException() 
    => new("This is a Read Only Collection and can not be modified");

  /// <summary>
  /// Parse a string into an <see cref="int"/> value
  /// </summary>
  /// <param name="s">the string to parse</param>
  /// <param name="val">out param where the parsed value is placed</param>
  /// <returns><c>true</c> if the string was parsed into an integer</returns>
  /// <remarks>
  /// <para>
  /// Attempts to parse the string into an integer. If the string cannot
  /// be parsed then this method returns <c>false</c>. The method does not throw an exception.
  /// </para>
  /// </remarks>
  public static bool TryParse(string s, out int val)
  {
    val = 0;

    try
    {
      if (double.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out double doubleVal))
      {
        val = Convert.ToInt32(doubleVal);
        return true;
      }
    }
    catch (Exception e) when (!e.IsFatal())
    {
      // Ignore exception, just return false
    }

    return false;
  }

  /// <summary>
  /// Parse a string into an <see cref="long"/> value
  /// </summary>
  /// <param name="s">the string to parse</param>
  /// <param name="val">out param where the parsed value is placed</param>
  /// <returns><c>true</c> if the string was parsed into an integer</returns>
  /// <remarks>
  /// <para>
  /// Attempts to parse the string into an integer. If the string cannot
  /// be parsed then this method returns <c>false</c>. The method does not throw an exception.
  /// </para>
  /// </remarks>
  public static bool TryParse(string s, out long val)
  {
    val = 0;

    try
    {
      if (double.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out double doubleVal))
      {
        val = Convert.ToInt64(doubleVal);
        return true;
      }
    }
    catch (Exception e) when (!e.IsFatal())
    {
      // Ignore exception, just return false
    }

    return false;
  }

  /// <summary>
  /// Parse a string into an <see cref="short"/> value
  /// </summary>
  /// <param name="s">the string to parse</param>
  /// <param name="val">out param where the parsed value is placed</param>
  /// <returns><c>true</c> if the string was parsed into an integer</returns>
  /// <remarks>
  /// <para>
  /// Attempts to parse the string into an integer. If the string cannot
  /// be parsed then this method returns <c>false</c>. The method does not throw an exception.
  /// </para>
  /// </remarks>
  public static bool TryParse(string s, out short val)
  {
    val = 0;

    try
    {
      if (double.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out double doubleVal))
      {
        val = Convert.ToInt16(doubleVal);
        return true;
      }
    }
    catch (Exception e) when (!e.IsFatal())
    {
      // Ignore exception, just return false
    }

    return false;
  }

  /// <summary>
  /// Lookup an application setting
  /// </summary>
  /// <param name="key">the application settings key to lookup</param>
  /// <returns>the value for the key, or <c>null</c></returns>
  public static string? GetAppSetting(string key)
  {
    try
    {
      return ConfigurationManager.AppSettings[key];
    }
    catch (Exception e) when (!e.IsFatal())
    {
      // If an exception is thrown here then it looks like the config file does not parse correctly.
      LogLog.Error(_declaringType, "Exception while reading ConfigurationSettings. Check your .config file is well formed XML.", e);
    }
    return null;
  }

  /// <summary>
  /// Convert a path into a fully qualified local file path.
  /// </summary>
  /// <param name="path">The path to convert.</param>
  /// <returns>The fully qualified path.</returns>
  /// <remarks>
  /// <para>
  /// Converts the path specified to a fully
  /// qualified path. If the path is relative it is
  /// taken as relative from the application base 
  /// directory.
  /// </para>
  /// <para>
  /// The path specified must be a local file path, a URI is not supported.
  /// </para>
  /// </remarks>
  public static string ConvertToFullPath(string path)
  {
    path.EnsureNotNull();

    string baseDirectory = string.Empty;
    try
    {
      string applicationBaseDirectory = ApplicationBaseDirectory;

      // applicationBaseDirectory may be a URI not a local file path
      Uri applicationBaseDirectoryUri = new(applicationBaseDirectory);
      if (applicationBaseDirectoryUri.IsFile)
      {
        baseDirectory = applicationBaseDirectoryUri.LocalPath;
      }
    }
    catch (Exception e) when (!e.IsFatal())
    {
      // Ignore URI exceptions & SecurityExceptions from SystemInfo.ApplicationBaseDirectory
    }

    if (!string.IsNullOrEmpty(baseDirectory))
    {
      // Note that Path.Combine will return the second path if it is rooted
      return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }
    return Path.GetFullPath(path);
  }

  /// <summary>
  /// Creates a new case-insensitive instance of the <see cref="Hashtable"/> class with the default initial capacity. 
  /// </summary>
  /// <returns>A new case-insensitive instance of the <see cref="Hashtable"/> class with the default initial capacity</returns>
  /// <remarks>
  /// <para>
  /// The new Hashtable instance uses the default load factor, the CaseInsensitiveHashCodeProvider, and the CaseInsensitiveComparer.
  /// </para>
  /// </remarks>
  public static Hashtable CreateCaseInsensitiveHashtable() => new(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Tests two strings for equality, the ignoring case.
  /// </summary>
  /// <remarks>
  /// If the platform permits, culture information is ignored completely (ordinal comparison).
  /// The aim of this method is to provide a fast comparison that deals with <c>null</c> and ignores different casing.
  /// It is not supposed to deal with various, culture-specific habits.
  /// Use it to compare against pure ASCII constants, like keywords etc.
  /// </remarks>
  /// <param name="a">The one string.</param>
  /// <param name="b">The other string.</param>
  /// <returns><c>true</c> if the strings are equal, <c>false</c> otherwise.</returns>
  public static bool EqualsIgnoringCase(string? a, string? b)
    => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

  /// <summary>
  /// The fully qualified type of the SystemInfo class.
  /// </summary>
  /// <remarks>
  /// Used by the internal logger to record the Type of the
  /// log message.
  /// </remarks>
  private static readonly Type _declaringType = typeof(SystemInfo);

  /// <summary>
  /// Cache the host name for the current machine
  /// </summary>
  private static string? _sHostName;

  /// <summary>
  /// Cache the application friendly name
  /// </summary>
  private static string? _sAppFriendlyName;
}