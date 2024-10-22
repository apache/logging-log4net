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
using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

using log4net.Core;

namespace log4net.Util;

/// <summary>
/// Impersonate a Windows Account
/// </summary>
/// <remarks>
/// <para>
/// This <see cref="SecurityContext"/> impersonates a Windows account.
/// </para>
/// <para>
/// How the impersonation is done depends on the value of <see cref="Impersonate"/>.
/// This allows the context to either impersonate a set of user credentials specified 
/// using username, domain name and password or to revert to the process credentials.
/// </para>
/// </remarks>
public class WindowsSecurityContext : SecurityContext, IOptionHandler
{
  /// <summary>
  /// The impersonation modes for the <see cref="WindowsSecurityContext"/>
  /// </summary>
  /// <remarks>
  /// <para>
  /// See the <see cref="WindowsSecurityContext.Credentials"/> property for
  /// details.
  /// </para>
  /// </remarks>
  public enum ImpersonationMode
  {
    /// <summary>
    /// Impersonate a user using the credentials supplied
    /// </summary>
    User,

    /// <summary>
    /// Revert this the thread to the credentials of the process
    /// </summary>
    Process
  }

  private string? _password;
  private WindowsIdentity? _identity;

  /// <summary>
  /// Gets or sets the impersonation mode for this security context
  /// </summary>
  /// <value>
  /// The impersonation mode for this security context
  /// </value>
  /// <remarks>
  /// <para>
  /// Impersonate either a user with user credentials or
  /// revert this thread to the credentials of the process.
  /// The value is one of the <see cref="ImpersonationMode"/>
  /// enum.
  /// </para>
  /// <para>
  /// The default value is <see cref="ImpersonationMode.User"/>
  /// </para>
  /// <para>
  /// When the mode is set to <see cref="ImpersonationMode.User"/>
  /// the user's credentials are established using the
  /// <see cref="UserName"/>, <see cref="DomainName"/> and <see cref="Password"/>
  /// values.
  /// </para>
  /// <para>
  /// When the mode is set to <see cref="ImpersonationMode.Process"/>
  /// no other properties need to be set. If the calling thread is 
  /// impersonating then it will be reverted back to the process credentials.
  /// </para>
  /// </remarks>
  public ImpersonationMode Credentials { get; set; } = ImpersonationMode.User;

  /// <summary>
  /// Gets or sets the Windows username for this security context
  /// </summary>
  /// <value>
  /// The Windows username for this security context
  /// </value>
  /// <remarks>
  /// <para>
  /// This property must be set if <see cref="Credentials"/>
  /// is set to <see cref="ImpersonationMode.User"/> (the default setting).
  /// </para>
  /// </remarks>
  public string? UserName { get; set; }

  /// <summary>
  /// Gets or sets the Windows domain name for this security context
  /// </summary>
  /// <value>
  /// The Windows domain name for this security context
  /// </value>
  /// <remarks>
  /// <para>
  /// The default value for <see cref="DomainName"/> is the local machine name
  /// taken from the <see cref="Environment.MachineName"/> property.
  /// </para>
  /// <para>
  /// This property must be set if <see cref="Credentials"/>
  /// is set to <see cref="ImpersonationMode.User"/> (the default setting).
  /// </para>
  /// </remarks>
  public string DomainName { get; set; } = Environment.MachineName;

