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
using log4net.Util;
#if (!NETCF && !SSCLI && !NETSTANDARD1_3)
using System.Diagnostics;
using System.Security.Principal;
#endif

namespace log4net.Core
{
	/// <summary>
	/// Provide methods for interactions with WindowsIdentity.
	/// </summary>
	public class WindowsIdentityProvider
	{
		private readonly static Type declaringType = typeof(WindowsIdentityProvider);
		
#if (!NETCF && !SSCLI && !NETSTANDARD1_3)
		private class IdentityCache
		{
			public readonly Stopwatch Obtained;
			public readonly SecurityIdentifier Sid;
			public readonly string Name;

			public IdentityCache(Stopwatch obtained, SecurityIdentifier sid, string name)
			{
				Obtained = obtained;
				Sid = sid;
				if (name == null) { name = ""; }
				Name = name;
			}
		}

		private static volatile IdentityCache CachedIdentity = null;
#endif

		/// <summary>
		/// Gets the name of the current user.
		/// </summary>
		/// <value>
		/// The name of the current user, or <c>NOT AVAILABLE</c> when the 
		/// underlying runtime has no support for retrieving the name of the 
		/// current user.
		/// <exception cref="SecurityException"/>
		/// </value>
		/// <remarks>
		/// <para>
		/// Calls <c>WindowsIdentity.GetCurrent().Name</c> to get the name of
		/// the current windows user.
		/// </para>
		/// <para>
		/// To improve performance, we cache the string representation of 
		/// the name, and reuse that as long as the SID stayed constant.  
		/// If the SID changes or 10 seconds have passed since the name
		/// was looked up then the name is looked up again.
		/// </para>
		/// </remarks>
		public static string CurrentIdentityName
		{
			get
			{
#if (NETCF || SSCLI || NETSTANDARD1_3) // NETSTANDARD1_3 TODO requires platform-specific code
				return SystemInfo.NotAvailableText;
#else
				try
				{
					WindowsIdentity currentWindowsIdentity = WindowsIdentity.GetCurrent();
					IdentityCache localCachedIdentity = CachedIdentity;

					//If the SID doesn't match or the name was obtained more than 10 seconds ago then lookup the name again.  We do this to
					//make sure the name is not more than 10 seconds out of date in the rare scenario where a user has been renamed
					if (localCachedIdentity == null || localCachedIdentity.Obtained.ElapsedMilliseconds > 10000 || !localCachedIdentity.Sid.Equals(currentWindowsIdentity.User))
					{
						localCachedIdentity = new IdentityCache(Stopwatch.StartNew(), currentWindowsIdentity.User, currentWindowsIdentity.Name);
						CachedIdentity = localCachedIdentity;
					}
					
					return localCachedIdentity.Name;
				}
				catch(SecurityException)
				{
					// This security exception will occur if the caller does not have 
					// some undefined set of SecurityPermission flags.
					LogLog.Debug(declaringType, "Security exception while trying to get current windows identity. Error Ignored. Empty user name.");
					return "";
				}
#endif
			}
		}
	}
}