  /// <summary>
  /// Sets the password for the Windows account specified by the <see cref="UserName"/> and <see cref="DomainName"/> properties.
  /// </summary>
  /// <value>
  /// The password for the Windows account specified by the <see cref="UserName"/> and <see cref="DomainName"/> properties.
  /// </value>
  /// <remarks>
  /// <para>
  /// This property must be set if <see cref="Credentials"/>
  /// is set to <see cref="ImpersonationMode.User"/> (the default setting).
  /// </para>
  /// </remarks>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1044:Properties should not be write only", 
    Justification = "Password must be write only")]
  public string Password { set => _password = value; }

  /// <summary>
  /// Initialize the SecurityContext based on the options set.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is part of the <see cref="IOptionHandler"/> delayed object
  /// activation scheme. The <see cref="ActivateOptions"/> method must 
  /// be called on this object after the configuration properties have
  /// been set. Until <see cref="ActivateOptions"/> is called this
  /// object is in an undefined state and must not be used. 
  /// </para>
  /// <para>
  /// If any of the configuration properties are modified then 
  /// <see cref="ActivateOptions"/> must be called again.
  /// </para>
  /// <para>
  /// The security context will try to Logon the specified user account and
  /// capture a primary token for impersonation.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentNullException">The required <see cref="UserName" />, 
  /// <see cref="DomainName" /> or <see cref="Password" /> properties were not specified.</exception>
  public void ActivateOptions()
  {
    if (Credentials == ImpersonationMode.User)
    {
      if (UserName is null)
      {
        throw new ArgumentNullException(nameof(UserName));
      }
      if (DomainName is null)
      {
        throw new ArgumentNullException(nameof(DomainName));
      }
      if (_password is null)
      {
        throw new ArgumentNullException(nameof(Password));
      }

      _identity = LogonUser(UserName, DomainName, _password);
    }
  }

  /// <summary>
  /// Impersonate the Windows account specified by the <see cref="UserName"/> and <see cref="DomainName"/> properties.
  /// </summary>
  /// <param name="state">caller provided state</param>
  /// <returns>
  /// An <see cref="IDisposable"/> instance that will revoke the impersonation of this SecurityContext
  /// </returns>
  /// <remarks>
  /// <para>
  /// Depending on the <see cref="Credentials"/> property either
  /// impersonate a user using credentials supplied or revert 
  /// to the process credentials.
  /// </para>
  /// </remarks>
  public override IDisposable? Impersonate(object state)
  {
    if (Credentials == ImpersonationMode.User)
    {
      if (_identity is not null)
      {
        return new DisposableImpersonationContext(_identity.Impersonate());
      }
    }
    else if (Credentials == ImpersonationMode.Process)
    {
      // Impersonate(0) will revert to the process credentials
      return new DisposableImpersonationContext(WindowsIdentity.Impersonate(IntPtr.Zero));
    }
    return null;
  }

  /// <summary>
  /// Create a <see cref="WindowsIdentity"/> given the userName, domainName and password.
  /// </summary>
  /// <param name="userName">the user name</param>
  /// <param name="domainName">the domain name</param>
  /// <param name="password">the password</param>
  /// <returns>the <see cref="WindowsIdentity"/> for the account specified</returns>
  /// <remarks>
  /// <para>
  /// Uses the Windows API call LogonUser to get a principal token for the account. This
  /// token is used to initialize the WindowsIdentity.
  /// </para>
  /// </remarks>
  [System.Security.SecuritySafeCritical]
  [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, UnmanagedCode = true)]
  private static WindowsIdentity LogonUser(string userName, string domainName, string password)
  {
    const int logon32ProviderDefault = 0;
    //This parameter causes LogonUser to create a primary token.
    const int logon32LogonInteractive = 2;

    // Call LogonUser to obtain a handle to an access token.
    IntPtr tokenHandle = IntPtr.Zero;
    if (!LogonUser(userName, domainName, password, logon32LogonInteractive, logon32ProviderDefault, ref tokenHandle))
    {
      NativeError error = NativeError.GetLastError();
      throw new Exception($"Failed to LogonUser [{userName}] in Domain [{domainName}]. Error: {error.ToString()}");
    }

    const int securityImpersonation = 2;
    IntPtr dupeTokenHandle = IntPtr.Zero;
    if (!DuplicateToken(tokenHandle, securityImpersonation, ref dupeTokenHandle))
    {
      NativeError error = NativeError.GetLastError();
      if (tokenHandle != IntPtr.Zero)
      {
        CloseHandle(tokenHandle);
      }
      throw new Exception($"Failed to DuplicateToken after LogonUser. Error: {error}");
    }

    var identity = new WindowsIdentity(dupeTokenHandle);

    // Free the tokens.
    if (dupeTokenHandle != IntPtr.Zero)
    {
      CloseHandle(dupeTokenHandle);
    }
    if (tokenHandle != IntPtr.Zero)
    {
      CloseHandle(tokenHandle);
    }

    return identity;
  }

  [DllImport("advapi32.dll", SetLastError = true)]
  [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
  private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

  [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
  [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
  private static extern bool CloseHandle(IntPtr handle);

  [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
  [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
  private static extern bool DuplicateToken(IntPtr existingTokenHandle, int securityImpersonationLevel, ref IntPtr duplicateTokenHandle);

  /// <summary>
  /// Adds <see cref="IDisposable"/> to <see cref="WindowsImpersonationContext"/>
  /// </summary>
  /// <remarks>
  /// <para>
  /// Helper class to expose the <see cref="WindowsImpersonationContext"/>
  /// through the <see cref="IDisposable"/> interface.
  /// </para>
  /// </remarks>
  private sealed class DisposableImpersonationContext : IDisposable
  {
    private readonly WindowsImpersonationContext _impersonationContext;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="impersonationContext">the impersonation context being wrapped</param>
    public DisposableImpersonationContext(WindowsImpersonationContext impersonationContext)
      => this._impersonationContext = impersonationContext;

    /// <summary>
    /// Revert the impersonation
    /// </summary>
    public void Dispose() => _impersonationContext.Undo();
  }
}
#endif // NET462_OR_GREATER